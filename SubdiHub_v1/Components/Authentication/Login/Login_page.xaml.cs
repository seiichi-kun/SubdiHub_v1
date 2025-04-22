using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using Supabase;
using System;
using System.Threading.Tasks;
using BCrypt.Net;
using SubdiHub_v1.Components.Models;
using SubdiHub_v1.Components.Authentication.Signup;

namespace SubdiHub_v1.Components.Authentication.Login
{
    public partial class Login_page : ContentPage
    {
        private readonly Supabase.Client _client;
        private const string SupabaseUrl = "https://cqslvtgwuabdgqxxtzfa.supabase.co";
        private const string SupabaseKey = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6ImNxc2x2dGd3dWFiZGdxeHh0emZhIiwicm9sZSI6ImFub24iLCJpYXQiOjE3NDM0Njg1MzksImV4cCI6MjA1OTA0NDUzOX0.m3gKzHkUMal7WhkA4rF-ymGL_97yQAGdUulGfy3Se7g";

        public Login_page()
        {
            InitializeComponent();
            _client = new Supabase.Client(SupabaseUrl, SupabaseKey);
        }

        // Handle page appearing to trigger animations
        private async void OnPageAppearing(object sender, EventArgs e)
        {
            // Logo fade-in animation
            LogoImage.Opacity = 0;
            await LogoImage.FadeTo(0.9, 1000, Easing.CubicOut);

            // Title slide-in and fade animation
            TitleLabel.Opacity = 0;
            TitleLabel.TranslationY = 20;
            await Task.WhenAll(
                TitleLabel.FadeTo(1, 500, Easing.CubicOut),
                TitleLabel.TranslateTo(0, 0, 500, Easing.CubicOut)
            );
        }

        // Handle eye button click for password visibility toggle and scale animation
        private async void OnEyeButtonClicked(object sender, EventArgs e)
        {
            // Scale animation for button press
            await EyeButton.ScaleTo(0.9, 100, Easing.Linear);
            await EyeButton.ScaleTo(1, 100, Easing.Linear);

            // Toggle password visibility
            PasswordEntry.IsPassword = !PasswordEntry.IsPassword;
        }

        // Handle login button click with animation and authentication
        private async void OnLoginClicked(object sender, EventArgs e)
        {
            // Scale animation for button press
            await LoginButton.ScaleTo(0.95, 100, Easing.Linear);
            await LoginButton.ScaleTo(1, 100, Easing.Linear);

            // Validate input
            if (!ValidateInput(out string username, out string password))
            {
                await DisplayAlert("Login Failed", "Please enter both username and password.", "OK");
                return;
            }

            try
            {
                // Fetch user from Supabase
                var user = await GetUserByUsername(username);
                if (user == null)
                {
                    await DisplayAlert("Login Failed", "User not found.", "OK");
                    return;
                }

                // Verify password
                if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
                {
                    await DisplayAlert("Login Failed", "Incorrect password.", "OK");
                    return;
                }

                // Store session and navigate
                await StoreUserSession(user);
                await DisplayAlert("Login Successful", $"Welcome, {user.FullName}!", "OK");
                await NavigateToDashboard(user.Role);
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Login failed: {ex.Message}", "OK");
            }
        }

        // Validate username and password input
        private bool ValidateInput(out string username, out string password)
        {
            username = UsernameEntry.Text?.Trim() ?? string.Empty;
            password = PasswordEntry.Text?.Trim() ?? string.Empty;
            return !string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(password);
        }

        // Fetch user by username from Supabase
        private async Task<User> GetUserByUsername(string username)
        {
            return await _client
                .From<User>()
                .Where(x => x.Username == username)
                .Single();
        }

        // Store user session in preferences and secure storage
        private async Task StoreUserSession(User user)
        {
            var preferences = new (string Key, string Value)[]
            {
                ("IsLoggedIn", "true"),
                ("UserFullName", user.FullName),
                ("UserRole", user.Role),
                ("UserEmail", user.Email)
            };

            foreach (var pref in preferences)
            {
                Preferences.Set(pref.Key, pref.Value);
            }

            await SecureStorage.SetAsync("session_token", user.UserId);
        }

        // Navigate to role-based dashboard
        private async Task NavigateToDashboard(string role)
        {
            var routes = new Dictionary<string, string>
            {
                ["Admin"] = "//Home_admin1",
                ["Vendor"] = "//Home_seller",
                ["Resident"] = "//Home_resident"
            };

            if (routes.TryGetValue(role, out var route))
            {
                await Shell.Current.GoToAsync(route);
            }
            else
            {
                await DisplayAlert("Error", "Invalid role detected.", "OK");
            }
        }

        // Handle sign-up tap
        private async void OnSignupTapped(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new Signup_page());
        }

        // Handle forgot password tap (placeholder for future implementation)
        private async void OnForgotPasswordTapped(object sender, EventArgs e)
        {
            // TODO: Implement forgot password logic (e.g., navigate to reset password page)
            await DisplayAlert("Forgot Password", "This feature is not yet implemented.", "OK");
        }
    }
}