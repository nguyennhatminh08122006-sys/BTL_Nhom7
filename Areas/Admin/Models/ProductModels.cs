using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace CanifaShop.Areas.Admin.Models;

public class ProductIndexVm
{
    public List<ProductRowVm> Products { get; set; } = new();
    public List<CategoryOptionVm> Categories { get; set; } = new();
    public string? Query { get; set; }
    public int? CategoryId { get; set; }
    public string Status { get; set; } = "";
    public int Page { get; set; }
    public int TotalPages { get; set; }
    public int TotalCount { get; set; }
}

public class ProductRowVm
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string CategoryName { get; set; } = "";
    public string CategoryIcon { get; set; } = "";
    public string? ImageUrl { get; set; }
    public string Color { get; set; } = "#EEEEEE";
    public decimal Price { get; set; }
    public decimal? OldPrice { get; set; }
    public int StockQuantity { get; set; }
    public bool IsActive { get; set; }
    public bool IsFeatured { get; set; }
}

public class CategoryOptionVm
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
}

public class ProductFormVm
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập tên sản phẩm.")]
    [StringLength(200, ErrorMessage = "Tên sản phẩm tối đa 200 ký tự.")]
    public string Name { get; set; } = "";

    [StringLength(2000, ErrorMessage = "Mô tả tối đa 2000 ký tự.")]
    public string Description { get; set; } = "";

    [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn danh mục.")]
    public int CategoryId { get; set; }

    [Range(typeof(decimal), "1", "999999999999999999", ErrorMessage = "Giá phải lớn hơn 0 và không vượt quá giới hạn lưu trữ.")]
    public decimal Price { get; set; }

    [Range(typeof(decimal), "1", "999999999999999999", ErrorMessage = "Giá gốc phải lớn hơn 0 và không vượt quá giới hạn lưu trữ.")]
    public decimal? OldPrice { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập ít nhất một size."), StringLength(100, ErrorMessage = "Tổng chuỗi size tối đa 100 ký tự.")]
    public string Sizes { get; set; } = "S,M,L,XL";

    [Range(0, int.MaxValue, ErrorMessage = "Tồn kho phải từ 0 trở lên.")]
    public int StockQuantity { get; set; }

    public bool IsActive { get; set; } = true;
    public bool IsFeatured { get; set; }

    [RegularExpression("^#[0-9A-Fa-f]{6}$", ErrorMessage = "Màu nền phải có dạng #RRGGBB.")]
    public string? Color { get; set; } = "#EEEEEE";

    public IFormFile? ImageFile { get; set; }
    public string? CurrentImageUrl { get; set; }
}
