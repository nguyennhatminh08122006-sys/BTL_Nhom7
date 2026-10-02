using CanifaShop.Areas.Admin.Models;
using CanifaShop.Data;
using CanifaShop.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CanifaShop.Areas.Admin.Controllers;

public class DashboardController : BaseAdminController
{
    private readonly AppDbContext _db;
    public DashboardController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index()
    {
        var orderCounts = await _db.Orders
            .GroupBy(o => o.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToDictionaryAsync(g => g.Status, g => g.Count);
        var model = new DashboardVm
        {
            CategoryCount = await _db.Categories.CountAsync(),
            ActiveProductCount = await _db.Products.CountAsync(p => p.IsActive),
            InactiveProductCount = await _db.Products.CountAsync(p => !p.IsActive),
            CustomerCount = await _db.Users.CountAsync(u => u.Role == "Customer"),
            AdminCount = await _db.Users.CountAsync(u => u.Role == "Admin"),
            LockedAccountCount = await _db.Users.CountAsync(u => u.IsLocked),
            CartCount = await _db.Carts.CountAsync(),
            CartsWithItemsCount = await _db.Carts.CountAsync(c => c.Items.Any()),
            OrdersByStatus = OrderStatus.All.Select(status => new OrderStatusCountVm
            {
                Status = status,
                Count = orderCounts.GetValueOrDefault(status)
            }).ToList(),
            RecentOrders = await _db.Orders
                .OrderByDescending(o => o.CreatedAt)
                .ThenByDescending(o => o.Id)
                .Take(5)
                .Select(o => new RecentOrderVm
                {
                    Id = o.Id,
                    CreatedAt = o.CreatedAt,
                    ReceiverName = o.ReceiverName,
                    Total = o.Total,
                    Status = o.Status
                })
                .ToListAsync()
        };

        return View(model);
    }
}
