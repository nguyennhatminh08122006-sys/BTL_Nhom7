using System.Globalization;
using CanifaShop.Areas.Admin.Models;
using CanifaShop.Data;
using CanifaShop.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CanifaShop.Areas.Admin.Controllers;

public class StatsController : BaseAdminController
{
    private const int LowStockThreshold = 10;
    private readonly AppDbContext _db;
    public StatsController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index(string? from, string? to)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var notice = (string?)null;
        var hasFrom = TryParseDate(from, out var parsedFrom);
        var hasTo = TryParseDate(to, out var parsedTo);
        var invalidDate = (!string.IsNullOrWhiteSpace(from) && !hasFrom)
            || (!string.IsNullOrWhiteSpace(to) && !hasTo);

        DateOnly fromDate;
        DateOnly toDate;
        if (invalidDate || (!hasFrom && !hasTo))
        {
            fromDate = today.AddDays(-29);
            toDate = today;
            if (invalidDate) notice = "Ngày lọc không hợp lệ; đã dùng khoảng 30 ngày gần nhất.";
        }
        else
        {
            fromDate = hasFrom ? parsedFrom : parsedTo.AddDays(-29);
            toDate = hasTo ? parsedTo : today;
            if (fromDate > toDate)
            {
                (fromDate, toDate) = (toDate, fromDate);
                notice = "Ngày bắt đầu lớn hơn ngày kết thúc; hệ thống đã đổi chỗ hai ngày.";
            }
            if (toDate.DayNumber - fromDate.DayNumber + 1 > 366)
            {
                fromDate = toDate.AddDays(-365);
                notice = "Khoảng thời gian tối đa là 366 ngày; ngày bắt đầu đã được điều chỉnh.";
            }
        }

        var fromAt = fromDate.ToDateTime(TimeOnly.MinValue);
        var toAt = toDate.ToDateTime(TimeOnly.MaxValue);
        var ordersInPeriod = _db.Orders.AsNoTracking().Where(o =>
            o.CreatedAt >= fromAt && o.CreatedAt <= toAt);
        var completedInPeriod = ordersInPeriod.Where(o => o.Status == OrderStatus.Completed);

        var completedSummary = await completedInPeriod
            .GroupBy(_ => 1)
            .Select(g => new { Revenue = g.Sum(o => o.Total), Count = g.Count() })
            .SingleOrDefaultAsync();
        var revenue = completedSummary?.Revenue ?? 0m;
        var completedCount = completedSummary?.Count ?? 0;
        var allOrderCount = await ordersInPeriod.CountAsync();
        var cancelledCount = await ordersInPeriod.CountAsync(o => o.Status == OrderStatus.Cancelled);
        var cancellationRate = allOrderCount == 0 ? null : (decimal?)cancelledCount / allOrderCount * 100m;
        var averageOrderValue = completedCount == 0 ? null : (decimal?)(revenue / completedCount);
        var waitingNewCount = await _db.Orders.CountAsync(o => o.Status == OrderStatus.New);
        var waitingConfirmedCount = await _db.Orders.CountAsync(o => o.Status == OrderStatus.Confirmed);

        decimal? previousRevenue = null;
        decimal? previousAverage = null;
        int? previousCompletedCount = null;
        var periodLength = toDate.DayNumber - fromDate.DayNumber + 1;
        if (fromDate.DayNumber >= periodLength)
        {
            var previousFrom = fromDate.AddDays(-periodLength);
            var previousTo = fromDate.AddDays(-1);
            var previousFromAt = previousFrom.ToDateTime(TimeOnly.MinValue);
            var previousToAt = previousTo.ToDateTime(TimeOnly.MaxValue);
            var previousSummary = await _db.Orders.AsNoTracking()
                .Where(o => o.Status == OrderStatus.Completed && o.CreatedAt >= previousFromAt && o.CreatedAt <= previousToAt)
                .GroupBy(_ => 1)
                .Select(g => new { Revenue = g.Sum(o => o.Total), Count = g.Count() })
                .SingleOrDefaultAsync();
            previousRevenue = previousSummary?.Revenue ?? 0m;
            previousCompletedCount = previousSummary?.Count ?? 0;
            if (previousSummary is { Count: > 0 }) previousAverage = previousSummary.Revenue / previousSummary.Count;
        }

