namespace CanifaShop.Areas.Admin.Models;

public class OrderIndexVm
{
    public List<OrderRowVm> Orders { get; set; } = new();
    public List<OrderStatusCountVm> StatusCounts { get; set; } = new();
    public string? Status { get; set; }
    public string? Query { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public int Page { get; set; }
    public int TotalPages { get; set; }
    public int TotalCount { get; set; }
}

public class OrderRowVm
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public string CustomerName { get; set; } = "";
    public string ReceiverName { get; set; } = "";
    public string Phone { get; set; } = "";
    public int ProductCount { get; set; }
    public decimal Total { get; set; }
    public string Status { get; set; } = "";
}

public class AdminOrderDetailsVm
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public string Status { get; set; } = "";
    public string PaymentMethod { get; set; } = "";
    public string? Note { get; set; }
    public string CustomerName { get; set; } = "";
    public string CustomerEmail { get; set; } = "";
    public string ReceiverName { get; set; } = "";
    public string Phone { get; set; } = "";
    public string Address { get; set; } = "";
    public List<AdminOrderLineVm> Items { get; set; } = new();
    public decimal Subtotal => Items.Sum(i => i.UnitPrice * i.Quantity);
    public decimal ShippingFee { get; set; }
    public decimal DiscountAmount { get; set; }
    public string? DiscountCode { get; set; }
    public decimal Total { get; set; }
    public IReadOnlyList<string> NextStatuses { get; set; } = [];
}

public class AdminOrderLineVm
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = "";
    public string Size { get; set; } = "";
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public string? ImageUrl { get; set; }
    public string Color { get; set; } = "#EEEEEE";
    public string Icon { get; set; } = "";
}
