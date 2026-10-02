namespace CanifaShop.Areas.Admin.Models;

public class DashboardVm
{
    public int CategoryCount { get; set; }
    public int ActiveProductCount { get; set; }
    public int InactiveProductCount { get; set; }
    public int CustomerCount { get; set; }
    public int AdminCount { get; set; }
    public int LockedAccountCount { get; set; }
    public int CartCount { get; set; }
    public int CartsWithItemsCount { get; set; }
    public List<OrderStatusCountVm> OrdersByStatus { get; set; } = new();
    public List<RecentOrderVm> RecentOrders { get; set; } = new();
}

public class OrderStatusCountVm
{
    public string Status { get; set; } = "";
    public int Count { get; set; }
}

public class RecentOrderVm
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public string ReceiverName { get; set; } = "";
    public decimal Total { get; set; }
    public string Status { get; set; } = "";
}

public class CategoryFormVm
{
    public int Id { get; set; }

    [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Vui lòng nhập tên danh mục.")]
    [System.ComponentModel.DataAnnotations.StringLength(100, ErrorMessage = "Tên danh mục tối đa 100 ký tự.")]
    public string Name { get; set; } = "";

    [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Vui lòng nhập slug.")]
    [System.ComponentModel.DataAnnotations.StringLength(100, ErrorMessage = "Slug tối đa 100 ký tự.")]
    [System.ComponentModel.DataAnnotations.RegularExpression("^[a-z0-9]+(-[a-z0-9]+)*$", ErrorMessage = "Slug chỉ gồm chữ thường không dấu, số và dấu gạch ngang.")]
    public string Slug { get; set; } = "";

    [System.ComponentModel.DataAnnotations.StringLength(10, ErrorMessage = "Icon tối đa 10 ký tự.")]
    public string? Icon { get; set; }
}

public class CategoryRowVm
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Slug { get; set; } = "";
    public string Icon { get; set; } = "";
    public int ProductCount { get; set; }
}
