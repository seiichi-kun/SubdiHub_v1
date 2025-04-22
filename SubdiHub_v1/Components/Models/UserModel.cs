using Supabase.Postgrest.Models;
using Supabase.Postgrest.Attributes;
using System;
using SubdiHub_v1.Components.Dashboards.Admin;
namespace SubdiHub_v1.Components.Models
{
    [Table("users")]
    public class User : BaseModel
    {
        [PrimaryKey("User_ID", false)]
        public string UserId { get; set; }

        [Column("Full_name")]
        public string FullName { get; set; }

        [Column("username")]
        public string Username { get; set; }

        [Column("email")]
        public string Email { get; set; }

        [Column("street")]
        public string Street { get; set; }

        [Column("block")]
        public string Block { get; set; }

        [Column("lot")]
        public string Lot { get; set; }

        [Column("password")]
        public string PasswordHash { get; set; }

        [Column("role")]
        public string Role { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Column("contact")]
        public string Contact { get; set; }
    }

    [Table("settingsdb")]
    public class SettingsDb : BaseModel
    {
        [PrimaryKey("id", false)]
        public string Id { get; set; }

        [Column("user_id")]
        public string UserId { get; set; }

        [Column("profileimageurl")]
        public string ProfileImage { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Column("is_online")]
        public bool IsOnline { get; set; }
    }

    [Table("announcements")]
    public class Announcement : BaseModel
    {
        [PrimaryKey("id", false)]
        public string Id { get; set; }

        [Column("user_id")]
        public string UserId { get; set; }

        [Reference(typeof(User))]
        public User Users { get; set; }

        [Column("content")]
        public string Content { get; set; }

        [Column("image_url")]
        public string ImageUrl { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
    }

    [Table("bookings")]
    public class Booking : BaseModel
    {
        [PrimaryKey("id", false)]
        public Guid Id { get; set; }

        [Column("resident_id")]
        public Guid ResidentId { get; set; }

        [Reference(typeof(User))]
        public User Resident { get; set; }

        [Column("driver_id")]
        public Guid? DriverId { get; set; }

        [Reference(typeof(User))]
        public User Driver { get; set; }

        [Column("pickup_lat")]
        public double PickupLat { get; set; }

        [Column("pickup_lng")]
        public double PickupLng { get; set; }

        [Column("destination_lat")]
        public double DestinationLat { get; set; }

        [Column("destination_lng")]
        public double DestinationLng { get; set; }

        [Column("ride_type")]
        public string RideType { get; set; }

        [Column("status")]
        public string Status { get; set; }

        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }
    }

    [Table("products")]
    public class Product : BaseModel
    {
        [PrimaryKey("id", false)]
        public string Id { get; set; }

        [Column("name")]
        public string Name { get; set; }

        [Column("price")]
        public decimal Price { get; set; }

        [Column("description")]
        public string Description { get; set; }

        [Column("image")]
        public string Image { get; set; }

        [Column("category")]
        public string Category { get; set; }

        [Column("Seller_id")]
        public string SellerId { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
    }

    [Table("cart_items")]
    public class CartItem : BaseModel
    {
        [PrimaryKey("id", false)]
        public string Id { get; set; }

        [Column("user_id")]
        public string UserId { get; set; }

        [Column("product_id")]
        public string ProductId { get; set; }

        [Column("seller_id")]
        public string SellerId { get; set; }

        [Column("product_name")]
        public string ProductName { get; set; }

        [Column("product_price")]
        public decimal ProductPrice { get; set; }

        [Column("product_image")]
        public string ProductImage { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Column("quantity")]
        public int Quantity { get; set; }
    }
    [Table("orders")]
    public class Order : BaseModel
    {
        [PrimaryKey("id", false)]
        public string Id { get; set; }

        [Column("user_id")]
        public string UserId { get; set; }

        [Column("order_date")]
        public DateTime OrderDate { get; set; }

        [Column("subtotal")]
        public decimal Subtotal { get; set; }

        [Column("delivery_fee")]
        public decimal DeliveryFee { get; set; }

        [Column("total")]
        public decimal Total { get; set; }

        [Column("status")]
        public string Status { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Column("seller_id")]
        public string SellerId { get; set; }
    }
    [Table("order_items")]
    public class OrderItem : BaseModel
    {
        [PrimaryKey("id", false)]
        public string Id { get; set; }

        [Column("order_id")]
        public string OrderId { get; set; }

        [Column("product_id")]
        public string ProductId { get; set; }

        [Column("product_name")]
        public string ProductName { get; set; }

        [Column("product_price")]
        public decimal ProductPrice { get; set; }

        [Column("quantity")]
        public int Quantity { get; set; }

        [Column("seller_id")]
        public string SellerId { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
    }
    public class CartService
    {
        private static CartService _instance;
        private readonly Supabase.Client _client;
        private const string SupabaseUrl = "https://cqslvtgwuabdgqxxtzfa.supabase.co";
        private const string SupabaseKey = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6ImNxc2x2dGd3dWFiZGdxeHh0emZhIiwicm9sZSI6ImFub24iLCJpYXQiOjE3NDM0Njg1MzksImV4cCI6MjA1OTA0NDUzOX0.m3gKzHkUMal7WhkA4rF-ymGL_97yQAGdUulGfy3Se7g";

        public static CartService Instance => _instance ??= new CartService();

        private CartService()
        {
            _client = new Supabase.Client(SupabaseUrl, SupabaseKey);
        }

        public async Task AddToCart(ProductCardViewModel product, string userId)
        {
            var cartItem = new CartItem
            {
                UserId = userId,
                ProductId = product.Id,
                SellerId = product.SellerId,
                ProductName = product.Name,
                ProductPrice = product.Price,
                ProductImage = product.ProductImage
            };

            await _client.From<CartItem>().Insert(cartItem);
        }

        public async Task<IReadOnlyList<ProductCardViewModel>> GetCartItems(string userId)
        {
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
                SellerId = item.SellerId
            }).ToList().AsReadOnly();
        }

        public async Task ClearCart(string userId)
        {
            await _client
                .From<CartItem>()
                .Where(x => x.UserId == userId)
                .Delete();
        }

        public async Task RemoveFromCart(ProductCardViewModel product, string userId)
        {
            await _client
                .From<CartItem>()
                .Where(x => x.UserId == userId && x.ProductId == product.Id)
                .Delete();
        }
    }
}