namespace SubdiHub_v1.Components.Dashboards.Resident;

using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using Supabase;
using System;
using System.Threading.Tasks;
using SubdiHub_v1.Components.Models;
using SubdiHub_v1.Components.Authentication.Login;

public partial class Account_resident : ContentPage
{
    private readonly Supabase.Client client;
    private string userId;
    private User currentUser;
    private bool isDataLoaded; // Prevent duplicate loading

    public Command EmergencyContactCommand { get; }

    public Account_resident()
    {
        InitializeComponent();

        // Initialize the Supabase client
        client = new Supabase.Client(
            "https://cqslvtgwuabdgqxxtzfa.supabase.co",
            "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6ImNxc2x2dGd3dWFiZGdxeHh0emZhIiwicm9sZSI6ImFub24iLCJpYXQiOjE3NDM0Njg1MzksImV4cCI6MjA1OTA0NDUzOX0.m3gKzHkUMal7WhkA4rF-ymGL_97yQAGdUulGfy3Se7g"
        );

        isDataLoaded = false;

        // Initialize the Emergency Contact command
        EmergencyContactCommand = new Command(async () => await NavigateToEmergencyContact());

        // Set the BindingContext
        BindingContext = this;
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
            // Fetch user and settings in parallel for efficiency
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
            var userSettings = await settingsTask;

            if (currentUser != null)
            {
                // Populate UI with name and role
                NameLabel.Text = string.IsNullOrEmpty(currentUser.FullName) ? "Unknown" : currentUser.FullName;
                RoleLabel.Text = string.IsNullOrEmpty(currentUser.Role) ? "No Role Assigned" : currentUser.Role;

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

    private async Task NavigateToEmergencyContact()
    {
        try
        {
            await Shell.Current.GoToAsync("EmergencyContact");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to navigate to Emergency Contact: {ex.Message}", "OK");
        }
    }

    private async void Edit_profile(object sender, EventArgs e)
    {
        try
        {
            await Shell.Current.GoToAsync("Edit_profile_Re"); // Adjusted for resident
            // Refresh data after editing
            isDataLoaded = false;
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to navigate to Edit Profile: {ex.Message}", "OK");
        }
    }

    private async void OnProfileImageTapped(object sender, EventArgs e)
    {
        try
        {
            await Shell.Current.GoToAsync("Edit_profile_R"); // Adjusted for resident
            // Refresh data after editing
            isDataLoaded = false;
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to navigate to Edit Profile: {ex.Message}", "OK");
        }
    }

    private async void Logout_btn(object sender, EventArgs e)
    {
        try
        {
            // Clear user session data
            Preferences.Remove("user_id");
            SecureStorage.Remove("session_token");

            // Reset the navigation stack by setting a new root page
            Application.Current.MainPage = new AppShell();

            // Navigate to LoginPage
            await Shell.Current.GoToAsync("Login_page");
        }
        catch (Exception ex)
        {
            await Application.Current.MainPage.DisplayAlert(
                "Logout Failed",
                "An error occurred while logging out: {ex.Message}",
                "OK"
            );
        }
    }
}