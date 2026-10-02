using System.Data;
using System.Security.Claims;
using CanifaShop.Areas.Admin.Models;
using CanifaShop.Areas.Admin.Services;
using CanifaShop.Data;
using CanifaShop.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CanifaShop.Areas.Admin.Controllers;

public class UsersController : BaseAdminController
{
    private const int PageSize = 10;
    private readonly AppDbContext _db;
    private readonly UserGuard _userGuard;

    public UsersController(AppDbContext db)
    {
        _db = db;
        _userGuard = new UserGuard(db);
    }

    public async Task<IActionResult> Index(string? q, string? role, string? status, int page = 1)
    {
        q = q?.Trim();
        role = role is "Admin" or "Customer" ? role : null;
        status = status is "active" or "locked" ? status : null;
        var currentUserId = GetCurrentUserId();
        var query = _db.Users.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(u => u.FullName.Contains(q) || u.Email.Contains(q) || (u.Phone != null && u.Phone.Contains(q)));
        if (role != null) query = query.Where(u => u.Role == role);
        if (status == "active") query = query.Where(u => !u.IsLocked);
        if (status == "locked") query = query.Where(u => u.IsLocked);

        var totalCount = await query.CountAsync();
        var totalPages = Math.Max(1, (int)Math.Ceiling(totalCount / (double)PageSize));
        page = Math.Clamp(page, 1, totalPages);
        var activeAdminCount = await _db.Users.CountAsync(u => u.Role == "Admin" && !u.IsLocked);
        var projectedUsers = await query
            .OrderByDescending(u => u.Id)
            .Skip((page - 1) * PageSize)
            .Take(PageSize)
            .Select(u => new
            {
                u.Id,
                u.FullName,
                u.Email,
                u.Phone,
                u.Role,
                u.IsLocked,
                OrderCount = _db.Orders.Count(o => o.UserId == u.Id)
            })
            .ToListAsync();

        var users = projectedUsers.Select(u =>
        {
            var isCurrentUser = currentUserId == u.Id;
            var lastActiveAdmin = u.Role == "Admin" && !u.IsLocked && activeAdminCount <= 1;
            var deleteHasOrders = u.OrderCount > 0;
            return new UserRowVm
            {
                Id = u.Id,
                FullName = u.FullName,
                Email = u.Email,
                Phone = u.Phone,
                Role = u.Role,
                IsLocked = u.IsLocked,
                OrderCount = u.OrderCount,
                IsCurrentUser = isCurrentUser,
                DisableLock = !u.IsLocked && lastActiveAdmin,
                LockDisabledReason = !u.IsLocked && lastActiveAdmin ? "Phải còn ít nhất một quản trị viên đang hoạt động." : null,
                DisableDelete = deleteHasOrders || lastActiveAdmin,
                DeleteDisabledReason = deleteHasOrders
                    ? $"Tài khoản đã có {u.OrderCount} đơn hàng, không thể xóa. Hãy dùng chức năng Khóa."
                    : lastActiveAdmin ? "Phải còn ít nhất một quản trị viên đang hoạt động." : null
            };
        }).ToList();

        return View(new UserIndexVm
        {
            Users = users,
            Query = q,
            Role = role,
            Status = status,
            Page = page,
            TotalPages = totalPages,
            TotalCount = totalCount,
            ReturnUrl = CurrentReturnUrl()
        });
    }

