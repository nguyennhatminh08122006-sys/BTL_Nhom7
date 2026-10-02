using CanifaShop.Areas.Admin.Models;
using CanifaShop.Data;
using CanifaShop.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CanifaShop.Areas.Admin.Controllers;

public class OrdersController : BaseAdminController
{
    private const int PageSize = 10;
    private readonly AppDbContext _db;
    public OrdersController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index(
        string? status,
        string? q,
        [FromQuery(Name = "from")] DateTime? fromDate,
        [FromQuery(Name = "to")] DateTime? toDate,
        int page = 1)
    {
        status = OrderStatus.All.Contains(status ?? "") ? status : null;
        q = q?.Trim();
        var query = _db.Orders.AsNoTracking().AsQueryable();
        if (status != null) query = query.Where(o => o.Status == status);
        if (!string.IsNullOrWhiteSpace(q))
        {
            var orderIdQuery = q.StartsWith('#') ? q[1..] : q;
            if (int.TryParse(orderIdQuery, out var orderId))
                query = query.Where(o => o.Id == orderId || o.ReceiverName.Contains(q) || o.Phone.Contains(q));
            else
                query = query.Where(o => o.ReceiverName.Contains(q) || o.Phone.Contains(q));
        }
        if (fromDate.HasValue)
        {
            var start = fromDate.Value.Date;
            query = query.Where(o => o.CreatedAt >= start);
        }
        if (toDate.HasValue)
        {
            var end = toDate.Value.Date;
            query = end < DateTime.MaxValue.Date
                ? query.Where(o => o.CreatedAt < end.AddDays(1))
                : query.Where(o => o.CreatedAt <= DateTime.MaxValue);
        }

        var totalCount = await query.CountAsync();
        var totalPages = Math.Max(1, (int)Math.Ceiling(totalCount / (double)PageSize));
        page = Math.Clamp(page, 1, totalPages);

        var groupedCounts = await _db.Orders.AsNoTracking()
            .GroupBy(o => o.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToDictionaryAsync(g => g.Status, g => g.Count);
        var statusCounts = OrderStatus.All.Select(s => new OrderStatusCountVm
        {
            Status = s,
            Count = groupedCounts.GetValueOrDefault(s)
        }).ToList();

        var orders = await query
            .OrderByDescending(o => o.CreatedAt)
            .ThenByDescending(o => o.Id)
            .Skip((page - 1) * PageSize)
            .Take(PageSize)
            .Select(o => new OrderRowVm
            {
                Id = o.Id,
                CreatedAt = o.CreatedAt,
                CustomerName = o.User!.FullName,
                ReceiverName = o.ReceiverName,
                Phone = o.Phone,
                ProductCount = o.Items.Sum(i => i.Quantity),
                Total = o.Total,
                Status = o.Status
            })
            .ToListAsync();

        return View(new OrderIndexVm
        {
            Orders = orders,
            StatusCounts = statusCounts,
            Status = status,
            Query = q,
            FromDate = fromDate,
            ToDate = toDate,
            Page = page,
            TotalPages = totalPages,
            TotalCount = totalCount
        });
    }

    public async Task<IActionResult> Details(int id)
    {
        var order = await _db.Orders.AsNoTracking()
            .Include(o => o.User)
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id);
        if (order == null) return NotFound();

        var productIds = order.Items.Select(i => i.ProductId).Distinct().ToList();
        var productInfo = await _db.Products.AsNoTracking()
            .Where(p => productIds.Contains(p.Id))
            .Select(p => new
            {
                p.Id,
                p.ImageUrl,
                p.Color,
                Icon = p.Category!.Icon
            })
            .ToDictionaryAsync(p => p.Id);

        var model = new AdminOrderDetailsVm
        {
            Id = order.Id,
            CreatedAt = order.CreatedAt,
            Status = order.Status,
            PaymentMethod = order.PaymentMethod,
            Note = order.Note,
            CustomerName = order.User?.FullName ?? "",
            CustomerEmail = order.User?.Email ?? "",
            ReceiverName = order.ReceiverName,
            Phone = order.Phone,
            Address = order.Address,
            ShippingFee = order.ShippingFee,
            DiscountAmount = order.DiscountAmount,
            DiscountCode = order.DiscountCode,
            Total = order.Total,
            NextStatuses = OrderStatus.GetNextStatuses(order.Status),
            Items = order.Items.Select(item =>
            {
                productInfo.TryGetValue(item.ProductId, out var product);
                return new AdminOrderLineVm
                {
                    ProductId = item.ProductId,
                    ProductName = item.ProductName,
                    Size = item.Size,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    ImageUrl = product?.ImageUrl,
                    Color = product?.Color ?? "#EEEEEE",
                    Icon = product?.Icon ?? ""
                };
            }).ToList()
        };

        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(int id, string targetStatus)
    {
        var currentStatus = await _db.Orders.AsNoTracking()
            .Where(o => o.Id == id)
            .Select(o => o.Status)
            .SingleOrDefaultAsync();
        if (currentStatus == null)
        {
            TempData["Error"] = "Không tìm thấy đơn hàng.";
            return RedirectToAction(nameof(Index));
        }
        if (!OrderStatus.CanTransition(currentStatus, targetStatus))
        {
            TempData["Error"] = $"Không thể chuyển đơn hàng từ '{currentStatus}' sang '{targetStatus}'. Vui lòng tải lại trang.";
            return RedirectToAction(nameof(Details), new { id });
        }

        if (targetStatus == OrderStatus.Cancelled)
        {
            await CancelAndRestoreStockAsync(id, currentStatus);
            return RedirectToAction(nameof(Details), new { id });
        }

        var affected = await _db.Orders
            .Where(o => o.Id == id && o.Status == currentStatus)
            .ExecuteUpdateAsync(setters => setters.SetProperty(o => o.Status, targetStatus));
        TempData[affected == 1 ? "Success" : "Error"] = affected == 1
            ? $"Đã cập nhật trạng thái đơn hàng thành '{targetStatus}'."
            : "Đơn hàng vừa được cập nhật bởi người khác, vui lòng tải lại";
        return RedirectToAction(nameof(Details), new { id });
    }

    private async Task CancelAndRestoreStockAsync(int orderId, string currentStatus)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync();
        try
        {
            var affected = await _db.Orders
                .Where(o => o.Id == orderId && o.Status == currentStatus)
                .ExecuteUpdateAsync(setters => setters.SetProperty(o => o.Status, OrderStatus.Cancelled));
            if (affected == 0)
            {
                await transaction.RollbackAsync();
                TempData["Error"] = "Đơn hàng vừa được cập nhật bởi người khác, vui lòng tải lại";
                return;
            }

            var quantities = await _db.OrderItems.Where(i => i.OrderId == orderId)
                .GroupBy(i => i.ProductId)
                .Select(g => new { ProductId = g.Key, Quantity = g.Sum(i => i.Quantity) })
                .ToListAsync();
            foreach (var item in quantities)
            {
                var updated = await _db.Products
                    .Where(p => p.Id == item.ProductId)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(p => p.StockQuantity, p => p.StockQuantity + item.Quantity));
                if (updated != 1)
                {
                    await transaction.RollbackAsync();
                    TempData["Error"] = "Không thể hoàn tồn kho cho đơn hàng. Trạng thái đơn và tồn kho chưa thay đổi.";
                    return;
                }
            }

            await transaction.CommitAsync();
            TempData["Success"] = "Đã hủy đơn hàng và hoàn lại tồn kho.";
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            TempData["Error"] = "Không thể hủy đơn hàng. Trạng thái đơn và tồn kho chưa thay đổi.";
        }
    }
}
