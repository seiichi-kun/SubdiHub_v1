using Microsoft.Maui.Storage;
using Supabase;
using System;
using System.IO;
using System.Threading.Tasks;
using SubdiHub_v1.Components.Models;
using BCrypt.Net;

namespace SubdiHub_v1.Components.Dashboards.Seller
{
    public partial class Edit_profile_S : ContentPage
    {
        private readonly Supabase.Client client;
        private string userId;
        private User currentUser;
        private SettingsDb userSettings;
        private bool isDataLoaded; // Prevent duplicate loading

        public Edit_profile_S()
        {
            InitializeComponent();

            // Initialize the Supabase client
            client = new Supabase.Client(
                "https://cqslvtgwuabdgqxxtzfa.supabase.co",
                "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6ImNxc2x2dGd3dWFiZGdxeHh0emZhIiwicm9sZSI6ImFub24iLCJpYXQiOjE3NDM0Njg1MzksImV4cCI6MjA1OTA0NDUzOX0.m3gKzHkUMal7WhkA4rF-ymGL_97yQAGdUulGfy3Se7g"
            );

            isDataLoaded = false;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            if (!isDataLoaded)
            {
                await LoadUserDataOnLogin();
                isDataLoaded = true;
            }
        }

        private async Task LoadUserDataOnLogin()
        {
            try
            {
                // Fetch user ID from SecureStorage
                userId = await SecureStorage.GetAsync("session_token");
                if (string.IsNullOrEmpty(userId) || userId == "00000000-0000-0000-0000-000000000000")
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
                var userTask = client
                    .From<User>()
                    .Filter("User_ID", Supabase.Postgrest.Constants.Operator.Equals, userId)
                    .Single();

                var settingsTask = client
                    .From<SettingsDb>()
                    .Filter("user_id", Supabase.Postgrest.Constants.Operator.Equals, userId)
                    .Single();

                // Await both tasks
                await Task.WhenAll(userTask, settingsTask);
                currentUser = await userTask;
                userSettings = await settingsTask;

                if (currentUser != null)
                {
                    // Populate UI elements
                    FullNameEntry.Text = currentUser.FullName;
                    UsernameEntry.Text = currentUser.Username;
                    EmailEntry.Text = currentUser.Email;
                    ContactEntry.Text = currentUser.Contact;
                    StreetEntry.Text = currentUser.Street;
                    BlockEntry.Text = currentUser.Block;
                    LotEntry.Text = currentUser.Lot;

                    // Load profile image
                    if (userSettings != null && !string.IsNullOrEmpty(userSettings.ProfileImage))
                    {
                        ProfileImage.Source = userSettings.ProfileImage;
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

        private async void OnUploadProfilePictureClicked(object sender, EventArgs e)
        {
            try
            {
                var fileResult = await FilePicker.PickAsync(new PickOptions
                {
                    FileTypes = FilePickerFileType.Images,
                    PickerTitle = "Select a profile picture"
                });

                if (fileResult != null)
                {
                    string profileImageUrl = await UploadImageToSupabaseStorage(fileResult);
                    await SaveProfileImage(profileImageUrl);
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to upload image: {ex.Message}\nInner Exception: {ex.InnerException?.Message}", "OK");
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
                var storage = client.Storage;
                var bucket = storage.From("profileimages");
                await bucket.Upload(fileBytes, fileName);

                var publicUrl = bucket.GetPublicUrl(fileName);
                return publicUrl;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to upload image to Supabase Storage: {ex.Message}\nInner Exception: {ex.InnerException?.Message}\nStack Trace: {ex.StackTrace}");
            }
        }

        private async Task SaveProfileImage(string profileImageUrl)
        {
            try
            {
                // Validate userId
                if (string.IsNullOrEmpty(userId) || userId == "00000000-0000-0000-0000-000000000000")
                {
                    await DisplayAlert("Error", "Invalid user ID. Please log in again.", "OK");
                    await Shell.Current.GoToAsync("//Login_page");
                    return;
                }

                if (userSettings != null)
                {
                    userSettings.ProfileImage = profileImageUrl;
                    await client
                        .From<SettingsDb>()
                        .Update(userSettings);
                }
                else
                {
                    userSettings = new SettingsDb
                    {
                        Id = Guid.NewGuid().ToString(),
                        UserId = userId,
                        ProfileImage = profileImageUrl,
                        CreatedAt = DateTime.UtcNow
                    };

                    await client
                        .From<SettingsDb>()
                        .Insert(userSettings);
                }

                await DisplayAlert("Success", "Profile picture updated successfully!", "OK");
                ProfileImage.Source = profileImageUrl;
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to save profile picture: {ex.Message}\nInner Exception: {ex.InnerException?.Message}", "OK");
            }
        }

        private async void OnSaveChangesClicked(object sender, EventArgs e)
        {
            try
            {
                // Validate required fields
                if (string.IsNullOrWhiteSpace(FullNameEntry.Text) ||
                    string.IsNullOrWhiteSpace(UsernameEntry.Text) ||
                    string.IsNullOrWhiteSpace(EmailEntry.Text))
                {
                    await DisplayAlert("Error", "Full Name, Username, and Email are required.", "OK");
                    return;
                }

                // Validate email format
                if (!EmailEntry.Text.Contains("@") || !EmailEntry.Text.Contains("."))
                {
                    await DisplayAlert("Error", "Please enter a valid email address.", "OK");
                    return;
                }

                // Handle password change
                if (!string.IsNullOrWhiteSpace(PasswordEntry.Text) || !string.IsNullOrWhiteSpace(ConfirmPasswordEntry.Text))
                {
                    if (PasswordEntry.Text != ConfirmPasswordEntry.Text)
                    {
                        await DisplayAlert("Error", "Passwords do not match.", "OK");
                        return;
                    }

                    if (PasswordEntry.Text.Length < 6)
                    {
                        await DisplayAlert("Error", "Password must be at least 6 characters long.", "OK");
                        return;
                    }

                    currentUser.PasswordHash = BCrypt.Net.BCrypt.HashPassword(PasswordEntry.Text);
                }

                // Update user data
                currentUser.FullName = FullNameEntry.Text;
                currentUser.Username = UsernameEntry.Text;
                currentUser.Email = EmailEntry.Text;
                currentUser.Contact = ContactEntry.Text;
                currentUser.Street = StreetEntry.Text;
                currentUser.Block = BlockEntry.Text;
                currentUser.Lot = LotEntry.Text;

                await client
                    .From<User>()
                    .Update(currentUser);

                // Update stored preferences
                Preferences.Set("UserFullName", currentUser.FullName);
                Preferences.Set("UserEmail", currentUser.Email);
                Preferences.Set("UserRole", currentUser.Role); // Added role

                await DisplayAlert("Success", "Profile updated successfully!", "OK");
                isDataLoaded = false; // Allow refresh on next appearance
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to save changes: {ex.Message}\nInner Exception: {ex.InnerException?.Message}", "OK");
            }
        }

        private async void OnCancelClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("..");
            isDataLoaded = false; // Allow refresh if re-entering
        }
    }
}