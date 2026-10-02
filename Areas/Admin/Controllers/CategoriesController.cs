using CanifaShop.Areas.Admin.Models;
using CanifaShop.Data;
using CanifaShop.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CanifaShop.Areas.Admin.Controllers;

public class CategoriesController : BaseAdminController
{
    private readonly AppDbContext _db;
    public CategoriesController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index()
    {
        var categories = await _db.Categories
            .OrderBy(c => c.Id)
            .Select(c => new CategoryRowVm
            {
                Id = c.Id,
                Name = c.Name,
                Slug = c.Slug,
                Icon = c.Icon,
                ProductCount = c.Products.Count
            })
            .ToListAsync();
        return View(categories);
    }

    [HttpGet]
    public IActionResult Create() => View(new CategoryFormVm());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CategoryFormVm vm)
    {
        vm.Name = vm.Name.Trim();
        vm.Slug = vm.Slug.Trim();
        vm.Icon = string.IsNullOrWhiteSpace(vm.Icon) ? null : vm.Icon.Trim();
        if (await _db.Categories.AnyAsync(c => c.Slug == vm.Slug))
            ModelState.AddModelError(nameof(vm.Slug), "Slug này đã được sử dụng.");
        if (!ModelState.IsValid) return View(vm);

        var category = new Category { Name = vm.Name, Slug = vm.Slug, Icon = vm.Icon ?? "" };
        _db.Categories.Add(category);
        try
        {
            await _db.SaveChangesAsync();
            TempData["Success"] = "Đã thêm danh mục.";
            return RedirectToAction(nameof(Index));
        }
        catch (DbUpdateException)
        {
            _db.Entry(category).State = EntityState.Detached;
            ModelState.AddModelError(nameof(vm.Slug), "Không thể lưu danh mục. Hãy kiểm tra slug có bị trùng không.");
            return View(vm);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var category = await _db.Categories.FindAsync(id);
        if (category == null) return NotFound();
        return View(new CategoryFormVm
        {
            Id = category.Id,
            Name = category.Name,
            Slug = category.Slug,
            Icon = category.Icon
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, CategoryFormVm vm)
    {
        if (id != vm.Id) return BadRequest();
        vm.Name = vm.Name.Trim();
        vm.Slug = vm.Slug.Trim();
        vm.Icon = string.IsNullOrWhiteSpace(vm.Icon) ? null : vm.Icon.Trim();
        if (await _db.Categories.AnyAsync(c => c.Id != id && c.Slug == vm.Slug))
            ModelState.AddModelError(nameof(vm.Slug), "Slug này đã được sử dụng.");
        if (!ModelState.IsValid) return View(vm);

        var category = await _db.Categories.FindAsync(id);
        if (category == null) return NotFound();
        category.Name = vm.Name;
        category.Slug = vm.Slug;
        category.Icon = vm.Icon ?? "";

        try
        {
            await _db.SaveChangesAsync();
            TempData["Success"] = "Đã cập nhật danh mục.";
            return RedirectToAction(nameof(Index));
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError(nameof(vm.Slug), "Không thể lưu danh mục. Hãy kiểm tra slug có bị trùng không.");
            return View(vm);
        }
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var category = await _db.Categories.FindAsync(id);
        if (category == null)
        {
            TempData["Error"] = "Không tìm thấy danh mục cần xóa.";
            return RedirectToAction(nameof(Index));
        }

        var productCount = await _db.Products.CountAsync(p => p.CategoryId == id);
        if (productCount > 0)
        {
            TempData["Error"] = $"Danh mục còn {productCount} sản phẩm, hãy chuyển hoặc xóa sản phẩm trước";
            return RedirectToAction(nameof(Index));
        }

        _db.Categories.Remove(category);
        try
        {
            await _db.SaveChangesAsync();
            TempData["Success"] = "Đã xóa danh mục.";
        }
        catch (DbUpdateException)
        {
            TempData["Error"] = "Không thể xóa danh mục vì dữ liệu liên quan vẫn đang được sử dụng.";
        }

        return RedirectToAction(nameof(Index));
    }
}
