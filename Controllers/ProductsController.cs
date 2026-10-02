using CanifaShop.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CanifaShop.Controllers;

public class ProductsController : Controller
{
    private readonly AppDbContext _db;
    public ProductsController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index(string? category, string? q, string? sort)
    {
        var query = _db.Products.Include(p => p.Category).Where(p => p.IsActive).AsQueryable();
        if (!string.IsNullOrEmpty(category)) query = query.Where(p => p.Category!.Slug == category);
        if (!string.IsNullOrWhiteSpace(q)) query = query.Where(p => p.Name.Contains(q));

        query = sort switch
        {
            "price_asc" => query.OrderBy(p => p.Price),
            "price_desc" => query.OrderByDescending(p => p.Price),
            _ => query.OrderByDescending(p => p.Id)
        };

        ViewBag.Categories = await _db.Categories.ToListAsync();
        ViewBag.Current = category;
        ViewBag.Q = q;
        ViewBag.Sort = sort;
        return View(await query.ToListAsync());
    }

    public async Task<IActionResult> Details(int id)
    {
        var product = await _db.Products.Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.Id == id && p.IsActive);
        if (product == null) return NotFound();
        ViewBag.Related = await _db.Products.Include(p => p.Category)
            .Where(p => p.IsActive && p.CategoryId == product.CategoryId && p.Id != id).Take(4).ToListAsync();
        return View(product);
    }
}
