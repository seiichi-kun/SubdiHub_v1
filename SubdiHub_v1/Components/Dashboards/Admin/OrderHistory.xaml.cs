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

namespace SubdiHub_v1.Components.Dashboards.Admin
{
    public partial class OrderHistoryViewModel : ObservableObject
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

        public OrderHistoryViewModel()
        {
            try
            {
                _client = new Supabase.Client(SupabaseUrl, SupabaseKey);
            }
            catch (Exception ex)
            {
                Shell.Current.DisplayAlert("Error", $"Failed to initialize Supabase: {ex.Message}", "OK");
            }
        }

        [RelayCommand]
        public async Task LoadOrders()
        {
            try
            {
                IsLoading = true;
                var userId = await SecureStorage.GetAsync("session_token");
                if (string.IsNullOrEmpty(userId) || userId == "00000000-0000-0000-0000-000000000000")
                {
                    await Shell.Current.DisplayAlert("Error", "Please log in to view your order history.", "OK");
                    await Shell.Current.GoToAsync("//Login_page");
                    IsEmptyVisible = true;
                    IsOrdersVisible = false;
                    return;
                }

                var orders = await GetOrders(userId);
                if (!orders.Any())
                {
                    IsEmptyVisible = true;
                    IsOrdersVisible = false;
                    return;
                }

                // Fetch seller information only for non-null seller IDs
                var sellerIds = orders.Select(o => o.SellerId).Where(id => id != null).Distinct().ToList();
                Dictionary<string, string> sellersDict = new();
                if (sellerIds.Any())
                {
                    var sellerResponse = await _client
                        .From<User>()
                        .Filter("User_ID", Supabase.Postgrest.Constants.Operator.In, sellerIds)
                        .Get();
                    sellersDict = sellerResponse.Models.ToDictionary(u => u.UserId, u => u.FullName);
                }

                IsEmptyVisible = false;
                IsOrdersVisible = true;
                Orders.Clear();
                foreach (var order in orders.OrderByDescending(o => o.OrderDate))
                {
                    var items = await GetOrderItems(order.Id);
                    Orders.Add(new OrderViewModel
                    {
                        Id = order.Id,
                        HeaderText = $"Order #{order.Id} - {order.OrderDate:MMM dd, yyyy}",
                        SellerName = string.IsNullOrEmpty(order.SellerId) ? "Unknown Seller" : sellersDict.GetValueOrDefault(order.SellerId, "Unknown Seller"),
                        StatusText = $"Status: {order.Status}",
                        StatusColor = GetStatusColor(order.Status),
                        TotalText = $"Total: ₱{order.Total:N2}",
                        CanCancel = order.Status == "Pending" || order.Status == "On the way to delivery",
                        Items = new ObservableCollection<OrderItemViewModel>(items),
                        ItemsHeight = items.Count * 60,
                        CancelOrderCommand = new AsyncRelayCommand(() => UpdateOrderStatus(order.Id, "Waiting for seller response to cancel"))
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

        private async Task<IReadOnlyList<Order>> GetOrders(string userId)
        {
            if (_client == null)
                throw new Exception("Supabase client not initialized.");

            var response = await _client
                .From<Order>()
                .Where(x => x.UserId == userId)
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
                await Shell.Current.DisplayAlert("Success", $"Order status updated to {newStatus}.", "OK");
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
                "Waiting for seller response to cancel" => Color.Parse("#FF4500"),
                "Order canceled" => Color.Parse("#D90429"),
                _ => Color.Parse("#6B7280")
            };
        }
    }

    public partial class OrderHistory : ContentPage
    {
        private readonly OrderHistoryViewModel _viewModel;

        public OrderHistory()
        {
            InitializeComponent();
            _viewModel = new OrderHistoryViewModel();
            BindingContext = _viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await _viewModel.LoadOrdersCommand.ExecuteAsync(null);

            // Animate order frames
            var frames = OrdersCollection?.GetVisualTreeDescendants()
                .OfType<Frame>()
                .Where(f => f.Id != Guid.Empty);
            if (frames != null)
            {
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
        public string SellerName { get; set; }
        public string StatusText { get; set; }
        public Color StatusColor { get; set; }
        public string TotalText { get; set; }
        public bool CanCancel { get; set; }
        public ObservableCollection<OrderItemViewModel> Items { get; set; }
        public double ItemsHeight { get; set; }
        public IAsyncRelayCommand CancelOrderCommand { get; set; }
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