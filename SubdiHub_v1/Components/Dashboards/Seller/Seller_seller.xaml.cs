using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using Supabase;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SubdiHub_v1.Components.Models;

namespace SubdiHub_v1.Components.Dashboards.Seller
{
    public partial class SellerSellerViewModel : ObservableObject
    {
        private readonly Supabase.Client _client;
        private const string SupabaseUrl = "https://cqslvtgwuabdgqxxtzfa.supabase.co";
        private const string SupabaseKey = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6ImNxc2x2dGd3dWFiZGdxeHh0emZhIiwicm9sZSI6ImFub24iLCJpYXQiOjE3NDM0Njg1MzksImV4cCI6MjA1OTA0NDUzOX0.m3gKzHkUMal7WhkA4rF-ymGL_97yQAGdUulGfy3Se7g";

        [ObservableProperty]
        private ObservableCollection<OrderViewModel> orders = new();

        [ObservableProperty]
        private bool isEmptyVisible;

        [ObservableProperty]
        private bool isOrdersVisible;

        [ObservableProperty]
        private bool isLoading;

        public SellerSellerViewModel()
        {
            try
            {
                _client = new Supabase.Client(SupabaseUrl, SupabaseKey);
            }
            catch (Exception ex)
            {
                // Use async display alert in a non-async constructor
                MainThread.BeginInvokeOnMainThread(async () =>
                    await Shell.Current.DisplayAlert("Error", $"Failed to initialize Supabase: {ex.Message}", "OK"));
            }
        }