        var statusGroups = await ordersInPeriod
            .GroupBy(o => o.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToDictionaryAsync(g => g.Status, g => g.Count);
        var statusColors = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [OrderStatus.New] = "#c28a16",
            [OrderStatus.Confirmed] = "#3974c6",
            [OrderStatus.Shipping] = "#7654bd",
            [OrderStatus.Completed] = "#25804a",
            [OrderStatus.Cancelled] = "#c8102e"
        };
        var orderStatuses = OrderStatus.All.Select(s => new StatsOrderStatusVm
        {
            Status = s,
            Count = statusGroups.GetValueOrDefault(s),
            Color = statusColors[s]
        }).ToList();

        var completedOrderIds = completedInPeriod.Select(o => o.Id);
        var topProducts = await (from item in _db.OrderItems.AsNoTracking()
                                 join order in _db.Orders.AsNoTracking() on item.OrderId equals order.Id
                                 join product in _db.Products.AsNoTracking() on item.ProductId equals product.Id
                                 where completedOrderIds.Contains(order.Id)
                                 group item by new { product.Id, product.Name } into productGroup
                                 orderby productGroup.Sum(i => i.Quantity) descending, productGroup.Key.Name
                                 select new StatsProductVm
                                 {
                                     ProductId = productGroup.Key.Id,
                                     Name = productGroup.Key.Name,
                                     Quantity = productGroup.Sum(i => i.Quantity),
                                     Revenue = productGroup.Sum(i => i.UnitPrice * i.Quantity)
                                 })
            .Take(5)
            .ToListAsync();

        var categoryRows = await (from item in _db.OrderItems.AsNoTracking()
                                  join order in _db.Orders.AsNoTracking() on item.OrderId equals order.Id
                                  join product in _db.Products.AsNoTracking() on item.ProductId equals product.Id
                                  join category in _db.Categories.AsNoTracking() on product.CategoryId equals category.Id
                                  where completedOrderIds.Contains(order.Id)
                                  group item by new { category.Id, category.Name } into categoryGroup
                                  select new
                                  {
                                      CategoryId = categoryGroup.Key.Id,
                                      categoryGroup.Key.Name,
                                      Revenue = categoryGroup.Sum(i => i.UnitPrice * i.Quantity)
                                  }).ToListAsync();
        var categoryRevenue = categoryRows.ToDictionary(c => c.CategoryId, c => c.Revenue);
        var categoryList = await _db.Categories.AsNoTracking()
            .OrderBy(c => c.Id)
            .Select(c => new { c.Id, c.Name })
            .ToListAsync();
        var categories = categoryList.Select(c => new StatsCategoryVm
        {
            CategoryId = c.Id,
            Name = c.Name,
            Revenue = categoryRevenue.GetValueOrDefault(c.Id)
        }).ToList();

        var timeline = await BuildTimelineAsync(completedInPeriod, fromDate, toDate);
        var lowStockProducts = await _db.Products.AsNoTracking()
            .Where(p => p.IsActive && p.StockQuantity <= LowStockThreshold)
            .OrderBy(p => p.StockQuantity)
            .ThenBy(p => p.Name)
            .Take(10)
            .Select(p => new LowStockProductVm
            {
                Id = p.Id,
                Name = p.Name,
                CategoryName = p.Category!.Name,
                StockQuantity = p.StockQuantity,
                ImageUrl = p.ImageUrl,
                Color = p.Color,
                Icon = p.Category.Icon
            })
            .ToListAsync();

