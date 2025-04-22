using Supabase;
using BCrypt.Net;
using System;
using System.Threading.Tasks;
using SubdiHub_v1.Components.Authentication.Login;
using Supabase.Postgrest.Models;
using Supabase.Postgrest.Attributes;
using SubdiHub_v1.Components.Models;
namespace SubdiHub_v1.Components.Authentication.Signup
{
    public partial class Signup_page : ContentPage
    {
        private readonly Supabase.Client client;

        public Signup_page()
        {
            InitializeComponent();
            client = new Supabase.Client("https://cqslvtgwuabdgqxxtzfa.supabase.co", "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6ImNxc2x2dGd3dWFiZGdxeHh0emZhIiwicm9sZSI6ImFub24iLCJpYXQiOjE3NDM0Njg1MzksImV4cCI6MjA1OTA0NDUzOX0.m3gKzHkUMal7WhkA4rF-ymGL_97yQAGdUulGfy3Se7g");
        }

        private async void OnLoginTapped(object sender, TappedEventArgs e)
        {
            await Navigation.PushAsync(new Login_page());
        }

        private void OnCheckBoxCheckedChanged(object sender, CheckedChangedEventArgs e)
        {
            if (sender is CheckBox selectedCheckBox && selectedCheckBox.IsChecked)
            {
                foreach (var checkBox in new[] { ResidentCheckBox, VendorsCheckBox, AdminCheckBox })
                {
                    if (checkBox != selectedCheckBox)
                        checkBox.IsChecked = false;
                }
            }
        }
        private async void OnPageAppearing(object sender, EventArgs e)
        {
            // Title slide-in and fade animation
            TitleLabel.Opacity = 0;
            TitleLabel.TranslationY = 20;
            await Task.WhenAll(
                TitleLabel.FadeTo(1, 500, Easing.CubicOut),
                TitleLabel.TranslateTo(0, 0, 500, Easing.CubicOut)
            );
        }
        private async void OnPasswordEyeButtonClicked(object sender, EventArgs e)
        {
            // Scale animation for button press
            await PasswordEyeButton.ScaleTo(0.9, 100, Easing.Linear);
            await PasswordEyeButton.ScaleTo(1, 100, Easing.Linear);

            // Toggle password visibility
            PasswordEntry.IsPassword = !PasswordEntry.IsPassword;
        }
        private async void OnConfirmPasswordEyeButtonClicked(object sender, EventArgs e)
        {
            // Scale animation for button press
            await ConfirmPasswordEyeButton.ScaleTo(0.9, 100, Easing.Linear);
            await ConfirmPasswordEyeButton.ScaleTo(1, 100, Easing.Linear);
            // Toggle password visibility
            ConfirmPasswordEntry.IsPassword = !ConfirmPasswordEntry.IsPassword;
        }

        private async void SignUpButton_Clicked(object sender, EventArgs e)
        {
            try
            {
                // Get user input
                string fullName = FullNameEntry.Text?.Trim();
                string username = UsernameEntry.Text?.Trim();
                string email = EmailEntry.Text?.Trim();
                string contact = ContactEntry.Text?.Trim();
                string street = StreetPicker.SelectedItem?.ToString();
                string block = BlockEntry.Text?.Trim();
                string lot = LotEntry.Text?.Trim();
                string password = PasswordEntry.Text;
                string confirmPassword = ConfirmPasswordEntry.Text;

                string role = "";
                if (ResidentCheckBox.IsChecked)
                    role = "Resident";
                else if (VendorsCheckBox.IsChecked)
                    role = "Vendor";
                else if (AdminCheckBox.IsChecked)
                    role = "Admin";

                // --- VALIDATIONS ---

                // Check empty fields
                if (string.IsNullOrEmpty(fullName) || string.IsNullOrEmpty(username) || string.IsNullOrEmpty(email) ||
                    string.IsNullOrEmpty(contact) || string.IsNullOrEmpty(block) || string.IsNullOrEmpty(lot) ||
                    string.IsNullOrEmpty(password) || string.IsNullOrEmpty(confirmPassword) || string.IsNullOrEmpty(role) || string.IsNullOrEmpty(street))
                {
                    await DisplayAlert("Error", "Please fill in all fields.", "OK");
                    return;
                }

                // Full name should have at least two words
                if (fullName.Split(' ').Length < 2)
                {
                    await DisplayAlert("Error", "Please enter your full name (first and last name).", "OK");
                    return;
                }

                // Email format check
                if (!System.Text.RegularExpressions.Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                {
                    await DisplayAlert("Error", "Invalid email format.", "OK");
                    return;
                }

                // Contact number must be digits only
                if (!System.Text.RegularExpressions.Regex.IsMatch(contact, @"^\d+$"))
                {
                    await DisplayAlert("Error", "Contact number must contain digits only.", "OK");
                    return;
                }

                // Passwords match
                if (password != confirmPassword)
                {
                    await DisplayAlert("Error", "Passwords do not match.", "OK");
                    return;
                }

                // Password length
                if (password.Length < 8)
                {
                    await DisplayAlert("Error", "Password must be at least 8 characters long.", "OK");
                    return;
                }
                // Hash password before storing
                string hashedPassword = BCrypt.Net.BCrypt.HashPassword(password);

                // Create user object
                var user = new User
                {
                    FullName = fullName,
                    Username = username,
                    Email = email,
                    Contact = contact,
                    Street = street,
                    Block = block,
                    Lot = lot,
                    PasswordHash = hashedPassword,
                    Role = role,
                    CreatedAt = DateTime.UtcNow
                };

                // Insert the user into Supabase
                var result = await client.From<User>().Insert(new[] { user });

                if (result != null)
                {
                    await DisplayAlert("Success", "Account Created!", "OK");

                    // Clear form fields
                    FullNameEntry.Text = "";
                    UsernameEntry.Text = "";
                    EmailEntry.Text = "";
                    ContactEntry.Text = "";
                    BlockEntry.Text = "";
                    LotEntry.Text = "";
                    PasswordEntry.Text = "";
                    ConfirmPasswordEntry.Text = "";
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", "Failed to create account: " + ex.Message, "OK");
                Console.WriteLine("Exception: " + ex.Message);
                Console.WriteLine("Stack Trace: " + ex.StackTrace);
            }
        }
    }
}