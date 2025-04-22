namespace SubdiHub_v1.Components.Dashboards.Seller;

using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using Supabase;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.IO;
using SubdiHub_v1.Components.Models;

public partial class Addproduct_seller : ContentPage
{
    private const string SupabaseUrl = "https://cqslvtgwuabdgqxxtzfa.supabase.co";
    private const string SupabaseKey = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6ImNxc2x2dGd3dWFiZGdxeHh0emZhIiwicm9sZSI6ImFub24iLCJpYXQiOjE3NDM0Njg1MzksImV4cCI6MjA1OTA0NDUzOX0.m3gKzHkUMal7WhkA4rF-ymGL_97yQAGdUulGfy3Se7g";
    private const string StorageBucket = "product-images";
    private const string NoImageSelectedText = "No image selected";
    private const string ErrorTitle = "Error";
    private const string SuccessTitle = "Success";

    private readonly Supabase.Client client;
    private readonly List<string> categories = new List<string>
    {
        "Food and Beverages",
        "Household Essentials",
        "Fashion and Accessories",
        "Gifts and Crafts",
        "Kids and Babies",
        "Electronics and Gadgets",
        "Beauty and Wellness",
        "Plants and Garden",
        "Others"
    };

    private string uploadedImageUrl;

    public Addproduct_seller()
    {
        InitializeComponent();
        client = new Supabase.Client(SupabaseUrl, SupabaseKey);

        // Force schema fetch to ensure the client has the latest schema
        Task.Run(async () =>
        {
            await client.From<Product>().Get();
        }).Wait();

        CategoryPicker.ItemsSource = categories;
        CategoryPicker.SelectedIndex = 0;
        uploadedImageUrl = null;
    }

    private async void OnSelectImageClicked(object sender, EventArgs e)
    {
        try
        {
            if (!MediaPicker.Default.IsCaptureSupported)
            {
                await ShowErrorAsync("Media picker is not supported on this device.");
                return;
            }

            var result = await MediaPicker.Default.PickPhotoAsync(new MediaPickerOptions
            {
                Title = "Select Product Image"
            });

            if (result == null)
            {
                ResetImageSelection();
                return; // User canceled
            }

            ImageFileNameLabel.Text = $"Selected: {result.FileName}";

            // Convert stream to byte array for upload
            byte[] imageBytes;
            using (var stream = await result.OpenReadAsync())
            using (var memoryStream = new MemoryStream())
            {
                await stream.CopyToAsync(memoryStream);
                imageBytes = memoryStream.ToArray();
            }

            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(result.FileName)}";
            var response = await client.Storage
                .From(StorageBucket)
                .Upload(imageBytes, fileName);

            if (string.IsNullOrEmpty(response))
            {
                await ShowErrorAsync("Failed to upload image to Supabase Storage.");
                ResetImageSelection();
                return;
            }

            uploadedImageUrl = client.Storage
                .From(StorageBucket)
                .GetPublicUrl(fileName);

            if (string.IsNullOrEmpty(uploadedImageUrl))
            {
                await ShowErrorAsync("Failed to retrieve the image URL.");
                ResetImageSelection();
                return;
            }
        }
        catch (Exception ex)
        {
            await ShowErrorAsync($"Failed to select or upload image: {ex.Message}");
            ResetImageSelection();
        }
    }

    private async void OnPostProductClicked(object sender, EventArgs e)
    {
        try
        {
            bool isFormValid = await ValidateFormAsync();
            if (!isFormValid)
                return;

            var sellerId = await ValidateSellerAsync();
            if (sellerId == null)
                return;

            var product = new Product
            {
                Name = ProductNameEntry.Text.Trim(),
                Price = decimal.Parse(PriceEntry.Text), // Already validated
                Description = DescriptionEditor.Text?.Trim(),
                Image = uploadedImageUrl,
                Category = CategoryPicker.SelectedItem.ToString(),
                SellerId = sellerId, // Reverted to SellerId to match likely model state
                CreatedAt = DateTime.UtcNow
            };

            // TODO: Enable RLS on 'products' table for production to restrict inserts to authenticated Vendors.
            await client
                .From<Product>()
                .Insert(product);

            await DisplayAlert(SuccessTitle, "Product posted successfully!", "OK");
            ClearForm();
        }
        catch (Exception ex)
        {
            await ShowErrorAsync($"Failed to post product: {ex.Message}");
        }
    }
        
    private async Task<bool> ValidateFormAsync()
    {
        if (string.IsNullOrWhiteSpace(ProductNameEntry.Text))
        {
            await ShowErrorAsync("Product name is required.");
            return false;
        }

        if (!decimal.TryParse(PriceEntry.Text, out decimal price) || price <= 0)
        {
            await ShowErrorAsync("Please enter a valid price.");
            return false;
        }

        if (CategoryPicker.SelectedItem == null)
        {
            await ShowErrorAsync("Please select a category.");
            return false;
        }

        if (string.IsNullOrEmpty(uploadedImageUrl))
        {
            await ShowErrorAsync("Please select a product image.");
            return false;
        }

        return true;
    }

    private async Task<string> ValidateSellerAsync()
    {
        var sellerId = await SecureStorage.GetAsync("session_token");
        if (string.IsNullOrEmpty(sellerId))
        {
            await ShowErrorAsync("You must be logged in to post a product.");
            await Shell.Current.GoToAsync("//Login_page");
            return null;
        }

        var user = await client
            .From<User>()
            .Filter("User_ID", Supabase.Postgrest.Constants.Operator.Equals, sellerId)
            .Single();

        if (user == null)
        {
            await ShowErrorAsync("Seller account not found.");
            await Shell.Current.GoToAsync("//Login_page");
            return null;
        }

        if (user.Role != "Vendor")
        {
            await ShowErrorAsync("Only users with the Vendor role can post products.");
            await Shell.Current.GoToAsync("//Seller_admin");
            return null;
        }

        return sellerId;
    }

    private void ResetImageSelection()
    {
        ImageFileNameLabel.Text = NoImageSelectedText;
        uploadedImageUrl = null;
    }

    private void ClearForm()
    {
        ProductNameEntry.Text = string.Empty;
        PriceEntry.Text = string.Empty;
        DescriptionEditor.Text = string.Empty;
        ResetImageSelection();
        CategoryPicker.SelectedIndex = 0;
    }

    private async Task ShowErrorAsync(string message)
    {
        await DisplayAlert(ErrorTitle, message, "OK");
    }
}