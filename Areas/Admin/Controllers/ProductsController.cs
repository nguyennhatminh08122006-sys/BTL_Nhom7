using CanifaShop.Areas.Admin.Models;
using CanifaShop.Data;
using CanifaShop.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CanifaShop.Areas.Admin.Controllers;

public class ProductsController : BaseAdminController
{
    private const int PageSize = 10;
    private const long MaxImageBytes = 2 * 1024 * 1024;
    private readonly AppDbContext _db;
    private readonly IWebHostEnvironment _environment;

    public ProductsController(AppDbContext db, IWebHostEnvironment environment)
    {
        _db = db;
        _environment = environment;
    }

    public async Task<IActionResult> Index(string? q, int? categoryId, string? status, int page = 1)
    {
        q = q?.Trim();
        status = status is "selling" or "out-of-stock" or "inactive" ? status : "";
        var query = _db.Products.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(q)) query = query.Where(p => p.Name.Contains(q));
        if (categoryId.HasValue) query = query.Where(p => p.CategoryId == categoryId.Value);
        query = status switch
        {
            "selling" => query.Where(p => p.IsActive && p.StockQuantity > 0),
            "out-of-stock" => query.Where(p => p.IsActive && p.StockQuantity == 0),
            "inactive" => query.Where(p => !p.IsActive),
            _ => query
        };

        var totalCount = await query.CountAsync();
        var totalPages = Math.Max(1, (int)Math.Ceiling(totalCount / (double)PageSize));
        page = Math.Clamp(page, 1, totalPages);

        var model = new ProductIndexVm
        {
            Query = q,
            CategoryId = categoryId,
            Status = status,
            Page = page,
            TotalPages = totalPages,
            TotalCount = totalCount,
            Categories = await LoadCategoriesAsync(),
            Products = await query
                .OrderByDescending(p => p.Id)
                .Skip((page - 1) * PageSize)
                .Take(PageSize)
                .Select(p => new ProductRowVm
                {
                    Id = p.Id,
                    Name = p.Name,
                    CategoryName = p.Category!.Name,
                    CategoryIcon = p.Category.Icon,
                    ImageUrl = p.ImageUrl,
                    Color = p.Color,
                    Price = p.Price,
                    OldPrice = p.OldPrice,
                    StockQuantity = p.StockQuantity,
                    IsActive = p.IsActive,
                    IsFeatured = p.IsFeatured
                })
                .ToListAsync()
        };

