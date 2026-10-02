using System.Security.Claims;
using CanifaShop.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CanifaShop.Components;

public class CartCountViewComponent : ViewComponent
{
    private readonly AppDbContext _db;
    public CartCountViewComponent(AppDbContext db) => _db = db;

    public async Task<IViewComponentResult> InvokeAsync()
    {
        if (User.Identity?.IsAuthenticated != true ||
            !int.TryParse(UserClaimsPrincipal.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            return Content("0");

        var count = await _db.CartItems
            .Where(i => _db.Carts.Any(c => c.Id == i.CartId && c.UserId == userId))
            .SumAsync(i => (int?)i.Quantity) ?? 0;
        return Content(count.ToString());
    }
}