        var chartData = new StatsChartsVm
        {
            TimelineLabels = timeline.Labels,
            TimelineRevenue = timeline.Values,
            StatusLabels = orderStatuses.Select(s => s.Status).ToList(),
            StatusCounts = orderStatuses.Select(s => s.Count).ToList(),
            StatusColors = orderStatuses.Select(s => s.Color).ToList(),
            ProductLabels = topProducts.Select(p => p.Name).ToList(),
            ProductQuantities = topProducts.Select(p => p.Quantity).ToList(),
            ProductRevenue = topProducts.Select(p => p.Revenue).ToList(),
            CategoryLabels = categories.Select(c => c.Name).ToList(),
            CategoryRevenue = categories.Select(c => c.Revenue).ToList()
        };

        return View(new StatsVm
        {
            FromDate = fromDate,
            ToDate = toDate,
            FilterNotice = notice,
            Revenue = revenue,
            CompletedOrderCount = completedCount,
            AverageOrderValue = averageOrderValue,
            CancellationRate = cancellationRate,
            TotalOrderCount = allOrderCount,
            WaitingNewCount = waitingNewCount,
            WaitingConfirmedCount = waitingConfirmedCount,
            RevenueChangePercent = CalculateChange(revenue, previousRevenue),
            CompletedCountChangePercent = previousCompletedCount.HasValue
                ? CalculateChange(completedCount, previousCompletedCount.Value)
                : null,
            AverageOrderChangePercent = averageOrderValue.HasValue && previousAverage.HasValue
                ? CalculateChange(averageOrderValue.Value, previousAverage.Value)
                : null,
            LowStockThreshold = LowStockThreshold,
            HasCompletedOrders = completedCount > 0,
            HasOrders = allOrderCount > 0,
            HasCategoryRevenue = categories.Any(c => c.Revenue > 0),
            OrderStatuses = orderStatuses,
            TopProducts = topProducts,
            Categories = categories,
            LowStockProducts = lowStockProducts,
            Charts = chartData
        });
    }

    private async Task<(List<string> Labels, List<decimal> Values)> BuildTimelineAsync(
        IQueryable<Order> completedOrders, DateOnly fromDate, DateOnly toDate)
    {
        var labels = new List<string>();
        var values = new List<decimal>();
        if (toDate.DayNumber - fromDate.DayNumber + 1 <= 92)
        {
            var grouped = await completedOrders
                .GroupBy(o => o.CreatedAt.Date)
                .Select(g => new { Date = g.Key, Revenue = g.Sum(o => o.Total) })
                .ToDictionaryAsync(g => g.Date, g => g.Revenue);
            var dayCount = toDate.DayNumber - fromDate.DayNumber + 1;
            for (var offset = 0; offset < dayCount; offset++)
            {
                var date = fromDate.AddDays(offset);
                var key = date.ToDateTime(TimeOnly.MinValue);
                labels.Add(date.ToString("dd/MM", CultureInfo.InvariantCulture));
                values.Add(grouped.GetValueOrDefault(key));
            }
        }
        else
        {
            var grouped = await completedOrders
                .GroupBy(o => new { o.CreatedAt.Year, o.CreatedAt.Month })
                .Select(g => new { g.Key.Year, g.Key.Month, Revenue = g.Sum(o => o.Total) })
                .ToDictionaryAsync(g => (g.Year, g.Month), g => g.Revenue);
            var year = fromDate.Year;
            var month = fromDate.Month;
            while (year < toDate.Year || (year == toDate.Year && month <= toDate.Month))
            {
                labels.Add($"{month:00}/{year}");
                values.Add(grouped.GetValueOrDefault((year, month)));
                if (month == 12)
                {
                    month = 1;
                    year++;
                }
                else month++;
            }
        }
        return (labels, values);
    }

    private static bool TryParseDate(string? value, out DateOnly date)
        => DateOnly.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out date);

    private static decimal? CalculateChange(decimal current, decimal? previous)
        => previous is > 0 ? (current - previous.Value) / previous.Value * 100m : null;
}