    public async Task<IActionResult> Details(int id)
    {
        var user = await _db.Users.AsNoTracking()
            .Where(u => u.Id == id)
            .Select(u => new { u.Id, u.FullName, u.Email, u.Phone, u.Role, u.IsLocked })
            .SingleOrDefaultAsync();
        if (user == null) return NotFound();

        var orderCounts = await _db.Orders.Where(o => o.UserId == id)
            .GroupBy(o => o.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToDictionaryAsync(g => g.Status, g => g.Count);
        var activeAdminCount = await _db.Users.CountAsync(u => u.Role == "Admin" && !u.IsLocked);
        var orderCount = await _db.Orders.CountAsync(o => o.UserId == id);
        var cartProductTypeCount = await (from cart in _db.Carts
                                          from item in cart.Items
                                          where cart.UserId == id
                                          select item.ProductId)
            .Distinct()
            .CountAsync();
        var recentOrders = await _db.Orders.AsNoTracking()
            .Where(o => o.UserId == id)
            .OrderByDescending(o => o.CreatedAt)
            .ThenByDescending(o => o.Id)
            .Take(10)
            .Select(o => new UserRecentOrderVm
            {
                Id = o.Id,
                CreatedAt = o.CreatedAt,
                Total = o.Total,
                Status = o.Status
            })
            .ToListAsync();

        var currentUserId = GetCurrentUserId();
        var isCurrentUser = currentUserId == id;
        var lastActiveAdmin = user.Role == "Admin" && !user.IsLocked && activeAdminCount <= 1;
        var deleteHasOrders = orderCount > 0;
        return View(new UserDetailsVm
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            Phone = user.Phone,
            Role = user.Role,
            IsLocked = user.IsLocked,
            OrderCount = orderCount,
            NonCancelledOrderTotal = await _db.Orders
                .Where(o => o.UserId == id && o.Status != OrderStatus.Cancelled)
                .Select(o => (decimal?)o.Total)
                .SumAsync() ?? 0,
            CartProductTypeCount = cartProductTypeCount,
            OrderStatusCounts = OrderStatus.All.Select(s => new UserOrderStatusCountVm
            {
                Status = s,
                Count = orderCounts.GetValueOrDefault(s)
            }).ToList(),
            RecentOrders = recentOrders,
            IsCurrentUser = isCurrentUser,
            DisableLock = !user.IsLocked && lastActiveAdmin,
            LockDisabledReason = !user.IsLocked && lastActiveAdmin ? "Phải còn ít nhất một quản trị viên đang hoạt động." : null,
            DisableDelete = deleteHasOrders || lastActiveAdmin,
            DeleteDisabledReason = deleteHasOrders
                ? $"Tài khoản đã có {orderCount} đơn hàng, không thể xóa. Hãy dùng chức năng Khóa."
                : lastActiveAdmin ? "Phải còn ít nhất một quản trị viên đang hoạt động." : null,
            ReturnUrl = Url.Action(nameof(Details), new { id }) ?? "/Admin/Users",
            DeleteReturnUrl = Url.Action(nameof(Index)) ?? "/Admin/Users"
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleLock(int id, bool locked, string? returnUrl)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var guard = await _userGuard.CheckAsync(id, GetCurrentUserId(), locked);
        if (!guard.Found || guard.Error != null)
        {
            await transaction.RollbackAsync();
            TempData["Error"] = guard.Error ?? "Không tìm thấy tài khoản.";
            return SafeRedirect(returnUrl, id);
        }

        var affected = await _db.Users
            .Where(u => u.Id == id && u.IsLocked == !locked)
            .ExecuteUpdateAsync(setters => setters.SetProperty(u => u.IsLocked, locked));
        if (affected == 0)
        {
            await transaction.RollbackAsync();
            TempData["Error"] = "Tài khoản vừa được cập nhật bởi người khác, vui lòng tải lại.";
            return SafeRedirect(returnUrl, id);
        }

        await transaction.CommitAsync();
        TempData["Success"] = locked ? $"Đã khóa tài khoản {guard.FullName}." : $"Đã mở khóa tài khoản {guard.FullName}.";
        return SafeRedirect(returnUrl, id);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, string? returnUrl)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var guard = await _userGuard.CheckAsync(id, GetCurrentUserId(), willDeactivate: true);
        if (!guard.Found || guard.Error != null)
        {
            await transaction.RollbackAsync();
            TempData["Error"] = guard.Error ?? "Không tìm thấy tài khoản.";
            return SafeRedirect(returnUrl, id);
        }

        var orderCount = await _db.Orders.CountAsync(o => o.UserId == id);
        if (orderCount > 0)
        {
            await transaction.RollbackAsync();
            TempData["Error"] = $"Tài khoản đã có {orderCount} đơn hàng, không thể xóa. Hãy dùng chức năng Khóa.";
            return SafeRedirect(returnUrl, id);
        }

        try
        {
            // Attach a key-only entity so PasswordHash and other account fields are never loaded.
            var user = new User { Id = id };
            _db.Users.Attach(user);
            _db.Users.Remove(user);
            await _db.SaveChangesAsync();
            await transaction.CommitAsync();
            TempData["Success"] = $"Đã xóa tài khoản {guard.FullName} ({guard.Email}); giỏ hàng của tài khoản cũng đã được xóa.";
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync();
            TempData["Error"] = "Không thể xóa tài khoản do dữ liệu liên quan. Tài khoản chưa bị xóa.";
        }

        return SafeRedirect(returnUrl, id);
    }

    private int? GetCurrentUserId()
        => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

    private string CurrentReturnUrl() => $"{Request.Path}{Request.QueryString}";

    private IActionResult SafeRedirect(string? returnUrl, int id)
    {
        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);
        return RedirectToAction(nameof(Details), new { id });
    }
}
