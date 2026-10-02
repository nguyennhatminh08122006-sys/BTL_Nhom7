using System.Data;
using System.Security.Claims;
using CanifaShop.Data;
using CanifaShop.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CanifaShop.Controllers;

[Authorize]
public class CartController : Controller
{
    private readonly AppDbContext _db;
    public CartController(AppDbContext db) => _db = db;

    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    public async Task<IActionResult> Index()
    {
        var cartId = await _db.Carts.Where(c => c.UserId == UserId)
            .Select(c => (int?)c.Id).SingleOrDefaultAsync();
        var items = new List<CartItemVm>();

        if (cartId.HasValue)
        {
            items = await (
                from item in _db.CartItems
                join product in _db.Products on item.ProductId equals product.Id
                join category in _db.Categories on product.CategoryId equals category.Id
                where item.CartId == cartId.Value
                select new CartItemVm
                {
                    ProductId = product.Id,
                    Name = product.Name,
                    ImageUrl = product.ImageUrl,
                    Color = product.Color,
                    Icon = category.Icon,
                    Price = product.Price,
                    Size = item.Size,
                    Quantity = item.Quantity
                }).ToListAsync();
        }

        return View(new CartIndexVm { Items = items });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(int productId, string size, int quantity = 1)
    {
        var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == productId);
        if (product == null || !product.IsActive) return NotFound();

        var sizes = product.Sizes.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (!sizes.Contains(size)) return BadRequest("Kích cỡ sản phẩm không hợp lệ.");

        quantity = Math.Clamp(quantity, 1, 20);
        await using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var cart = await _db.Carts.SingleOrDefaultAsync(c => c.UserId == UserId);
        if (cart == null)
        {
            cart = new Cart { UserId = UserId, CreatedAt = DateTime.Now };
            _db.Carts.Add(cart);
            await _db.SaveChangesAsync();
        }

        var line = await _db.CartItems.SingleOrDefaultAsync(i =>
            i.CartId == cart.Id && i.ProductId == productId && i.Size == size);
        var oldQuantity = line?.Quantity ?? 0;
        var newQuantity = Math.Min(oldQuantity + quantity, 20);
        var addedQuantity = newQuantity - oldQuantity;
        var productCartQuantity = await _db.CartItems
            .Where(i => i.CartId == cart.Id && i.ProductId == productId)
            .SumAsync(i => (int?)i.Quantity) ?? 0;

        if (productCartQuantity + addedQuantity > product.StockQuantity)
        {
            await transaction.RollbackAsync();
            return BadRequest($"Chỉ còn {product.StockQuantity} sản phẩm '{product.Name}' trong kho.");
        }

        if (line == null)
        {
            _db.CartItems.Add(new CartItem
            {
                CartId = cart.Id,
                ProductId = product.Id,
                Size = size,
                Quantity = newQuantity
            });
        }
        else
        {
            line.Quantity = newQuantity;
        }

        await _db.SaveChangesAsync();
        await transaction.CommitAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(int productId, string size, int quantity)
    {
        var cartId = await _db.Carts.Where(c => c.UserId == UserId)
            .Select(c => (int?)c.Id).SingleOrDefaultAsync();
        if (!cartId.HasValue) return RedirectToAction(nameof(Index));

        var line = await _db.CartItems.FirstOrDefaultAsync(i =>
            i.CartId == cartId.Value && i.ProductId == productId && i.Size == size);
        if (line == null) return RedirectToAction(nameof(Index));

        if (quantity <= 0)
        {
            _db.CartItems.Remove(line);
        }
        else
        {
            var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == productId);
            var otherQuantity = await _db.CartItems
                .Where(i => i.CartId == cartId.Value && i.ProductId == productId && i.Id != line.Id)
                .SumAsync(i => (int?)i.Quantity) ?? 0;
            var maxQuantity = Math.Min(20, Math.Max(0, (product?.StockQuantity ?? 0) - otherQuantity));
            if (quantity > maxQuantity)
                return BadRequest($"Số lượng '{product?.Name}' vượt quá hàng còn trong kho hoặc giới hạn 20 sản phẩm mỗi dòng.");
            line.Quantity = quantity;
        }

        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Remove(int productId, string size)
    {
        var cartId = await _db.Carts.Where(c => c.UserId == UserId)
            .Select(c => (int?)c.Id).SingleOrDefaultAsync();
        if (cartId.HasValue)
        {
            var line = await _db.CartItems.FirstOrDefaultAsync(i =>
                i.CartId == cartId.Value && i.ProductId == productId && i.Size == size);
            if (line != null)
            {
                _db.CartItems.Remove(line);
                await _db.SaveChangesAsync();
            }
        }

        return RedirectToAction(nameof(Index));
    }
}
