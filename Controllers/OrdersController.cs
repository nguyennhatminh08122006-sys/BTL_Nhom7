using System.Security.Claims;
using CanifaShop.Data;
using CanifaShop.Helpers;
using CanifaShop.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CanifaShop.Controllers;

[Authorize]
public class OrdersController : Controller
{
    private readonly AppDbContext _db;
    public OrdersController(AppDbContext db) => _db = db;

    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<IActionResult> Checkout()
    {
        var cartItems = await LoadCartItemsAsync(UserId);
        if (cartItems.Count == 0) return RedirectToAction("Index", "Cart");

        var user = await _db.Users.FindAsync(UserId);
        return View(new CheckoutVm
        {
            ReceiverName = user?.FullName ?? "",
            Phone = user?.Phone ?? "",
            CartItems = cartItems
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Checkout(CheckoutVm vm)
    {
        var cart = await _db.Carts.SingleOrDefaultAsync(c => c.UserId == UserId);
        if (cart == null) return RedirectToAction("Index", "Cart");

        var cartItems = await _db.CartItems.Where(i => i.CartId == cart.Id).ToListAsync();
        if (cartItems.Count == 0) return RedirectToAction("Index", "Cart");

        if (!ModelState.IsValid)
        {
            vm.CartItems = await LoadCartItemsAsync(UserId);
            return View(vm);
        }

        await using var transaction = await _db.Database.BeginTransactionAsync();
        var productIds = cartItems.Select(i => i.ProductId).Distinct().ToList();
        var products = await _db.Products.Where(p => productIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id);
        var quantities = cartItems.GroupBy(i => i.ProductId)
            .ToDictionary(g => g.Key, g => g.Sum(i => i.Quantity));

        foreach (var (productId, quantity) in quantities)
        {
            if (!products.TryGetValue(productId, out var product))
            {
                await transaction.RollbackAsync();
                ModelState.AddModelError("", "Một sản phẩm trong giỏ không còn tồn tại. Giỏ hàng được giữ nguyên.");
                return await CheckoutViewWithCartAsync(vm);
            }
            if (!product.IsActive)
            {
                await transaction.RollbackAsync();
                ModelState.AddModelError("", $"Sản phẩm '{product.Name}' đã ngừng kinh doanh. Giỏ hàng được giữ nguyên.");
                return await CheckoutViewWithCartAsync(vm);
            }
            if (product.StockQuantity < quantity)
            {
                await transaction.RollbackAsync();
                ModelState.AddModelError("", $"Sản phẩm '{product.Name}' chỉ còn {product.StockQuantity} sản phẩm trong kho, không đủ số lượng trong giỏ. Giỏ hàng được giữ nguyên.");
                return await CheckoutViewWithCartAsync(vm);
            }
        }

        foreach (var (productId, quantity) in quantities)
        {
            var affected = await _db.Products
                .Where(p => p.Id == productId && p.IsActive && p.StockQuantity >= quantity)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(p => p.StockQuantity, p => p.StockQuantity - quantity));
            if (affected == 0)
            {
                await transaction.RollbackAsync();
                var name = products.TryGetValue(productId, out var product) ? product.Name : "sản phẩm";
                ModelState.AddModelError("", $"Tồn kho sản phẩm '{name}' vừa thay đổi và không còn đủ số lượng. Giỏ hàng được giữ nguyên.");
                return await CheckoutViewWithCartAsync(vm);
            }
        }

        var order = new Order
        {
            UserId = UserId,
            ReceiverName = vm.ReceiverName.Trim(),
            Phone = vm.Phone.Trim(),
            Address = vm.Address.Trim(),
            Note = vm.Note,
            PaymentMethod = "COD",
            ShippingFee = 0,
            DiscountAmount = 0,
            Status = "Mới"
        };
        foreach (var item in cartItems)
        {
            var product = products[item.ProductId];
            order.Items.Add(new OrderItem
            {
                ProductId = product.Id,
                ProductName = product.Name,
                Size = item.Size,
                Quantity = item.Quantity,
                UnitPrice = product.Price
            });
        }
        order.Total = order.Items.Sum(i => i.UnitPrice * i.Quantity);

        _db.Orders.Add(order);
        await _db.SaveChangesAsync();

        var cartItemIds = cartItems.Select(i => i.Id).ToList();
        await _db.CartItems.Where(i => cartItemIds.Contains(i.Id)).ExecuteDeleteAsync();
        await transaction.CommitAsync();
        return RedirectToAction(nameof(Success), new { id = order.Id });
    }

    private async Task<IActionResult> CheckoutViewWithCartAsync(CheckoutVm vm)
    {
        vm.CartItems = await LoadCartItemsAsync(UserId);
        return View("Checkout", vm);
    }

    private Task<List<CartItemVm>> LoadCartItemsAsync(int userId)
        => (from item in _db.CartItems
            join cart in _db.Carts on item.CartId equals cart.Id
            join product in _db.Products on item.ProductId equals product.Id
            where cart.UserId == userId
            select new CartItemVm
            {
                ProductId = product.Id,
                Name = product.Name,
                ImageUrl = product.ImageUrl,
                Color = product.Color,
                Price = product.Price,
                Size = item.Size,
                Quantity = item.Quantity
            }).ToListAsync();

    public async Task<IActionResult> Success(int id)
    {
        var order = await _db.Orders.Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id && o.UserId == UserId);
        return order == null ? NotFound() : View(order);
    }

    public async Task<IActionResult> History()
    {
        var orders = await _db.Orders.Include(o => o.Items)
            .Where(o => o.UserId == UserId).OrderByDescending(o => o.CreatedAt).ToListAsync();
        return View(orders);
    }
}
