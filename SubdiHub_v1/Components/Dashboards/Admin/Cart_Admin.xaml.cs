using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using Supabase;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using SubdiHub_v1.Components.Models;
using Microsoft.Maui.Controls.Shapes;

namespace SubdiHub_v1.Components.Dashboards.Admin
{
    public partial class Cart_Admin : ContentPage
    {
        private readonly Supabase.Client _client;
        private const string SupabaseUrl = "https://cqslvtgwuabdgqxxtzfa.supabase.co";
        private const string SupabaseKey = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6ImNxc2x2dGd3dWFiZGdxeHh0emZhIiwicm9sZSI6ImFub24iLCJpYXQiOjE3NDM0Njg1MzksImV4cCI6MjA1OTA0NDUzOX0.m3gKzHkUMal7WhkA4rF-ymGL_97yQAGdUulGfy3Se7g";

        public Cart_Admin()
        {
            InitializeComponent();
            try
            {
                _client = new Supabase.Client(SupabaseUrl, SupabaseKey);
                InitializeCheckoutButton();
            }
            catch (Exception ex)
            {
                DisplayAlert("Error", $"Failed to initialize Supabase client: {ex.Message}", "OK");
                _client = null;
            }
        }


        private async Task<IReadOnlyList<ProductCardViewModel>> GetCartItems(string userId)
        {
            try
            {
                if (_client == null)
                {
                    throw new Exception("Supabase client not initialized.");
                }

                var response = await _client
                    .From<CartItem>()
                    .Where(x => x.UserId == userId)
                    .Get();

                var cartItems = response.Models;

                var sellerIds = cartItems.Select(c => c.SellerId).Distinct().ToList();
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

                return cartItems.Select(item => new ProductCardViewModel
                {
                    Id = item.ProductId,
                    ProductImage = item.ProductImage,
                    Name = item.ProductName,
                    Price = item.ProductPrice,
                    SellerName = sellersDict.GetValueOrDefault(item.SellerId, "Unknown Seller"),
                    ProfileImage = profileImageDict.GetValueOrDefault(item.SellerId, "default_profile.png"),
                    SellerId = item.SellerId,
                    CartId = item.Id,
                    Quantity = item.Quantity
                }).ToList().AsReadOnly();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to fetch cart items: {ex.Message}", "OK");
                return new List<ProductCardViewModel>().AsReadOnly();
            }
        }

        private async Task UpdateQuantity(string cartItemId, int newQuantity, string userId)
        {
            try
            {
                if (_client == null)
                {
                    throw new Exception("Supabase client not initialized.");
                }

                await _client
                    .From<CartItem>()
                    .Where(x => x.Id == cartItemId && x.UserId == userId)
                    .Set(x => x.Quantity, newQuantity)
                    .Update();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to update quantity: {ex.Message}", "OK");
            }
        }

        private async Task RemoveFromCart(ProductCardViewModel product, string userId)
        {
            try
            {
                if (_client == null)
                {
                    throw new Exception("Supabase client not initialized.");
                }

                await _client
                    .From<CartItem>()
                    .Where(x => x.UserId == userId && x.ProductId == product.Id)
                    .Delete();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to remove item: {ex.Message}", "OK");
            }
        }

        private void UpdatePricingSummary(IReadOnlyList<ProductCardViewModel> cartItems)
        {
            if (cartItems == null || !cartItems.Any())
            {
                PricingSummary.IsVisible = false;
                CheckoutButton.IsVisible = false;
                return;
            }

            decimal subtotal = cartItems.Sum(item => item.Price * item.Quantity);
            decimal deliveryFee = 10.00m;
            decimal total = subtotal + deliveryFee;

            SubtotalLabel.Text = $"₱{subtotal:N2}";
            DeliveryLabel.Text = $"₱{deliveryFee:N2}";
            TotalItemsLabel.Text = $"TOTAL ({cartItems.Sum(item => item.Quantity)} items)";
            TotalLabel.Text = $"₱{total:N2}";

            PricingSummary.IsVisible = true;
            CheckoutButton.IsVisible = true;
        }

