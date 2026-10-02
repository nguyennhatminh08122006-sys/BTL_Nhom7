using System.ComponentModel.DataAnnotations;

namespace CanifaShop.Models;

public class RegisterVm
{
    [Required(ErrorMessage = "Vui lòng nhập họ tên"), StringLength(100)]
    [Display(Name = "Họ và tên")] public string FullName { get; set; } = "";

    [Required(ErrorMessage = "Vui lòng nhập email"), EmailAddress(ErrorMessage = "Email không hợp lệ")]
    public string Email { get; set; } = "";

    [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
    [Display(Name = "Số điện thoại")] public string? Phone { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập mật khẩu"), MinLength(6, ErrorMessage = "Mật khẩu tối thiểu 6 ký tự")]
    [DataType(DataType.Password)][Display(Name = "Mật khẩu")] public string Password { get; set; } = "";

    [Compare("Password", ErrorMessage = "Mật khẩu nhập lại không khớp")]
    [DataType(DataType.Password)][Display(Name = "Nhập lại mật khẩu")] public string ConfirmPassword { get; set; } = "";
}

public class LoginVm
{
    [Required(ErrorMessage = "Vui lòng nhập email"), EmailAddress(ErrorMessage = "Email không hợp lệ")]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "Vui lòng nhập mật khẩu")]
    [DataType(DataType.Password)][Display(Name = "Mật khẩu")] public string Password { get; set; } = "";
    public string? ReturnUrl { get; set; }
}

public class CheckoutVm
{
    [Required(ErrorMessage = "Vui lòng nhập tên người nhận"), StringLength(100)]
    [Display(Name = "Người nhận")] public string ReceiverName { get; set; } = "";

    [Required(ErrorMessage = "Vui lòng nhập số điện thoại"), Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
    [Display(Name = "Số điện thoại")] public string Phone { get; set; } = "";

    [Required(ErrorMessage = "Vui lòng nhập địa chỉ giao hàng"), StringLength(300)]
    [Display(Name = "Địa chỉ giao hàng")] public string Address { get; set; } = "";

    [StringLength(500)][Display(Name = "Ghi chú")] public string? Note { get; set; }
    public List<CartItemVm> CartItems { get; set; } = new();
    public decimal CartTotal => CartItems.Sum(i => i.Price * i.Quantity);
}

public class CartItemVm
{
    public int ProductId { get; set; }
    public string Name { get; set; } = "";
    public string? ImageUrl { get; set; }
    public string Color { get; set; } = "#EEEEEE";
    public string Icon { get; set; } = "";
    public decimal Price { get; set; }
    public string Size { get; set; } = "";
    public int Quantity { get; set; }
}

public class CartIndexVm
{
    public List<CartItemVm> Items { get; set; } = new();
    public decimal Total => Items.Sum(i => i.Price * i.Quantity);
}
