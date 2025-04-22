using Microsoft.Maui.Controls;
using Supabase;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using SubdiHub_v1.Components.Models;

namespace SubdiHub_v1.Components.Dashboards.Seller
{
    public partial class Productposted : ContentPage
    {
        private const string SupabaseUrl = "https://cqslvtgwuabdgqxxtzfa.supabase.co";
        private const string SupabaseKey = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6ImNxc2x2dGd3dWFiZGdxeHh0emZhIiwicm9sZSI6ImFub24iLCJpYXQiOjE3NDM0Njg1MzksImV4cCI6MjA1OTA0NDUzOX0.m3gKzHkUMal7WhkA4rF-ymGL_97yQAGdUulGfy3Se7g";
        private readonly Supabase.Client _client;
        public ObservableCollection<Product> Products { get; set; }
        public ICommand DeleteProductCommand { get; }

        public Productposted()
        {
            InitializeComponent();
            _client = new Supabase.Client(SupabaseUrl, SupabaseKey);
            Products = new ObservableCollection<Product>();
            DeleteProductCommand = new Command<Product>(async product => await DeleteProductAsync(product));
            BindingContext = this;

            // Load products when the page appears to ensure fresh data
            Appearing += async (s, e) => await LoadProductsAsync();
        }

        private async Task<string> GetSellerIdAsync()
        {
            var sellerId = await SecureStorage.GetAsync("session_token");
            if (string.IsNullOrEmpty(sellerId))
            {
                await DisplayAlert("Error", "Please log in to view your products.", "OK");
                await Shell.Current.GoToAsync("//Login_page");
            }
            return sellerId;
        }

        private async Task LoadProductsAsync()
        {
            try
            {
                var sellerId = await GetSellerIdAsync();
                if (string.IsNullOrEmpty(sellerId)) return;

                var response = await _client
                    .From<Product>()
                    .Filter("Seller_id", Supabase.Postgrest.Constants.Operator.Equals, sellerId)
                    .Get();

                // Update collection on the main thread to ensure UI updates
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    Products.Clear();
                    foreach (var product in response.Models)
                    {
                        Products.Add(product);
                    }

                    if (Products.Count == 0)
                    {
                        DisplayAlert("Info", "No products found. Start by adding a new product!", "OK");
                    }
                });
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to load products: {ex.Message}", "OK");
            }
        }

        private async Task DeleteProductAsync(Product product)
        {
            try
            {
                var sellerId = await GetSellerIdAsync();
                if (string.IsNullOrEmpty(sellerId)) return;

                if (product.SellerId != sellerId)
                {
                    await DisplayAlert("Error", "You can only delete your own products.", "OK");
                    return;
                }

                bool confirm = await DisplayAlert("Confirm Delete",
                    $"Are you sure you want to delete {product.Name}?",
                    "Yes", "No");

                if (!confirm) return;

                await _client
                    .From<Product>()
                    .Filter("id", Supabase.Postgrest.Constants.Operator.Equals, product.Id)
                    .Delete();

                // Remove from collection on the main thread
                MainThread.BeginInvokeOnMainThread(() => Products.Remove(product));

                await DisplayAlert("Success", "Product deleted successfully!", "OK");
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to delete product: {ex.Message}", "OK");
            }
        }
    }
}