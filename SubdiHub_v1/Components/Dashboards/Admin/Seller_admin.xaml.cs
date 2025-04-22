using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using Supabase;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using SubdiHub_v1.Components.Models;

namespace SubdiHub_v1.Components.Dashboards.Admin
{
    public partial class Seller_admin : ContentPage
    {
        private readonly Supabase.Client _client;
        private const string SupabaseUrl = "https://cqslvtgwuabdgqxxtzfa.supabase.co";
        private const string SupabaseKey = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6ImNxc2x2dGd3dWFiZGdxeHh0emZhIiwicm9sZSI6ImFub24iLCJpYXQiOjE3NDM0Njg1MzksImV4cCI6MjA1OTA0NDUzOX0.m3gKzHkUMal7WhkA4rF-ymGL_97yQAGdUulGfy3Se7g";
        private string _selectedCategory;
        private readonly Dictionary<string, (List<ProductCardViewModel> Products, DateTime Timestamp)> _cache;
        private const int CacheDurationMinutes = 5; // Cache for 5 minutes

        public Command AddToCartCommand { get; }
        public ObservableCollection<ProductCardViewModel> ProductList { get; }

        public Seller_admin()
        {
            InitializeComponent();
            try
            {
                _client = new Supabase.Client(SupabaseUrl, SupabaseKey);
                _cache = new Dictionary<string, (List<ProductCardViewModel>, DateTime)>();
                ProductList = new ObservableCollection<ProductCardViewModel>();
                AddToCartCommand = new Command<ProductCardViewModel>(async (product) => await OnAddToCart(product));
                BindingContext = this;
            }
            catch (Exception ex)
            {
                DisplayAlert("Error", $"Failed to initialize Supabase client: {ex.Message}", "OK");
                _client = null;
            }
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            if (_client == null)
            {
                await DisplayAlert("Error", "Supabase client not initialized.", "OK");
                return;
            }

            var userId = await SecureStorage.GetAsync("session_token");
            if (string.IsNullOrEmpty(userId) || userId == "00000000-0000-0000-0000-000000000000")
            {
                await DisplayAlert("Error", "Please log in to continue.", "OK");
                await Shell.Current.GoToAsync("//Login_page");
                return;
            }

            UserNameLabel.Text = $"Hi, {Preferences.Get("UserFullName", "User")}";
            _selectedCategory = null;
            await LoadVendorProducts();
        }

        private async void OnCategoryTapped(object sender, EventArgs e)
        {
            if (_client == null)
            {
                await DisplayAlert("Error", "Supabase client not initialized.", "OK");
                return;
            }
            if (e is TappedEventArgs tappedEventArgs)
            {
                _selectedCategory = tappedEventArgs.Parameter as string;
                await LoadVendorProducts();
            }
        }

        private async Task LoadVendorProducts()
        {
            try
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    LoadingIndicator.IsRunning = true;
                    NoProductsLabel.IsVisible = false;
                    ProductList.Clear();
                });

                var cacheKey = _selectedCategory ?? "All";

