using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using Supabase;
using System;
using System.IO;
using System.Threading.Tasks;
using SubdiHub_v1.Components.Models;

namespace SubdiHub_v1.Components.Dashboards.Admin
{
    public partial class UploadAnnouncement : ContentPage
    {
        private readonly Supabase.Client _client;
        private string _userId;
        private User _currentUser;
        private SettingsDb _userSettings;
        private bool _isDataLoaded; // Prevent duplicate loading
        private FileResult _selectedImage; // Store the selected image for upload

        public UploadAnnouncement()
        {
            InitializeComponent();

            // Initialize the Supabase client
            _client = new Supabase.Client(
                "https://cqslvtgwuabdgqxxtzfa.supabase.co",
                "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6ImNxc2x2dGd3dWFiZGdxeHh0emZhIiwicm9sZSI6ImFub24iLCJpYXQiOjE3NDM0Njg1MzksImV4cCI6MjA1OTA0NDUzOX0.m3gKzHkUMal7WhkA4rF-ymGL_97yQAGdUulGfy3Se7g"
            );

            _isDataLoaded = false;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            if (!_isDataLoaded)
            {
                await LoadUserDataOnLogin();
                _isDataLoaded = true;
            }
        }

        private async Task LoadUserDataOnLogin()
        {
            try
            {
                // Fetch user ID from SecureStorage
                _userId = await SecureStorage.GetAsync("session_token");
                if (string.IsNullOrEmpty(_userId) || _userId == "00000000-0000-0000-0000-000000000000")
                {
                    await DisplayAlert("Error", "User not logged in or invalid session token.", "OK");
                    await Shell.Current.GoToAsync("//Login_page");
                    return;
                }

                // Load user data
                await LoadUserData();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to initialize user data: {ex.Message}", "OK");
            }
        }

        private async Task LoadUserData()
        {
            try
            {
                // Fetch user and settings in parallel
                var userTask = _client
                    .From<User>()
                    .Filter("User_ID", Supabase.Postgrest.Constants.Operator.Equals, _userId)
                    .Single();

                var settingsTask = _client
                    .From<SettingsDb>()
                    .Filter("user_id", Supabase.Postgrest.Constants.Operator.Equals, _userId)
                    .Single();

                // Await both tasks
                await Task.WhenAll(userTask, settingsTask);
                _currentUser = await userTask;
                _userSettings = await settingsTask;

                if (_currentUser != null)
                {
                    // Populate UI elements
                    UserNameLabel.Text = _currentUser.FullName;
                    DateLabel.Text = DateTime.Now.ToString("MMMM dd, yyyy");

                    // Load profile image
                    if (_userSettings != null && !string.IsNullOrEmpty(_userSettings.ProfileImage))
                    {
                        ProfileImage.Source = _userSettings.ProfileImage;
                    }
                    else
                    {
                        ProfileImage.Source = "default_profile.png"; // Ensure this resource exists
                    }
                }
                else
                {
                    await DisplayAlert("Error", "User not found in users table.", "OK");
                    await Shell.Current.GoToAsync("//Login_page");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to load user data: {ex.Message}", "OK");
            }
        }

        private async void OnSelectImageClicked(object sender, EventArgs e)
        {
            try
            {
                var fileResult = await FilePicker.PickAsync(new PickOptions
                {
                    FileTypes = FilePickerFileType.Images,
                    PickerTitle = "Select an image"
                });

                if (fileResult != null)
                {
                    _selectedImage = fileResult; // Store for upload during post
                    var stream = await fileResult.OpenReadAsync();
                    PreviewImage.Source = ImageSource.FromStream(() => stream);
                    PreviewImageBorder.IsVisible = true;
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to select image: {ex.Message}", "OK");
            }
        }

        private async void OnPostAnnouncementClicked(object sender, EventArgs e)
        {
            try
            {
                // Check if the user is an admin
                if (_currentUser.Role != "Admin")
                {
                    await DisplayAlert("Permission Denied", "Only admins can post announcements.", "OK");
                    return;
                }

                // Validate announcement content
                if (string.IsNullOrWhiteSpace(PostEditor.Text))
                {
                    await DisplayAlert("Error", "Please enter an announcement.", "OK");
                    return;
                }

                // Upload image if selected
                string imageUrl = null;
                if (_selectedImage != null)
                {
                    imageUrl = await UploadImageToSupabaseStorage(_selectedImage);
                }

                // Create the announcement
                var announcement = new Announcement
                {
                    Id = Guid.NewGuid().ToString(), // Supabase expects a string for uuid
                    UserId = _userId, // Matches user_id in the table
                    Content = PostEditor.Text,
                    ImageUrl = imageUrl,
                    CreatedAt = DateTime.UtcNow
                };

                // Save to Supabase
                await _client
                    .From<Announcement>()
                    .Insert(announcement);

                await DisplayAlert("Success", "Announcement posted successfully!", "OK");

                // Reset the form
                PostEditor.Text = string.Empty;
                PreviewImage.Source = null;
                PreviewImageBorder.IsVisible = false;
                _selectedImage = null;
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to post announcement: {ex.Message}\nInner Exception: {ex.InnerException?.Message}", "OK");
            }
        }

        private async Task<string> UploadImageToSupabaseStorage(FileResult fileResult)
        {
            try
            {
                using var stream = await fileResult.OpenReadAsync();
                using var memoryStream = new MemoryStream();
                await stream.CopyToAsync(memoryStream);
                var fileBytes = memoryStream.ToArray();

                var fileName = $"{Guid.NewGuid()}{Path.GetExtension(fileResult.FileName)}";
                var storage = _client.Storage;
                var bucket = storage.From("announcementimages"); // Create this bucket in Supabase
                await bucket.Upload(fileBytes, fileName);

                var publicUrl = bucket.GetPublicUrl(fileName);
                return publicUrl;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to upload image to Supabase Storage: {ex.Message}\nInner Exception: {ex.InnerException?.Message}");
            }
        }
    }
}