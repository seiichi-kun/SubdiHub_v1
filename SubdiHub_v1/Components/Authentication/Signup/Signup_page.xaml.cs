using Supabase;
using BCrypt.Net;
using System;
using System.Threading.Tasks;
using SubdiHub_v1.Components.Authentication.Login;
using Supabase.Postgrest.Models;
using Supabase.Postgrest.Attributes;
using SubdiHub_v1.Components.Models;
using Microsoft.Extensions.Logging;

namespace SubdiHub_v1.Components.Authentication.Signup
{
    public partial class Signup_page : ContentPage
    {
        private readonly Supabase.Client client;

        public Signup_page()
        {
            InitializeComponent();
            client = new Supabase.Client("https://cqslvtgwuabdgqxxtzfa.supabase.co", "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6ImNxc2x2dGd3dWFiZGdxeHh0emZhIiwicm9sZSI6ImFub24iLCJpYXQiOjE3NDM0Njg1MzksImV4cCI6MjA1OTA0NDUzOX0.m3gKzHkUMal7WhkA4rF-ymGL_97yQAGdUulGfy3Se7g");
            InitializeStreetPicker();
        }

        private void InitializeStreetPicker()
        {
            var streets = new[] { "1st", "2nd", "3rd" };
            foreach (var street in streets)
            {
                StreetPicker.Items.Add(street);
            }
            StreetPicker.SelectedIndex = -1; // No default selection
            StreetPicker.SelectedIndexChanged += (s, e) =>
            {
                if (StreetPicker.SelectedIndex != -1)
                {
                    StreetPicker.SelectedItem = StreetPicker.Items[StreetPicker.SelectedIndex];
                }
            };
        }

        private async void OnLoginTapped(object sender, TappedEventArgs e)
        {
            await Navigation.PushAsync(new Login_page());
        }

        private async void OnPageAppearing(object sender, EventArgs e)
        {
            await Task.WhenAll(
                TitleLabel.FadeTo(1, 400, Easing.SinOut),
                TitleLabel.TranslateTo(0, 0, 400, Easing.SinOut)
            );
        }

        private async void OnPasswordEyeIconTapped(object sender, EventArgs e)
        {
            await PasswordEyeIcon.ScaleTo(0.9, 80);
            await PasswordEyeIcon.ScaleTo(1, 80);
            PasswordEntry.IsPassword = !PasswordEntry.IsPassword;
            PasswordEyeIcon.Source = PasswordEntry.IsPassword ? "show.png" : "hide.png";
        }

        private async void OnConfirmPasswordEyeIconTapped(object sender, EventArgs e)
        {
            await ConfirmPasswordEyeIcon.ScaleTo(0.9, 80);
            await ConfirmPasswordEyeIcon.ScaleTo(1, 80);
            ConfirmPasswordEntry.IsPassword = !ConfirmPasswordEntry.IsPassword;
            ConfirmPasswordEyeIcon.Source = ConfirmPasswordEntry.IsPassword ? "show.png" : "hide.png";
        }
        private async void AdminRadioButton_CheckedChanged(object sender, CheckedChangedEventArgs e)
        {
            if (e.Value)
            {
                // Prompt user for developer password
                string result = await DisplayPromptAsync(
                    "Developer Password Required",
                    "Creating an Admin account requires developer authorization.\nPlease enter the password to proceed.",
                    "OK", "Cancel", "Enter Password", -1, Keyboard.Text);

                if (result == "HoneyT123")
                {
                    AdminRadioButton.IsChecked = true;
                }
                else
                {
                    await DisplayAlert("Access Denied", "Incorrect developer password. Please contact the developer.", "OK");

                    // Revert back to unselected state
                    AdminRadioButton.IsChecked = false;
                }
            }
        }

        private async void SignUpButton_Clicked(object sender, EventArgs e)
        {
            try
            {
                string fullName = FullNameEntry.Text?.Trim();
                string username = UsernameEntry.Text?.Trim();
                string email = EmailEntry.Text?.Trim();
                string contact = ContactEntry.Text?.Trim();
                string street = StreetPicker.SelectedItem?.ToString();
                string block = BlockEntry.Text?.Trim();
                string lot = LotEntry.Text?.Trim();
                string password = PasswordEntry.Text;
                string confirmPassword = ConfirmPasswordEntry.Text;

                string role = ResidentRadioButton.IsChecked ? "Resident" :
                              VendorsRadioButton.IsChecked ? "Vendor" :
                              AdminRadioButton.IsChecked ? "Admin" : "";

                // Validations
                if (string.IsNullOrEmpty(fullName) || string.IsNullOrEmpty(username) || string.IsNullOrEmpty(email) ||
                    string.IsNullOrEmpty(contact) || string.IsNullOrEmpty(block) || string.IsNullOrEmpty(lot) ||
                    string.IsNullOrEmpty(password) || string.IsNullOrEmpty(confirmPassword) || string.IsNullOrEmpty(role) || string.IsNullOrEmpty(street))
                {
                    await DisplayAlert("Error", "Please fill in all fields.", "OK");
                    return;
                }

                if (fullName.Split(' ').Length < 2)
                {
                    await DisplayAlert("Error", "Please enter your full name (first and last name).", "OK");
                    return;
                }

                if (!System.Text.RegularExpressions.Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                {
                    await DisplayAlert("Error", "Invalid email format.", "OK");
                    return;
                }

                if (!System.Text.RegularExpressions.Regex.IsMatch(contact, @"^\d+$"))
                {
                    await DisplayAlert("Error", "Contact number must contain digits only.", "OK");
                    return;
                }

                if (password != confirmPassword)
                {
                    await DisplayAlert("Error", "Passwords do not match.", "OK");
                    return;
                }

                if (password.Length < 8)
                {
                    await DisplayAlert("Error", "Password must be at least 8 characters long.", "OK");
                    return;
                }

                string hashedPassword = BCrypt.Net.BCrypt.HashPassword(password);

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

                var result = await client.From<User>().Insert(new[] { user });

                if (result != null)
                {
                    await DisplayAlert("Success", "Account Created!", "OK");
                    FullNameEntry.Text = "";
                    UsernameEntry.Text = "";
                    EmailEntry.Text = "";
                    ContactEntry.Text = "";
                    BlockEntry.Text = "";
                    LotEntry.Text = "";
                    PasswordEntry.Text = "";
                    ConfirmPasswordEntry.Text = "";
                    StreetPicker.SelectedIndex = -1;
                    ResidentRadioButton.IsChecked = false;
                    VendorsRadioButton.IsChecked = false;
                    AdminRadioButton.IsChecked = false;
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", "Failed to create account: " + ex.Message, "OK");
            }
        }
    }
}