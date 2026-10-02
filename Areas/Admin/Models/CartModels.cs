namespace CanifaShop.Areas.Admin.Models;

public class AdminCartIndexVm
{
    public List<CartRowVm> Carts { get; set; } = new();
    public string? Query { get; set; }
    public string? Status { get; set; }
    public int Page { get; set; }
    public int TotalPages { get; set; }
    public int TotalCount { get; set; }
    public string ReturnUrl { get; set; } = "";
}

public class CartRowVm
{
    public int Id { get; set; }
    public string CustomerName { get; set; } = "";
    public string CustomerEmail { get; set; } = "";
    public int LineCount { get; set; }
    public int TotalQuantity { get; set; }
    public decimal EstimatedValue { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool HasItems { get; set; }
}

public class CartDetailsVm
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string CustomerName { get; set; } = "";
    public string CustomerEmail { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public List<CartLineVm> Items { get; set; } = new();
    public decimal EstimatedValue => Items.Sum(i => i.UnitPrice * i.Quantity);
    public string ReturnUrl { get; set; } = "";
}

public class CartLineVm
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = "";
    public string Size { get; set; } = "";
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public int StockQuantity { get; set; }
    public bool IsActive { get; set; }
    public bool SizeAvailable { get; set; }
    public string Sizes { get; set; } = "";
    public string? ImageUrl { get; set; }
    public string Color { get; set; } = "#EEEEEE";
    public string Icon { get; set; } = "";
    public List<string> Warnings { get; set; } = new();
}
