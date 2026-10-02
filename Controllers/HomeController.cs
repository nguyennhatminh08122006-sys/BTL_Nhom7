using CanifaShop.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CanifaShop.Controllers;

public class HomeController : Controller
{
    private readonly AppDbContext _db;
    public HomeController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index()
    {
        ViewBag.Categories = await _db.Categories.ToListAsync();
        var featured = await _db.Products.Include(p => p.Category)
            .Where(p => p.IsActive && p.IsFeatured).OrderByDescending(p => p.Id).Take(8).ToListAsync();
        return View(featured);
    }

    public IActionResult Error() => View();
}
