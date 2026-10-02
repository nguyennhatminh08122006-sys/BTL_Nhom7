# Canifa Shop – ASP.NET Core MVC (.NET 8) + SQL Server LocalDB

## Yêu cầu
- .NET 8 SDK
- SQL Server LocalDB (đi kèm Visual Studio) hoặc SQL Server Express

## Chạy
```
cd CanifaShop
dotnet run
```
Mở Chrome: http://localhost:5000

Lần chạy đầu tiên ứng dụng tự tạo database `CanifaShopDb` (các bảng Categories, Products, Users, Orders, OrderItems) và thêm 16 sản phẩm mẫu.

Tài khoản demo: demo@canifa.vn / Demo@123

## Dùng SQL Server Express thay vì LocalDB
Sửa `ConnectionStrings:Default` trong `appsettings.json`, ví dụ:
`Server=.\SQLEXPRESS;Database=CanifaShopDb;Trusted_Connection=True;TrustServerCertificate=True`

## Chức năng
- Trang chủ, danh sách sản phẩm, lọc theo danh mục, tìm kiếm, sắp xếp giá
- Chi tiết sản phẩm, chọn size, thêm vào giỏ
- Giỏ hàng (lưu theo session): cập nhật số lượng, xóa
- Đăng ký, đăng nhập (mật khẩu được băm), đăng xuất
- Đặt hàng (COD), lịch sử đơn hàng