                if (_cache.TryGetValue(cacheKey, out var cachedData) &&
                    (DateTime.UtcNow - cachedData.Timestamp).TotalMinutes < CacheDurationMinutes)
                {
                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        foreach (var product in cachedData.Products)
                        {
                            ProductList.Add(product);
                        }
                        NoProductsLabel.Text = ProductList.Any() ? string.Empty : $"No products found for category: {cacheKey}";
                        NoProductsLabel.IsVisible = !ProductList.Any();
                        LoadingIndicator.IsRunning = false;
                    });
                    return;
                }

                var productQuery = _client
                    .From<Product>()
                    .Select("*")
                    .Order("created_at", Supabase.Postgrest.Constants.Ordering.Descending);

                if (!string.IsNullOrEmpty(_selectedCategory))
                {
                    productQuery = productQuery.Where(p => p.Category == _selectedCategory);
                }

                var productResponse = await productQuery.Get();
                var products = productResponse.Models;

                if (!products.Any())
                {
                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        NoProductsLabel.Text = $"No products found for category: {_selectedCategory ?? "All"}";
                        NoProductsLabel.IsVisible = true;
                        LoadingIndicator.IsRunning = false;
                    });
                    return;
                }

                var sellerIds = products
                    .Select(p => p.SellerId)
                    .Where(id => !string.IsNullOrEmpty(id))
                    .Distinct()
                    .ToList();

                var sellerTask = _client
                    .From<User>()
                    .Filter("User_ID", Supabase.Postgrest.Constants.Operator.In, sellerIds)
                    .Get();

                var settingsTask = _client
                    .From<SettingsDb>()
                    .Filter("user_id", Supabase.Postgrest.Constants.Operator.In, sellerIds)
                    .Get();

                await Task.WhenAll(sellerTask, settingsTask);

                var sellersDict = (await sellerTask).Models.ToDictionary(u => u.UserId, u => u.FullName);
                var profileImageDict = (await settingsTask).Models.ToDictionary(s => s.UserId, s => s.ProfileImage);

                var productViewModels = products.Select(product => new ProductCardViewModel
                {
                    Id = product.Id,
                    ProductImage = product.Image,
                    Name = product.Name,
                    Category = product.Category,
                    Price = product.Price,
                    Description = product.Description,
                    SellerName = sellersDict.GetValueOrDefault(product.SellerId, "Unknown Seller"),
                    ProfileImage = profileImageDict.GetValueOrDefault(product.SellerId, "default_profile.png"),
                    SellerId = product.SellerId
                }).ToList();

                _cache[cacheKey] = (productViewModels, DateTime.UtcNow);

                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    foreach (var product in productViewModels)
                    {
                        ProductList.Add(product);
                    }
                    ProductsCollectionView.ItemsSource = ProductList;
                    LoadingIndicator.IsRunning = false;
                });
            }
            catch (Exception ex)
            {
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    LoadingIndicator.IsRunning = false;
                    await DisplayAlert("Error", $"Failed to load products: {ex.Message}", "OK");
                });
            }
        }

        private async void OnProductImageTapped(object sender, TappedEventArgs e)
        {
            if (e.Parameter is ProductCardViewModel product)
            {
                await Navigation.PushAsync(new ProductDetailPage(product));
            }
        }

        private async Task OnAddToCart(ProductCardViewModel product)
        {
            try
            {
                var userId = await SecureStorage.GetAsync("session_token");
                if (string.IsNullOrEmpty(userId) || userId == "00000000-0000-0000-0000-000000000000")
                {
                    await DisplayAlert("Error", "Please log in to add items to cart.", "OK");
                    await Shell.Current.GoToAsync("//Login_page");
                    return;
                }

                // Check if the product already exists in the cart for the user
                var existingCartItem = await _client
                    .From<CartItem>()
                    .Filter("user_id", Supabase.Postgrest.Constants.Operator.Equals, userId)
                    .Filter("product_id", Supabase.Postgrest.Constants.Operator.Equals, product.Id)
                    .Get();

                if (existingCartItem.Models.Any())
                {
                    var cartItem = existingCartItem.Models.First();
                    cartItem.Quantity += 1;
                    await _client
                        .From<CartItem>()
                        .Where(c => c.Id == cartItem.Id)
                        .Set(c => c.Quantity, cartItem.Quantity)
                        .Update();
                    await DisplayAlert("Success", $"{product.Name} quantity updated to {cartItem.Quantity} in cart.", "OK");
                }
                else
                {
                    var newCartItem = new CartItem
                    {
                        UserId = userId,
                        ProductId = product.Id,
                        SellerId = product.SellerId,
                        ProductName = product.Name,
                        ProductPrice = product.Price,
                        ProductImage = product.ProductImage,
                        CreatedAt = DateTime.UtcNow,
                        Quantity = 1
                    };
                    var response = await _client.From<CartItem>().Insert(newCartItem);
                    newCartItem.Id = response.Models.First().Id; // Get the ID of the newly inserted item

                    // Update the ProductCardViewModel with the cart info
                    product.Quantity = existingCartItem.Models.Any() ? existingCartItem.Models.First().Quantity : 1;
                    product.CartId = existingCartItem.Models.Any() ? existingCartItem.Models.First().Id : newCartItem.Id;
                     }
                }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to add to cart: {ex.Message}", "OK");
            }
        }
    }

    public class ProductCardViewModel
    {
        public string Id { get; set; }
        public string ProductImage { get; set; }
        public string Name { get; set; }
        public string Category { get; set; }
        public decimal Price { get; set; }
        public string Description { get; set; }
        public string SellerName { get; set; }
        public string ProfileImage { get; set; }
        public string SellerId { get; set; }
        public string CartId { get; set; }
        public int Quantity { get; set; }
    }

    // Your existing models are already included correctly
    // [Table("users")] public class User : BaseModel { ... }
    // [Table("settingsdb")] public class SettingsDb : BaseModel { ... }
    // [Table("announcements")] public class Announcement : BaseModel { ... }
    // [Table("bookings")] public class Booking : BaseModel { ... }
    // [Table("products")] public class Product : BaseModel { ... }
    // [Table("cart_items")] public class CartItem : BaseModel { ... }
}