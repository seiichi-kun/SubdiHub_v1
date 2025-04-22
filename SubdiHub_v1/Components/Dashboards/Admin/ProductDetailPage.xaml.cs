using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using Supabase;
using System;
using System.Threading.Tasks;
using SubdiHub_v1.Components.Models;

namespace SubdiHub_v1.Components.Dashboards.Admin
{
    public partial class ProductDetailPage : ContentPage
    {
        private readonly Supabase.Client _client;
        private const string SupabaseUrl = "https://cqslvtgwuabdgqxxtzfa.supabase.co";
        private const string SupabaseKey = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6ImNxc2x2dGd3dWFiZGdxeHh0emZhIiwicm9sZSI6ImFub24iLCJpYXQiOjE3NDM0Njg1MzksImV4cCI6MjA1OTA0NDUzOX0.m3gKzHkUMal7WhkA4rF-ymGL_97yQAGdUulGfy3Se7g";
        private ProductCardViewModel _product;

        public ProductDetailPage(ProductCardViewModel product)
        {
            InitializeComponent();

            try
            {
                _client = new Supabase.Client(SupabaseUrl, SupabaseKey);
                _product = product ?? new ProductCardViewModel { Quantity = 1 }; // Ensure default quantity
                BindingContext = _product;

                // Wire up buttons
                var decreaseButton = this.FindByName<Button>("DecreaseButton");
                var increaseButton = this.FindByName<Button>("IncreaseButton");
                var numberLabel = this.FindByName<Label>("number");
                var addToCartButton = this.FindByName<Button>("AddToCartButton");

                void UpdateLabel()
                {
                    if (numberLabel != null)
                        numberLabel.Text = _product.Quantity.ToString();
                }

                if (decreaseButton != null)
                {
                    decreaseButton.Clicked += (s, e) =>
                    {
                        if (_product.Quantity > 1)
                        {
                            _product.Quantity--;
                            UpdateLabel();
                        }
                    };
                }

                if (increaseButton != null)
                {
                    increaseButton.Clicked += (s, e) =>
                    {
                        _product.Quantity++;
                        UpdateLabel();
                    };
                }

                if (addToCartButton != null)
                {
                    addToCartButton.Clicked += OnAddToCartClicked;
                }

                // Initialize quantity label
                UpdateLabel();
            }
            catch (Exception ex)
            {
                DisplayAlert("Error", $"Failed to initialize Supabase client: {ex.Message}", "OK");
                _client = null;
            }
        }

        private void OnDecreaseQuantity(object sender, EventArgs e)
        {
            if (_product.Quantity > 1)
            {
                _product.Quantity--;
                System.Diagnostics.Debug.WriteLine($"Decreased quantity to: {_product.Quantity}");
            }
        }

        private void OnIncreaseQuantity(object sender, EventArgs e)
        {
            _product.Quantity++;
            System.Diagnostics.Debug.WriteLine($"Increased quantity to: {_product.Quantity}");
        }

        private async void OnAddToCartClicked(object sender, EventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"Before AddToCart - Quantity: {_product.Quantity}");
            await OnAddToCart(_product);
            System.Diagnostics.Debug.WriteLine($"After AddToCart - Quantity: {_product.Quantity}");
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

                string cartId = null;
                int finalQuantity = 0;

                if (existingCartItem.Models.Any())
                {
                    var cartItem = existingCartItem.Models.First();
                    cartItem.Quantity += product.Quantity; // Add the user-selected quantity
                    finalQuantity = cartItem.Quantity;
                    await _client
                        .From<CartItem>()
                        .Where(c => c.Id == cartItem.Id)
                        .Set(c => c.Quantity, cartItem.Quantity)
                        .Update();
                    cartId = cartItem.Id;
                    await DisplayAlert("Success", $"{product.Name} quantity updated to {finalQuantity} in cart.", "OK");
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
                        Quantity = product.Quantity // Use the user-selected quantity
                    };
                    var response = await _client.From<CartItem>().Insert(newCartItem);
                    newCartItem.Id = response.Models.First().Id; // Assign the generated ID
                    cartId = newCartItem.Id;
                    finalQuantity = newCartItem.Quantity;
                    await DisplayAlert("Success", $"{product.Name} added to cart with quantity {finalQuantity}.", "OK");
                }

                // Update the ProductCardViewModel with the cart info
                product.CartId = cartId;
                // Reset the UI quantity to 1 after adding to cart to start fresh for the next addition
                product.Quantity = 1;
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to add to cart: {ex.Message}", "OK");
            }
        }
    }
}