        return View(model);
    }

    public async Task<IActionResult> Details(int id)
    {
        var product = await _db.Products.AsNoTracking()
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.Id == id);
        if (product == null) return NotFound();

        ViewBag.OrderCount = await _db.OrderItems.Where(i => i.ProductId == id)
            .Select(i => i.OrderId).Distinct().CountAsync();
        ViewBag.CartCount = await _db.CartItems.Where(i => i.ProductId == id)
            .Select(i => i.CartId).Distinct().CountAsync();
        return View(product);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        await LoadCategoryOptionsAsync();
        return View(new ProductFormVm());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ProductFormVm vm)
    {
        await ValidateFormAsync(vm);
        if (!ModelState.IsValid)
        {
            await LoadCategoryOptionsAsync();
            return View(vm);
        }

        string? imageUrl = null;
        if (HasUpload(vm.ImageFile))
        {
            try
            {
                imageUrl = await SaveImageAsync(vm.ImageFile!);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                ModelState.AddModelError(nameof(vm.ImageFile), "Không thể lưu ảnh. Vui lòng thử lại.");
                await LoadCategoryOptionsAsync();
                return View(vm);
            }
        }

        var product = new Product
        {
            Name = vm.Name.Trim(),
            Description = vm.Description.Trim(),
            CategoryId = vm.CategoryId,
            Price = vm.Price,
            OldPrice = vm.OldPrice,
            Sizes = vm.Sizes,
            StockQuantity = vm.StockQuantity,
            IsActive = vm.IsActive,
            IsFeatured = vm.IsFeatured,
            Color = vm.Color!,
            ImageUrl = imageUrl
        };
        _db.Products.Add(product);
        try
        {
            await _db.SaveChangesAsync();
            TempData["Success"] = "Đã thêm sản phẩm.";
            return RedirectToAction(nameof(Index));
        }
        catch (DbUpdateException)
        {
            if (imageUrl != null) TryDeleteGeneratedImage(imageUrl);
            _db.Entry(product).State = EntityState.Detached;
            ModelState.AddModelError("", "Không thể lưu sản phẩm. Vui lòng kiểm tra dữ liệu và thử lại.");
            await LoadCategoryOptionsAsync();
            return View(vm);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var product = await _db.Products.FindAsync(id);
        if (product == null) return NotFound();
        await LoadCategoryOptionsAsync();
        return View(ToFormVm(product));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ProductFormVm vm)
    {
        if (id != vm.Id) return BadRequest();
        var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == id);
        if (product == null) return NotFound();
        vm.CurrentImageUrl = product.ImageUrl;
        await ValidateFormAsync(vm);
        if (!ModelState.IsValid)
        {
            await LoadCategoryOptionsAsync();
            return View(vm);
        }

        string? newImageUrl = null;
        if (HasUpload(vm.ImageFile))
        {
            try
            {
                newImageUrl = await SaveImageAsync(vm.ImageFile!);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                ModelState.AddModelError(nameof(vm.ImageFile), "Không thể lưu ảnh. Vui lòng thử lại.");
                await LoadCategoryOptionsAsync();
                return View(vm);
            }
        }

        var oldImageUrl = product.ImageUrl;
        product.Name = vm.Name.Trim();
        product.Description = vm.Description.Trim();
        product.CategoryId = vm.CategoryId;
        product.Price = vm.Price;
        product.OldPrice = vm.OldPrice;
        product.Sizes = vm.Sizes;
        product.StockQuantity = vm.StockQuantity;
        product.IsActive = vm.IsActive;
        product.IsFeatured = vm.IsFeatured;
        product.Color = vm.Color!;
        if (newImageUrl != null) product.ImageUrl = newImageUrl;

        try
        {
            await _db.SaveChangesAsync();
            if (newImageUrl != null && oldImageUrl != null)
                await DeleteReplacedImageIfUnusedAsync(oldImageUrl, id);
            TempData["Success"] = "Đã cập nhật sản phẩm.";
            return RedirectToAction(nameof(Index));
        }
        catch (DbUpdateException)
        {
            if (newImageUrl != null) TryDeleteGeneratedImage(newImageUrl);
            ModelState.AddModelError("", "Không thể lưu thay đổi sản phẩm. Vui lòng thử lại.");
            await LoadCategoryOptionsAsync();
            return View(vm);
        }
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(
        int id, string? q, int? categoryId, string? status, int page = 1)
    {
        var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == id);
        if (product == null)
        {
            TempData["Error"] = "Không tìm thấy sản phẩm.";
        }
        else
        {
            product.IsActive = !product.IsActive;
            await _db.SaveChangesAsync();
            TempData["Success"] = product.IsActive ? "Đã hiện sản phẩm." : "Đã ẩn sản phẩm.";
        }

        return RedirectToAction(nameof(Index), new { q, categoryId, status, page });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, string? q, int? categoryId, string? status, int page = 1)
    {
        var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == id);
        if (product == null)
        {
            TempData["Error"] = "Không tìm thấy sản phẩm cần xóa.";
            return RedirectToAction(nameof(Index), new { q, categoryId, status, page });
        }

        if (await _db.OrderItems.AnyAsync(i => i.ProductId == id))
        {
            TempData["Error"] = "Sản phẩm đã có trong đơn hàng, hãy chuyển sang Ngừng bán. Hãy dùng nút Ẩn để ngừng bán sản phẩm.";
            return RedirectToAction(nameof(Index), new { q, categoryId, status, page });
        }

        await using var transaction = await _db.Database.BeginTransactionAsync();
        await _db.CartItems.Where(i => i.ProductId == id).ExecuteDeleteAsync();
        _db.Products.Remove(product);
        try
        {
            await _db.SaveChangesAsync();
            await transaction.CommitAsync();
            TempData["Success"] = "Đã xóa sản phẩm.";
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync();
            TempData["Error"] = "Không thể xóa sản phẩm vì sản phẩm đang được dữ liệu khác sử dụng.";
        }

        return RedirectToAction(nameof(Index), new { q, categoryId, status, page });
    }

    private async Task<bool> ValidateFormAsync(ProductFormVm vm)
    {
        vm.Name = vm.Name?.Trim() ?? "";
        vm.Description = vm.Description?.Trim() ?? "";
        vm.Color = string.IsNullOrWhiteSpace(vm.Color) ? "#EEEEEE" : vm.Color.Trim();
        vm.Sizes = NormalizeSizes(vm.Sizes, out var sizeError);
        if (sizeError != null) ModelState.AddModelError(nameof(vm.Sizes), sizeError);
        if (vm.Price != decimal.Truncate(vm.Price))
            ModelState.AddModelError(nameof(vm.Price), "Giá phải là số nguyên VND.");
        if (vm.OldPrice.HasValue && vm.OldPrice.Value != decimal.Truncate(vm.OldPrice.Value))
            ModelState.AddModelError(nameof(vm.OldPrice), "Giá gốc phải là số nguyên VND.");
        if (vm.OldPrice.HasValue && vm.OldPrice.Value <= vm.Price)
            ModelState.AddModelError(nameof(vm.OldPrice), "Giá gốc phải lớn hơn giá bán.");
        if (!await _db.Categories.AnyAsync(c => c.Id == vm.CategoryId))
            ModelState.AddModelError(nameof(vm.CategoryId), "Danh mục đã chọn không tồn tại.");

        var imageError = await ValidateImageAsync(vm.ImageFile);
        if (imageError != null) ModelState.AddModelError(nameof(vm.ImageFile), imageError);
        return ModelState.IsValid;
    }

    private static string NormalizeSizes(string? value, out string? error)
    {
        error = null;
        var sizes = (value ?? "")
            .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (sizes.Count == 0)
        {
            error = "Vui lòng nhập ít nhất một size.";
            return "";
        }
        if (sizes.Any(s => s.Length > 20))
            error = "Mỗi size tối đa 20 ký tự.";

        var normalized = string.Join(',', sizes);
        if (normalized.Length > 100)
            error = "Tổng chuỗi size sau khi chuẩn hóa tối đa 100 ký tự.";
        return normalized;
    }

    private static bool HasUpload(IFormFile? file)
        => file != null && !string.IsNullOrEmpty(file.FileName);

    private static async Task<string?> ValidateImageAsync(IFormFile? file)
    {
        if (!HasUpload(file)) return null;
        if (file!.Length == 0) return "Tệp ảnh rỗng.";
        if (file.Length > MaxImageBytes) return "Ảnh không được vượt quá 2 MB.";

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (extension is not (".jpg" or ".jpeg" or ".png" or ".webp"))
            return "Chỉ nhận ảnh .jpg, .jpeg, .png hoặc .webp.";

        var header = new byte[12];
        var count = 0;
        await using (var stream = file.OpenReadStream())
        {
            while (count < header.Length)
            {
                var read = await stream.ReadAsync(header.AsMemory(count));
                if (read == 0) break;
                count += read;
            }
        }

        var isJpeg = count >= 3 && header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF;
        var isPng = count >= 8 && header.AsSpan(0, 8).SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A });
        var isWebp = count >= 12 && header.AsSpan(0, 4).SequenceEqual("RIFF"u8) && header.AsSpan(8, 4).SequenceEqual("WEBP"u8);
        var matchesExtension = extension switch
        {
            ".jpg" or ".jpeg" => isJpeg,
            ".png" => isPng,
            ".webp" => isWebp,
            _ => false
        };
        return matchesExtension ? null : "Nội dung tệp không khớp với định dạng ảnh đã chọn.";
    }

    private async Task<string> SaveImageAsync(IFormFile file)
    {
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var fileName = $"{Guid.NewGuid():N}{extension}";
        var directory = GetImageDirectory();
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, fileName);
        try
        {
            await using var output = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None);
            await file.CopyToAsync(output);
        }
        catch
        {
            TryDeletePath(path);
            throw;
        }
        return $"/image/{fileName}";
    }

    private async Task DeleteReplacedImageIfUnusedAsync(string oldImageUrl, int productId)
    {
        var fileName = GetSafeManagedFileName(oldImageUrl);
        if (fileName == null) return;
        if (await _db.Products.AnyAsync(p => p.Id != productId && p.ImageUrl == oldImageUrl)) return;
        TryDeletePath(Path.Combine(GetImageDirectory(), fileName));
    }

    private string? GetSafeManagedFileName(string imageUrl)
    {
        const string prefix = "/image/";
        if (!imageUrl.StartsWith(prefix, StringComparison.Ordinal)) return null;
        var fileName = imageUrl[prefix.Length..];
        if (Path.GetFileName(fileName) != fileName) return null;

        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        var stem = Path.GetFileNameWithoutExtension(fileName);
        if (extension is not (".jpg" or ".jpeg" or ".png" or ".webp") ||
            !Guid.TryParseExact(stem, "N", out _))
            return null;
        return fileName;
    }

    private string GetImageDirectory()
    {
        var webRoot = _environment.WebRootPath ?? Path.Combine(_environment.ContentRootPath, "wwwroot");
        return Path.GetFullPath(Path.Combine(webRoot, "image"));
    }

    private void TryDeleteGeneratedImage(string imageUrl)
    {
        var fileName = GetSafeManagedFileName(imageUrl);
        if (fileName != null) TryDeletePath(Path.Combine(GetImageDirectory(), fileName));
    }

    private static void TryDeletePath(string path)
    {
        try
        {
            if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }

    private async Task LoadCategoryOptionsAsync()
    {
        ViewBag.Categories = await _db.Categories.OrderBy(c => c.Id)
            .Select(c => new CategoryOptionVm { Id = c.Id, Name = c.Name })
            .ToListAsync();
    }

    private Task<List<CategoryOptionVm>> LoadCategoriesAsync()
        => _db.Categories.OrderBy(c => c.Id)
            .Select(c => new CategoryOptionVm { Id = c.Id, Name = c.Name })
            .ToListAsync();

    private static ProductFormVm ToFormVm(Product product)
        => new()
        {
            Id = product.Id,
            Name = product.Name,
            Description = product.Description,
            CategoryId = product.CategoryId,
            Price = product.Price,
            OldPrice = product.OldPrice,
            Sizes = product.Sizes,
            StockQuantity = product.StockQuantity,
            IsActive = product.IsActive,
            IsFeatured = product.IsFeatured,
            Color = product.Color,
            CurrentImageUrl = product.ImageUrl
        };
}
