namespace CanifaShop.Areas.Admin.Models;

public class StatsVm
{
    public DateOnly FromDate { get; set; }
    public DateOnly ToDate { get; set; }
    public string? FilterNotice { get; set; }
    public decimal Revenue { get; set; }
    public int CompletedOrderCount { get; set; }
    public decimal? AverageOrderValue { get; set; }
    public decimal? CancellationRate { get; set; }
    public int TotalOrderCount { get; set; }
    public int WaitingNewCount { get; set; }
    public int WaitingConfirmedCount { get; set; }
    public decimal? RevenueChangePercent { get; set; }
    public decimal? CompletedCountChangePercent { get; set; }
    public decimal? AverageOrderChangePercent { get; set; }
    public int LowStockThreshold { get; set; }
    public bool HasCompletedOrders { get; set; }
    public bool HasOrders { get; set; }
    public bool HasCategoryRevenue { get; set; }
    public List<StatsOrderStatusVm> OrderStatuses { get; set; } = new();
    public List<StatsProductVm> TopProducts { get; set; } = new();
    public List<StatsCategoryVm> Categories { get; set; } = new();
    public List<LowStockProductVm> LowStockProducts { get; set; } = new();
    public StatsChartsVm Charts { get; set; } = new();
}

public class StatsOrderStatusVm
{
    public string Status { get; set; } = "";
    public int Count { get; set; }
    public string Color { get; set; } = "#888888";
}

public class StatsProductVm
{
    public int ProductId { get; set; }
    public string Name { get; set; } = "";
    public int Quantity { get; set; }
    public decimal Revenue { get; set; }
}

public class StatsCategoryVm
{
    public int CategoryId { get; set; }
    public string Name { get; set; } = "";
    public decimal Revenue { get; set; }
}

public class LowStockProductVm
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string CategoryName { get; set; } = "";
    public int StockQuantity { get; set; }
    public string? ImageUrl { get; set; }
    public string Color { get; set; } = "#EEEEEE";
    public string Icon { get; set; } = "";
}

public class StatsChartsVm
{
    public List<string> TimelineLabels { get; set; } = new();
    public List<decimal> TimelineRevenue { get; set; } = new();
    public List<string> StatusLabels { get; set; } = new();
    public List<int> StatusCounts { get; set; } = new();
    public List<string> StatusColors { get; set; } = new();
    public List<string> ProductLabels { get; set; } = new();
    public List<int> ProductQuantities { get; set; } = new();
    public List<decimal> ProductRevenue { get; set; } = new();
    public List<string> CategoryLabels { get; set; } = new();
    public List<decimal> CategoryRevenue { get; set; } = new();
}
