using System.Security.Claims;
using CanifaShop.Data;
using CanifaShop.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CanifaShop.Controllers;

public class AccountController : Controller
{
    private readonly AppDbContext _db;
    private readonly PasswordHasher<User> _hasher = new();
    public AccountController(AppDbContext db) => _db = db;

    [HttpGet] public IActionResult Register() => View(new RegisterVm());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterVm vm)
    {
        if (!ModelState.IsValid) return View(vm);
        var email = vm.Email.Trim().ToLower();
        if (await _db.Users.AnyAsync(u => u.Email == email))
        {
            ModelState.AddModelError("Email", "Email này đã được đăng ký.");
            return View(vm);
        }
        var user = new User { FullName = vm.FullName.Trim(), Email = email, Phone = vm.Phone };
        user.PasswordHash = _hasher.HashPassword(user, vm.Password);
        _db.Users.Add(user);
        await _db.SaveChangesAsync();
        await SignIn(user);
        return RedirectToAction("Index", "Home");
    }

    [HttpGet] public IActionResult Login(string? returnUrl) => View(new LoginVm { ReturnUrl = returnUrl });

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginVm vm)
    {
        if (!ModelState.IsValid) return View(vm);
        var email = vm.Email.Trim().ToLower();
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user == null)
        {
            ModelState.AddModelError("", "Email hoặc mật khẩu không đúng.");
            return View(vm);
        }
        if (user.IsLocked)
        {
            ModelState.AddModelError("", "Tài khoản của bạn đã bị khóa. Vui lòng liên hệ quản trị viên.");
            return View(vm);
        }
        if (_hasher.VerifyHashedPassword(user, user.PasswordHash, vm.Password) == PasswordVerificationResult.Failed)
        {
            ModelState.AddModelError("", "Email hoặc mật khẩu không đúng.");
            return View(vm);
        }
        await SignIn(user);
        if (!string.IsNullOrEmpty(vm.ReturnUrl) && Url.IsLocalUrl(vm.ReturnUrl)) return Redirect(vm.ReturnUrl);
        return RedirectToAction("Index", "Home");
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Index", "Home");
    }

    private Task SignIn(User user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.FullName),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.Role)
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        return HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));
    }
}