        private async Task LoadCartItems()
        {
            try
            {
                var userId = await SecureStorage.GetAsync("session_token");
                if (string.IsNullOrEmpty(userId) || userId == "00000000-0000-0000-0000-000000000000")
                {
                    await DisplayAlert("Error", "Please log in to view your cart.", "OK");
                    await Shell.Current.GoToAsync("//Login_page");
                    EmptyCartLabel.IsVisible = true;
                    CartContainer.IsVisible = false;
                    PricingSummary.IsVisible = false;
                    CheckoutButton.IsVisible = false;
                    return;
                }

                var cartItems = await GetCartItems(userId);
                if (!cartItems.Any())
                {
                    EmptyCartLabel.IsVisible = true;
                    CartContainer.IsVisible = false;
                    PricingSummary.IsVisible = false;
                    CheckoutButton.IsVisible = false;
                    return;
                }

                EmptyCartLabel.IsVisible = false;
                CartContainer.IsVisible = true;
                CartContainer.Children.Clear();

                var groupedBySeller = cartItems.GroupBy(item => item.SellerId);
                foreach (var sellerGroup in groupedBySeller)
                {
                    var sellerName = sellerGroup.First().SellerName;
                    var frame = new Frame
                    {
                        Padding = 15,
                        Margin = new Thickness(10, 5),
                        BackgroundColor = Colors.White,
                        CornerRadius = 20,
                        HasShadow = false,
                        BorderColor = Color.Parse("#E8ECEF")
                    };

                    var stackLayout = new VerticalStackLayout { Spacing = 10 };
                    stackLayout.Children.Add(new Label
                    {
                        Text = $"Seller: {sellerName}",
                        FontSize = 18,
                        FontAttributes = FontAttributes.Bold,
                        TextColor = Color.Parse("#121212")
                    });

                    foreach (var item in sellerGroup)
                    {
                        var productLayout = new HorizontalStackLayout { Spacing = 10, Margin = new Thickness(0, 5, 0, 0) };
                        productLayout.Children.Add(new Image
                        {
                            Source = item.ProductImage,
                            HeightRequest = 50,
                            WidthRequest = 50,
                            Aspect = Aspect.AspectFill,
                            Clip = new RoundRectangleGeometry(new CornerRadius(10), new Rect(0, 0, 50, 50))
                        });

                        var detailsLayout = new VerticalStackLayout
                        {
                            Spacing = 5,
                            VerticalOptions = LayoutOptions.Center,
                            Children =
                            {
                                new Label
                                {
                                    Text = item.Name,
                                    FontSize = 14,
                                    TextColor = Color.Parse("#121212"),
                                    FontAttributes = FontAttributes.Bold
                                },
                                new Label
                                {
                                    Text = $"₱{item.Price:N2}",
                                    FontSize = 12,
                                    TextColor = Color.Parse("#2D6A4F")
                                }
                            }
                        };

                        var quantityLayout = new HorizontalStackLayout { Spacing = 5, VerticalOptions = LayoutOptions.Center };
                        var decreaseButton = new Button
                        {
                            Text = "-",
                            FontSize = 12,
                            BackgroundColor = Color.Parse("#FF4D4D"),
                            TextColor = Colors.White,
                            CornerRadius = 5,
                            WidthRequest = 30,
                            HeightRequest = 30,
                            Padding = new Thickness(0)
                        };
                        decreaseButton.Command = new Command(async () => await UpdateQuantity(item, -1));

                        var quantityLabel = new Label
                        {
                            Text = item.Quantity.ToString(),
                            FontSize = 12,
                            TextColor = Color.Parse("#121212"),
                            VerticalOptions = LayoutOptions.Center,
                            HorizontalOptions = LayoutOptions.Center,
                            WidthRequest = 30
                        };

                        var increaseButton = new Button
                        {
                            Text = "+",
                            FontSize = 12,
                            BackgroundColor = Color.Parse("#2D6A4F"),
                            TextColor = Colors.White,
                            CornerRadius = 5,
                            WidthRequest = 30,
                            HeightRequest = 30,
                            Padding = new Thickness(0)
                        };
                        increaseButton.Command = new Command(async () => await UpdateQuantity(item, 1));

                        quantityLayout.Children.Add(decreaseButton);
                        quantityLayout.Children.Add(quantityLabel);
                        quantityLayout.Children.Add(increaseButton);

                        detailsLayout.Children.Add(quantityLayout);
                        productLayout.Children.Add(detailsLayout);

                        var deleteButton = new Button
                        {
                            Text = "Delete",
                            FontSize = 12,
                            BackgroundColor = Color.Parse("#FF4D4D"),
                            TextColor = Colors.White,
                            CornerRadius = 5,
                            Padding = new Thickness(10, 5),
                            VerticalOptions = LayoutOptions.Center,
                            HorizontalOptions = LayoutOptions.EndAndExpand
                        };
                        deleteButton.Command = new Command(async () => await DeleteFromCart(item));
                        productLayout.Children.Add(deleteButton);

                        stackLayout.Children.Add(productLayout);
                    }

                    frame.Content = stackLayout;
                    CartContainer.Children.Add(frame);
                }

                UpdatePricingSummary(cartItems);
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to load cart: {ex.Message}", "OK");
                EmptyCartLabel.IsVisible = true;
                CartContainer.IsVisible = false;
                PricingSummary.IsVisible = false;
                CheckoutButton.IsVisible = false;
            }
        }

