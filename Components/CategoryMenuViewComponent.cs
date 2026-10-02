using CanifaShop.Data;
using CanifaShop.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CanifaShop.Components;

public class CategoryMenuViewComponent : ViewComponent
{
    private readonly AppDbContext _db;
    public CategoryMenuViewComponent(AppDbContext db) => _db = db;

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var categories = await _db.Categories.OrderBy(c => c.Id).ToListAsync();
        return View(categories);
    }
}
