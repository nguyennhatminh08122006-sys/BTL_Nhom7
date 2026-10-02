using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CanifaShop.Models;

public class Category
{
    public int Id { get; set; }
    [Required, StringLength(100)] public string Name { get; set; } = "";
    [Required, StringLength(100)] public string Slug { get; set; } = "";
    [StringLength(10)] public string Icon { get; set; } = "";
    public List<Product> Products { get; set; } = new();
}

public class Product
{
    public int Id { get; set; }
    [Required, StringLength(200)] public string Name { get; set; } = "";
    [StringLength(2000)] public string Description { get; set; } = "";
    [Column(TypeName = "decimal(18,0)")] public decimal Price { get; set; }
    [Column(TypeName = "decimal(18,0)")] public decimal? OldPrice { get; set; }
    [StringLength(100)] public string Sizes { get; set; } = "S,M,L,XL";
    [StringLength(20)] public string Color { get; set; } = "#EEEEEE";
    public bool IsFeatured { get; set; }
    public string? ImageUrl { get; set; }
    public int StockQuantity { get; set; }
    public bool IsActive { get; set; } = true;
    public int CategoryId { get; set; }
    public Category? Category { get; set; }
}

public class User
{
    public int Id { get; set; }
    [Required, StringLength(100)] public string FullName { get; set; } = "";
    [Required, StringLength(200)] public string Email { get; set; } = "";
    [StringLength(20)] public string? Phone { get; set; }
    [Required] public string PasswordHash { get; set; } = "";
    [Required, StringLength(20)] public string Role { get; set; } = "Customer";
    public bool IsLocked { get; set; }
}

public class Order
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User? User { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    [Required, StringLength(100)] public string ReceiverName { get; set; } = "";
    [Required, StringLength(20)] public string Phone { get; set; } = "";
    [Required, StringLength(300)] public string Address { get; set; } = "";
    [StringLength(500)] public string? Note { get; set; }
    [Column(TypeName = "decimal(18,0)")] public decimal Total { get; set; }
    [StringLength(30)] public string Status { get; set; } = "Mới";
    [Required, StringLength(30)] public string PaymentMethod { get; set; } = "COD";
    [Column(TypeName = "decimal(18,0)")] public decimal ShippingFee { get; set; }
    [Column(TypeName = "decimal(18,0)")] public decimal DiscountAmount { get; set; }
    [StringLength(50)] public string? DiscountCode { get; set; }
    public List<OrderItem> Items { get; set; } = new();
}

public class OrderItem
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public Order? Order { get; set; }
    public int ProductId { get; set; }
    [StringLength(200)] public string ProductName { get; set; } = "";
    [StringLength(20)] public string Size { get; set; } = "";
    public int Quantity { get; set; }
    [Column(TypeName = "decimal(18,0)")] public decimal UnitPrice { get; set; }
}

public class Cart
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public List<CartItem> Items { get; set; } = new();
}

public class CartItem
{
    public int Id { get; set; }
    public int CartId { get; set; }
    public int ProductId { get; set; }
    [Required, StringLength(20)] public string Size { get; set; } = "";
    public int Quantity { get; set; }
}