        private async Task UpdateQuantity(ProductCardViewModel item, int change)
        {
            try
            {
                var userId = await SecureStorage.GetAsync("session_token");
                if (string.IsNullOrEmpty(userId) || userId == "00000000-0000-0000-0000-000000000000")
                {
                    await DisplayAlert("Error", "Please log in to modify your cart.", "OK");
                    await Shell.Current.GoToAsync("//Login_page");
                    return;
                }

                int newQuantity = item.Quantity + change;

                if (newQuantity <= 0)
                {
                    await DeleteFromCart(item);
                    return;
                }

                await UpdateQuantity(item.CartId, newQuantity, userId);
                item.Quantity = newQuantity;
                await LoadCartItems();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to update quantity: {ex.Message}", "OK");
            }
        }

        private async Task DeleteFromCart(ProductCardViewModel item)
        {
            try
            {
                var userId = await SecureStorage.GetAsync("session_token");
                if (string.IsNullOrEmpty(userId) || userId == "00000000-0000-0000-0000-000000000000")
                {
                    await DisplayAlert("Error", "Please log in to modify your cart.", "OK");
                    await Shell.Current.GoToAsync("//Login_page");
                    return;
                }

                await RemoveFromCart(item, userId);
                await DisplayAlert("Success", $"{item.Name} removed from cart.", "OK");
                await LoadCartItems();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to remove item from cart: {ex.Message}", "OK");
            }
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LoadCartItems();
        }
        private async Task ProcessCheckout()
        {
            try
            {
                var userId = await SecureStorage.GetAsync("session_token");
                if (string.IsNullOrEmpty(userId) || userId == "00000000-0000-0000-0000-000000000000")
                {
                    await DisplayAlert("Error", "Please log in to proceed with checkout.", "OK");
                    await Shell.Current.GoToAsync("//Login_page");
                    return;
                }

                var cartItems = await GetCartItems(userId);
                if (!cartItems.Any())
                {
                    await DisplayAlert("Error", "Your cart is empty.", "OK");
                    return;
                }

                var groupedBySeller = cartItems.GroupBy(item => item.SellerId);
                var createdOrders = new List<(string OrderId, decimal Total)>();

                foreach (var sellerGroup in groupedBySeller)
                {
                    var sellerItems = sellerGroup.ToList();
                    var sellerId = sellerGroup.Key;

                    decimal subtotal = sellerItems.Sum(item => item.Price * item.Quantity);
                    decimal deliveryFee = 10.00m;
                    decimal total = subtotal + deliveryFee;

                    var order = new Order
                    {
                        UserId = userId,
                        SellerId = sellerId,
                        OrderDate = DateTime.UtcNow,
                        Subtotal = subtotal,
                        DeliveryFee = deliveryFee,
                        Total = total,
                        Status = "Pending",
                        CreatedAt = DateTime.UtcNow
                    };

                    var orderResponse = await _client
                        .From<Order>()
                        .Insert(order);

                    var orderId = orderResponse.Models.First().Id;
                    createdOrders.Add((orderId, total));

                    var orderItems = sellerItems.Select(item => new OrderItem
                    {
                        OrderId = orderId,
                        ProductId = item.Id,
                        ProductName = item.Name,
                        ProductPrice = item.Price,
                        Quantity = item.Quantity,
                        SellerId = item.SellerId,
                        CreatedAt = DateTime.UtcNow
                    }).ToList();

                    await _client
                        .From<OrderItem>()
                        .Insert(orderItems);
                }

                await CartService.Instance.ClearCart(userId);
                await LoadCartItems();

                decimal grandTotal = createdOrders.Sum(o => o.Total);
                await DisplayAlert("Success", $"Your orders for ₱{grandTotal:N2} have been placed and saved to your history!", "OK");

                try
                {
                    await Shell.Current.GoToAsync("//OrderHistory");
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Navigation Error", $"Failed to navigate: {ex.Message}", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Checkout failed: {ex.Message}", "OK");
            }
        }

        private void InitializeCheckoutButton()
        {
            CheckoutButton.Clicked += async (sender, args) => await ProcessCheckout();
        }
    }
}