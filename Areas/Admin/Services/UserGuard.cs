using CanifaShop.Data;
using Microsoft.EntityFrameworkCore;

namespace CanifaShop.Areas.Admin.Services;

public class UserGuard
{
    private readonly AppDbContext _db;
    public UserGuard(AppDbContext db) => _db = db;

    public async Task<UserGuardResult> CheckAsync(int targetId, int? currentUserId, bool willDeactivate)
    {
        var target = await _db.Users.AsNoTracking()
            .Where(u => u.Id == targetId)
            .Select(u => new { u.Id, u.FullName, u.Email, u.Role, u.IsLocked })
            .SingleOrDefaultAsync();
        if (target == null)
            return new UserGuardResult(false, "Không tìm thấy tài khoản.", "", "", "", true);

        if (willDeactivate && currentUserId == null)
            return new UserGuardResult(true, "Không thể xác định tài khoản đang đăng nhập để bảo vệ tài khoản này.", target.FullName, target.Email, target.Role, target.IsLocked);
        if (willDeactivate && currentUserId == targetId)
            return new UserGuardResult(true, "Bạn không thể khóa hoặc xóa chính tài khoản đang đăng nhập.", target.FullName, target.Email, target.Role, target.IsLocked);

        if (willDeactivate && target.Role == "Admin" && !target.IsLocked)
        {
            var activeAdminCount = await _db.Users.CountAsync(u => u.Role == "Admin" && !u.IsLocked);
            if (activeAdminCount <= 1)
                return new UserGuardResult(true, "Không thể thực hiện vì hệ thống phải luôn còn ít nhất một quản trị viên đang hoạt động.", target.FullName, target.Email, target.Role, target.IsLocked);
        }

        return new UserGuardResult(true, null, target.FullName, target.Email, target.Role, target.IsLocked);
    }
}

public record UserGuardResult(bool Found, string? Error, string FullName, string Email, string Role, bool IsLocked);
