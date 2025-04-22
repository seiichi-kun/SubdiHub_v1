using Microsoft.Maui.Controls;
using SubdiHub_v1.Components.Authentication.Login;
using SubdiHub_v1.Components.Authentication.Signup;

namespace SubdiHub_v1
{
    public partial class Welcome_page : ContentPage
    {
        public Welcome_page()
        {
            InitializeComponent();
        }

        // Handle page appearing to trigger animations
        private async void OnPageAppearing(object sender, EventArgs e)
        {
            // Logo fade-in animation
            LogoImage.Opacity = 0;
            await LogoImage.FadeTo(0.9, 1000, Easing.CubicOut);

            // Header slide-in and fade animation
            HeaderLabel.Opacity = 0;
            HeaderLabel.TranslationY = 20;
            await Task.WhenAll(
                HeaderLabel.FadeTo(1, 500, Easing.CubicOut),
                HeaderLabel.TranslateTo(0, 0, 500, Easing.CubicOut)
            );

            // Subtitle slide-in and fade animation
            SubtitleLabel.Opacity = 0;
            SubtitleLabel.TranslationY = 20;
            await Task.WhenAll(
                SubtitleLabel.FadeTo(1, 500, Easing.CubicOut),
                SubtitleLabel.TranslateTo(0, 0, 500, Easing.CubicOut)
            );
        }

        // Handle sign-in button click with animation
        private async void OnLoginClicked(object sender, EventArgs e)
        {
            // Scale animation for button press
            await SignInButton.ScaleTo(0.95, 100, Easing.Linear);
            await SignInButton.ScaleTo(1, 100, Easing.Linear);

            // Navigate to login page
            await Navigation.PushAsync(new Login_page());
        }

        // Handle create account button click with animation
        private async void OnSignupClicked(object sender, EventArgs e)
        {
            // Scale animation for button press
            await CreateAccountButton.ScaleTo(0.95, 100, Easing.Linear);
            await CreateAccountButton.ScaleTo(1, 100, Easing.Linear);

            // Navigate to signup page
            await Navigation.PushAsync(new Signup_page());
        }
    }
}