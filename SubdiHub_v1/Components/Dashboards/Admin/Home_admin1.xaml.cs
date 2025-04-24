using Microsoft.Maui.Controls;
using Supabase;
using System;
using System.Threading.Tasks;
using SubdiHub_v1.Components.Models;
using Microsoft.Maui.Controls.Shapes;

namespace SubdiHub_v1.Components.Dashboards.Admin
{
    public partial class Home_admin1 : ContentPage
    {
        private readonly Supabase.Client _client;

        public Home_admin1()
        {
            InitializeComponent();

            // Initialize the Supabase client
            _client = new Supabase.Client(
                "https://cqslvtgwuabdgqxxtzfa.supabase.co",
                "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6ImNxc2x2dGd3dWFiZGdxeHh0emZhIiwicm9sZSI6ImFub24iLCJpYXQiOjE3NDM0Njg1MzksImV4cCI6MjA1OTA0NDUzOX0.m3gKzHkUMal7WhkA4rF-ymGL_97yQAGdUulGfy3Se7g"
            );
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LoadAnnouncements();
        }

        private async Task LoadAnnouncements()
        {
            try
            {
                // Fetch announcements with user data (Full_name)
                var response = await _client
                    .From<Announcement>()
                    .Select("*")
                    .Order("created_at", Supabase.Postgrest.Constants.Ordering.Descending)
                    .Get();

                // Fetch all user IDs from announcements
                var userIds = response.Models.Select(a => a.UserId).Distinct().ToList();
                var settingsResponse = await _client
                    .From<SettingsDb>()
                    .Filter("user_id", Supabase.Postgrest.Constants.Operator.In, userIds)
                    .Get();
                var settingsDict = settingsResponse.Models.ToDictionary(s => s.UserId, s => s.ProfileImage);

                // Clear existing items
                AnnouncementsContainer.Children.Clear();

                // Create UI elements for each announcement
                foreach (var announcement in response.Models)
                {
                    // Get the profile image from the dictionary
                    string profileImageUrl = settingsDict.ContainsKey(announcement.UserId) ? settingsDict[announcement.UserId] : "default_profile.png";

                    // Create the card (Border)
                    var cardBorder = new Border
                    {
                        BackgroundColor = Colors.White,
                        StrokeThickness = 0,
                        Stroke = Color.Parse("#E0E6F0"),
                        Padding = new Thickness(15),
                        Margin = new Thickness(0, 5),
                        StrokeShape = new RoundRectangle { CornerRadius = 15 }
                    };
                    cardBorder.Shadow = new Shadow
                    {
                        Brush = Color.Parse("#20000000"),
                        Offset = new Point(0, 4),
                        Radius = 8,
                        Opacity = 0.1f
                    };

                    // Create the main layout for the card
                    var cardLayout = new Grid
                    {
                        RowDefinitions = new RowDefinitionCollection
                        {
                            new RowDefinition { Height = GridLength.Auto },
                            new RowDefinition { Height = GridLength.Star }
                        }
                    };

                    // Delete Button
                    var deleteButton = new Button
                    {
                        Text = "✕",
                        FontSize = 16,
                        TextColor = Colors.White,
                        BackgroundColor = Color.Parse("#EF4444"),
                        WidthRequest = 30,
                        HeightRequest = 30,
                        CornerRadius = 15,
                        HorizontalOptions = LayoutOptions.End,
                        VerticalOptions = LayoutOptions.Start,
                        Margin = new Thickness(0, -10, -10, 0)
                    };
                    deleteButton.Clicked += async (s, e) => await DeleteAnnouncement(announcement.Id);

                    // Content Layout
                    var contentLayout = new VerticalStackLayout { Spacing = 10 };
                    contentLayout.SetValue(Grid.RowProperty, 1);

                    // Admin Info Section
                    var adminInfoLayout = new HorizontalStackLayout { Spacing = 15 };

                    // Profile Image
                    var profileImageBorder = new Border
                    {
                        StrokeThickness = 2,
                        Stroke = Color.Parse("#E0E6F0"),
                        BackgroundColor = Colors.White,
                        WidthRequest = 50,
                        HeightRequest = 50,
                        Padding = new Thickness(2),
                        StrokeShape = new RoundRectangle { CornerRadius = 25 }
                    };
                    var profileImage = new Image
                    {
                        WidthRequest = 46,
                        HeightRequest = 46,
                        Aspect = Aspect.AspectFill,
                        Source = profileImageUrl
                    };
                    profileImage.Clip = new RoundRectangleGeometry
                    {
                        CornerRadius = 23,
                        Rect = new Rect(0, 0, 46, 46)
                    };
                    profileImageBorder.Content = profileImage;

                    // Admin Name and Date
                    var adminDetailsLayout = new VerticalStackLayout { VerticalOptions = LayoutOptions.Center };
                    var adminNameLabel = new Label
                    {
                        FontSize = 16,
                        FontAttributes = FontAttributes.Bold,
                        TextColor = Color.Parse("#1A1A1A"),
                        Text = announcement.Users?.FullName ?? "Unknown Admin"
                    };
                    var dateLabel = new Label
                    {
                        FontSize = 12,
                        TextColor = Color.Parse("#6B7280"),
                        Text = announcement.CreatedAt.ToString("MMMM dd, yyyy")
                    };
                    adminDetailsLayout.Children.Add(adminNameLabel);
                    adminDetailsLayout.Children.Add(dateLabel);

                    adminInfoLayout.Children.Add(profileImageBorder);
                    adminInfoLayout.Children.Add(adminDetailsLayout);

                    // Announcement Content
                    var descriptionLabel = new Label
                    {
                        FontSize = 14,
                        TextColor = Color.Parse("#1A1A1A"),
                        LineBreakMode = LineBreakMode.WordWrap,
                        Text = announcement.Content
                    };

                    // Announcement Image
                    var announcementImage = new Image
                    {
                        Aspect = Aspect.AspectFill,
                        HeightRequest = 200,
                        Margin = new Thickness(0, 10, 0, 0),
                        Source = announcement.ImageUrl,
                        IsVisible = !string.IsNullOrEmpty(announcement.ImageUrl)
                    };

                    // Add all elements to the content layout
                    contentLayout.Children.Add(adminInfoLayout);
                    contentLayout.Children.Add(descriptionLabel);
                    contentLayout.Children.Add(announcementImage);

                    // Add elements to card layout
                    cardLayout.Children.Add(deleteButton);
                    cardLayout.Children.Add(contentLayout);

                    // Add the card to the border
                    cardBorder.Content = cardLayout;

                    // Add the card to the container
                    AnnouncementsContainer.Children.Add(cardBorder);
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to load announcements: {ex.Message}\nInner Exception: {ex.InnerException?.Message}", "OK");
            }
        }

        private async Task DeleteAnnouncement(string announcementId) // Changed parameter type to string
        {
            bool confirmed = await DisplayAlert(
                "Delete Announcement",
                "Are you sure you want to delete this announcement?",
                "Confirm",
                "Cancel"
            );

            if (confirmed)
            {
                try
                {
                    await _client
                        .From<Announcement>()
                        .Where(a => a.Id == announcementId) // No change needed here as both are now strings
                        .Delete();

                    await LoadAnnouncements(); // Refresh the list
                    await DisplayAlert("Success", "Announcement deleted successfully", "OK");
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Error", $"Failed to delete announcement: {ex.Message}", "OK");
                }
            }
        }

        private async void AddAnnouncement(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new UploadAnnouncement());
        }
    }
}