using System.Data;
using CanifaShop.Areas.Admin.Models;
using CanifaShop.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CanifaShop.Areas.Admin.Controllers;

public class CartsController : BaseAdminController
{
    private const int PageSize = 10;
    private readonly AppDbContext _db;
    public CartsController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index(string? q, string? status, int page = 1)
    {
        q = q?.Trim();
        status = status is "withItems" or "empty" ? status : null;
        var query = _db.Carts.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(c => _db.Users.Any(u => u.Id == c.UserId && (u.FullName.Contains(q) || u.Email.Contains(q))));
        if (status == "withItems") query = query.Where(c => c.Items.Any());
        if (status == "empty") query = query.Where(c => !c.Items.Any());

        var totalCount = await query.CountAsync();
        var totalPages = Math.Max(1, (int)Math.Ceiling(totalCount / (double)PageSize));
        page = Math.Clamp(page, 1, totalPages);
        var carts = await (from cart in query
                           join user in _db.Users.AsNoTracking() on cart.UserId equals user.Id
                           orderby cart.CreatedAt descending, cart.Id descending
                           select new CartRowVm
                           {
                               Id = cart.Id,
                               CustomerName = user.FullName,
                               CustomerEmail = user.Email,
                               LineCount = _db.CartItems.Count(item => item.CartId == cart.Id),
                               TotalQuantity = _db.CartItems.Where(item => item.CartId == cart.Id)
                                   .Select(item => (int?)item.Quantity).Sum() ?? 0,
                               EstimatedValue = (from item in _db.CartItems
                                                 join product in _db.Products on item.ProductId equals product.Id
                                                 where item.CartId == cart.Id
                                                 select (decimal?)(item.Quantity * product.Price)).Sum() ?? 0,
                               CreatedAt = cart.CreatedAt,
                               HasItems = _db.CartItems.Any(item => item.CartId == cart.Id)
                           })
            .Skip((page - 1) * PageSize)
            .Take(PageSize)
            .ToListAsync();

        return View(new AdminCartIndexVm
        {
            Carts = carts,
            Query = q,
            Status = status,
            Page = page,
            TotalPages = totalPages,
            TotalCount = totalCount,
            ReturnUrl = $"{Request.Path}{Request.QueryString}"
        });
    }

    public async Task<IActionResult> Details(int id, string? returnUrl)
    {
        var cart = await (from c in _db.Carts.AsNoTracking()
                          join user in _db.Users.AsNoTracking() on c.UserId equals user.Id
                          where c.Id == id
                          select new
                          {
                              c.Id,
                              c.UserId,
                              CustomerName = user.FullName,
                              CustomerEmail = user.Email,
                              c.CreatedAt
                          }).SingleOrDefaultAsync();
        if (cart == null) return NotFound();

        var items = await (from item in _db.CartItems.AsNoTracking()
                           join product in _db.Products.AsNoTracking() on item.ProductId equals product.Id
                           join category in _db.Categories.AsNoTracking() on product.CategoryId equals category.Id
                           where item.CartId == id
                           orderby item.Id
                           select new CartLineVm
                           {
                               Id = item.Id,
                               ProductId = product.Id,
                               ProductName = product.Name,
                               Size = item.Size,
                               Quantity = item.Quantity,
                               UnitPrice = product.Price,
                               StockQuantity = product.StockQuantity,
                               IsActive = product.IsActive,
                               Sizes = product.Sizes,
                               ImageUrl = product.ImageUrl,
                               Color = product.Color,
                               Icon = category.Icon
                           }).ToListAsync();
        foreach (var item in items)
        {
            item.SizeAvailable = item.Sizes.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                .Contains(item.Size, StringComparer.Ordinal);
            if (!item.IsActive) item.Warnings.Add("Ngừng bán");
            if (item.StockQuantity == 0) item.Warnings.Add("Hết hàng");
            if (item.Quantity > item.StockQuantity) item.Warnings.Add("Vượt tồn kho");
            if (!item.SizeAvailable) item.Warnings.Add("Size không còn");
        }

        return View(new CartDetailsVm
        {
            Id = cart.Id,
            UserId = cart.UserId,
            CustomerName = cart.CustomerName,
            CustomerEmail = cart.CustomerEmail,
            CreatedAt = cart.CreatedAt,
            Items = items,
            ReturnUrl = !string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl)
                ? returnUrl
                : Url.Action(nameof(Index)) ?? "/Admin/Carts"
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteItem(int cartId, int itemId, string? returnUrl)
    {
        var owner = await (from cart in _db.Carts.AsNoTracking()
                           join user in _db.Users.AsNoTracking() on cart.UserId equals user.Id
                           where cart.Id == cartId
                           select new { user.FullName, user.Email }).SingleOrDefaultAsync();
        if (owner == null)
        {
            TempData["Error"] = "Giỏ hàng không còn tồn tại.";
            return SafeRedirect(returnUrl, cartId);
        }

        var affected = await _db.CartItems
            .Where(item => item.Id == itemId && item.CartId == cartId)
            .ExecuteDeleteAsync();
        if (affected == 0)
            TempData["Error"] = "Giỏ hàng không còn tồn tại.";
        else
            TempData["Success"] = $"Đã xóa dòng hàng khỏi giỏ của {owner.FullName} ({owner.Email}). Tồn kho không thay đổi.";
        return RedirectToAction(nameof(Details), new { id = cartId, returnUrl });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, string? returnUrl)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var cart = await (from c in _db.Carts.AsNoTracking()
                          join user in _db.Users.AsNoTracking() on c.UserId equals user.Id
                          where c.Id == id
                          select new { c.Id, user.FullName, user.Email }).SingleOrDefaultAsync();
        if (cart == null)
        {
            await transaction.RollbackAsync();
            TempData["Error"] = "Giỏ hàng không còn tồn tại.";
            return SafeRedirect(returnUrl, id);
        }

        var affected = await _db.Carts.Where(c => c.Id == id).ExecuteDeleteAsync();
        if (affected == 0)
        {
            await transaction.RollbackAsync();
            TempData["Error"] = "Giỏ hàng không còn tồn tại.";
        }
        else
        {
            await transaction.CommitAsync();
            TempData["Success"] = $"Đã xóa giỏ hàng của {cart.FullName} ({cart.Email}). Tồn kho không thay đổi.";
        }

        return SafeRedirect(returnUrl, id);
    }

    private IActionResult SafeRedirect(string? returnUrl, int cartId)
    {
        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);
        return RedirectToAction(nameof(Details), new { id = cartId });
    }
}