        [RelayCommand]
        public async Task LoadOrders()
        {
            try
            {
                IsLoading = true;
                var sellerId = await SecureStorage.GetAsync("session_token");
                if (string.IsNullOrEmpty(sellerId) || sellerId == "00000000-0000-0000-0000-000000000000")
                {
                    await Shell.Current.DisplayAlert("Error", "Please log in to view your orders.", "OK");
                    await Shell.Current.GoToAsync("//Login_page");
                    IsEmptyVisible = true;
                    IsOrdersVisible = false;
                    return;
                }

                var orders = await GetOrders(sellerId);
                if (!orders.Any())
                {
                    IsEmptyVisible = true;
                    IsOrdersVisible = false;
                    return;
                }

                // Fetch user information for all user_id values
                var userIds = orders.Select(o => o.UserId).Distinct().ToList();
                var userResponse = await _client
                    .From<User>()
                    .Filter("User_ID", Supabase.Postgrest.Constants.Operator.In, userIds)
                    .Get();
                var usersDict = userResponse.Models.ToDictionary(u => u.UserId, u => u);

                // Fetch profile images from settingsdb
                var settingsResponse = await _client
                    .From<SettingsDb>()
                    .Filter("user_id", Supabase.Postgrest.Constants.Operator.In, userIds)
                    .Get();
                var profileImagesDict = settingsResponse.Models.ToDictionary(s => s.UserId, s => s.ProfileImage);

                IsEmptyVisible = false;
                IsOrdersVisible = true;
                Orders.Clear();
                foreach (var order in orders.OrderByDescending(o => o.OrderDate))
                {
                    var items = await GetOrderItems(order.Id);
                    var user = usersDict.GetValueOrDefault(order.UserId);
                    var address = user != null
                        ? $"{user.Street ?? "N/A"}, Block {user.Block ?? "N/A"}, Lot {user.Lot ?? "N/A"}"
                        : "Unknown Address";
                    Orders.Add(new OrderViewModel
                    {
                        Id = order.Id,
                        HeaderText = $"Order #{order.Id} - {order.OrderDate:MMM dd, yyyy}",
                        UserName = user?.FullName ?? "Unknown User",
                        UserAddress = address,
                        UserContact = user?.Contact ?? "N/A",
                        UserProfileImage = profileImagesDict.GetValueOrDefault(order.UserId, "default_profile.png"),
                        StatusText = $"Status: {order.Status}",
                        StatusColor = GetStatusColor(order.Status),
                        TotalText = $"Total: ₱{order.Total:N2}",
                        IsPending = order.Status == "Pending",
                        IsWaitingForCancel = order.Status == "Waiting for seller response to cancel",
                        IsOnTheWay = order.Status == "On the way to delivery",
                        Items = new ObservableCollection<OrderItemViewModel>(items),
                        ItemsHeight = items.Count * 50,
                        AcceptOrderCommand = new AsyncRelayCommand(() => UpdateOrderStatus(order.Id, "On the way to delivery")),
                        DeclineOrderCommand = new AsyncRelayCommand(() => UpdateOrderStatus(order.Id, "Declined - Sorry, the item is out of stock")),
                        ConfirmCancelCommand = new AsyncRelayCommand(() => UpdateOrderStatus(order.Id, "Order canceled")),
                        MarkDeliveredCommand = new AsyncRelayCommand(() => UpdateOrderStatus(order.Id, "Delivered successfully"))
                    });
                }
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Error", $"Failed to load orders: {ex.Message}", "OK");
                IsEmptyVisible = true;
                IsOrdersVisible = false;
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task<IReadOnlyList<Order>> GetOrders(string sellerId)
        {
            if (_client == null)
                throw new Exception("Supabase client not initialized.");

            var response = await _client
                .From<Order>()
                .Where(x => x.SellerId == sellerId)
                .Get();
            return response.Models.AsReadOnly();
        }

        private async Task<IReadOnlyList<OrderItemViewModel>> GetOrderItems(string orderId)
        {
            if (_client == null)
                throw new Exception("Supabase client not initialized.");

            var response = await _client
                .From<OrderItem>()
                .Where(x => x.OrderId == orderId)
                .Get();

            var orderItems = response.Models;
            var productIds = orderItems.Select(oi => oi.ProductId).Distinct().ToList();
            var productsTask = _client
                .From<Product>()
                .Filter("id", Supabase.Postgrest.Constants.Operator.In, productIds)
                .Get();

            var products = (await productsTask).Models.ToDictionary(p => p.Id, p => p.Image);
            return orderItems.Select(item => new OrderItemViewModel
            {
                ProductId = item.ProductId,
                ProductName = item.ProductName,
                ProductPrice = item.ProductPrice,
                Quantity = item.Quantity,
                ProductImage = products.GetValueOrDefault(item.ProductId, "default_product.png"),
                PriceQuantityText = $"₱{item.ProductPrice:N2} x {item.Quantity}"
            }).ToList().AsReadOnly();
        }

        private async Task UpdateOrderStatus(string orderId, string newStatus)
        {
            try
            {
                await _client
                    .From<Order>()
                    .Where(x => x.Id == orderId)
                    .Set(x => x.Status, newStatus)
                    .Update();
                await LoadOrders(); // Refresh orders to reflect status change
                await Shell.Current.DisplayAlert("Success", $"Order {newStatus}.", "OK");
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Error", $"Failed to update order status: {ex.Message}", "OK");
            }
        }

        private Color GetStatusColor(string status)
        {
            return status switch
            {
                "Pending" => Color.Parse("#FFA500"),
                "On the way to delivery" => Color.Parse("#2D6A4F"),
                "Delivered successfully" => Color.Parse("#008000"),
                "Declined" => Color.Parse("#D90429"),
                "Waiting for seller response to cancel" => Color.Parse("#FF4500"),
                "Order canceled" => Color.Parse("#D90429"),
                _ => Color.Parse("#6B7280")
            };
        }
    }

    public partial class Seller_seller : ContentPage
    {
        private readonly SellerSellerViewModel _viewModel;

        public Seller_seller()
        {
            InitializeComponent();
            _viewModel = new SellerSellerViewModel();
            BindingContext = _viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await _viewModel.LoadOrdersCommand.ExecuteAsync(null);

            // Animate order frames
            var ordersCollection = this.FindByName<CollectionView>("OrdersCollection");
            if (ordersCollection != null)
            {
                var frames = ordersCollection.GetVisualTreeDescendants()
                    .OfType<Frame>()
                    .Where(f => f.Id != Guid.Empty);
                foreach (var frame in frames)
                {
                    frame.TranslationY = 50;
                    frame.Opacity = 0;
                    await Task.WhenAll(
                        frame.TranslateTo(0, 0, 300, Easing.CubicOut),
                        frame.FadeTo(1, 300)
                    );
                }
            }
        }
    }

    public class OrderViewModel : ObservableObject
    {
        public string Id { get; set; }
        public string HeaderText { get; set; }
        public string UserName { get; set; }
        public string UserAddress { get; set; }
        public string UserContact { get; set; }
        public string UserProfileImage { get; set; }
        public string StatusText { get; set; }
        public Color StatusColor { get; set; }
        public string TotalText { get; set; }
        public bool IsPending { get; set; }
        public bool IsWaitingForCancel { get; set; }
        public bool IsOnTheWay { get; set; }
        public ObservableCollection<OrderItemViewModel> Items { get; set; }
        public double ItemsHeight { get; set; }
        public IAsyncRelayCommand AcceptOrderCommand { get; set; }
        public IAsyncRelayCommand DeclineOrderCommand { get; set; }
        public IAsyncRelayCommand ConfirmCancelCommand { get; set; }
        public IAsyncRelayCommand MarkDeliveredCommand { get; set; }
    }

    public class OrderItemViewModel : ObservableObject
    {
        public string ProductId { get; set; }
        public string ProductName { get; set; }
        public decimal ProductPrice { get; set; }
        public int Quantity { get; set; }
        public string ProductImage { get; set; }
        public string PriceQuantityText { get; set; }
    }
}