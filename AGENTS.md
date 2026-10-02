# AGENTS.md – CanifaShop

Hướng dẫn cho AI agent làm việc trong repo này. Đọc file này trước khi sửa code.

## 1. Tổng quan
Website bán thời trang (kiểu Canifa), dự án học tập.
- **Stack:** ASP.NET Core MVC (.NET 10), EF Core (SqlServer), Razor views, CSS thuần.
- **Database:** SQL Server LocalDB, tên `CanifaShopDb`. Cấu trúc chi tiết xem `database.md`.
- **Chạy:** `dotnet run` rồi mở `http://localhost:5000`.
- **Ngôn ngữ giao diện:** tiếng Việt. Giữ nguyên chữ tiếng Việt có dấu trong view và thông báo lỗi.

## 2. Cấu trúc thư mục
```
Controllers/   Home, Products, Cart, Account, Orders
Areas/Admin/  Controllers (BaseAdminController, Dashboard, Stats, Categories, Products, Orders, Users, Carts), Models, Services, Views
Components/    CartCountViewComponent.cs
               CategoryMenuViewComponent.cs
Models/        Entities.cs (Category, Product, User, Order, OrderItem, Cart, CartItem), ViewModels.cs
Areas/Admin/Models/ ProductModels.cs, OrderModels.cs, UserModels.cs, CartModels.cs, StatsVm.cs, DashboardVm.cs
Areas/Admin/Services/ UserGuard.cs
Models/        OrderStatus.cs (trạng thái và luồng chuyển đơn hàng)
Data/          AppDbContext.cs, SeedData.cs
Helpers/       CartHelper.cs (Format.Money())
Views/         Home, Products, Cart, Account, Orders, Shared (_Layout, _ProductCard)
wwwroot/css/   site.css
               admin.css
wwwroot/image/ ảnh sản phẩm (.jpg)
```

## 3. Lệnh thường dùng
```
dotnet build     # phải build sạch trước khi báo xong việc
dotnet run       # chạy thử ở http://localhost:5000
```
Không có test project. Kiểm tra bằng build và chạy tay trên trình duyệt.

## 4. Quy tắc về database (quan trọng)
- Database được tạo bằng `EnsureCreated()` trong `Program.cs`, **không dùng EF migrations**. Không chạy `dotnet ef migrations`.
- `EnsureCreated()` **không tự thêm cột** vào database đã tồn tại. Khi thêm/đổi thuộc tính trong `Models/Entities.cs`, phải đưa kèm câu `ALTER TABLE` cho người dùng tự chạy trong SSMS, và cập nhật `database.md`.
- Không xóa hoặc tạo lại database của người dùng. Không chạy lệnh SQL phá dữ liệu (`DROP`, `TRUNCATE`, `DELETE` không có `WHERE`).
- Giá tiền dùng `decimal(18,0)` (VND). Hiển thị bằng `.Money()` trong `Helpers/CartHelper.cs`.
- Giỏ hàng lưu trong `Carts`/`CartItems`, mỗi User có một giỏ; không lưu giỏ trong session.
- Giỏ chỉ lưu `ProductId`, `Size`, `Quantity`; tên, ảnh, giá và tồn kho luôn đọc từ `Products`.
- Đặt hàng kiểm tra lại sản phẩm và tồn kho trong transaction, cập nhật tồn kho có điều kiện rồi xóa các dòng đã đặt khỏi giỏ.
- Trạng thái đơn hàng dùng tập trung trong `Models/OrderStatus.cs`: Mới → Đã xác nhận → Đang giao → Hoàn thành; đơn chưa hoàn thành có thể chuyển sang Đã hủy. Hoàn thành và Đã hủy là trạng thái kết thúc.
- Hủy đơn Admin cập nhật trạng thái và hoàn tồn kho theo số lượng từng sản phẩm trong cùng transaction; chỉ hoàn tồn đúng một lần khi chuyển trạng thái hợp lệ sang Đã hủy.
- Tài khoản Admin không được tự khóa/xóa; luôn giữ ít nhất một Admin đang hoạt động. Kiểm tra ở server trước các thao tác khóa/xóa.
- Không truy vấn hoặc hiển thị `PasswordHash` trong khu vực Admin; danh sách và chi tiết tài khoản chỉ dùng projection các trường cần thiết.
- Thống kê doanh thu chỉ tính `Orders.Total` của đơn `Hoàn thành`, theo ngày đặt `CreatedAt`; doanh thu sản phẩm/danh mục là tổng `OrderItems.UnitPrice × Quantity`, không gồm phí giao hàng và giảm giá.
- Trang thống kê dùng Chart.js qua CDN ghim phiên bản, chỉ tải script trong trang đó; không thêm gói NuGet/npm cho biểu đồ.

## 5. Ảnh sản phẩm
- Ảnh nằm trong `wwwroot/image/`, đường dẫn trong DB dạng `/image/Aopolonam_image.jpg` (bắt đầu bằng `/image/`, không có `wwwroot`).
- Cột `Products.ImageUrl` (nullable) lưu đường dẫn. Class `Product` và view model giỏ hàng dùng thuộc tính `ImageUrl`.
- Luôn có phương án dự phòng: nếu `ImageUrl` rỗng thì hiện ô màu (`Color`) và icon (`Category.Icon`) như thiết kế gốc.
- Tên file phân biệt đúng chữ hoa/thường và đuôi file. Khi ảnh không hiện, kiểm tra khớp tên trước khi sửa code.
- Upload ảnh admin chỉ nhận `.jpg`, `.jpeg`, `.png`, `.webp`, tối đa 2 MB; phải kiểm tra magic bytes, tên file do server tạo (GUID), không dùng tên client.
- Chỉ xóa ảnh upload do server tạo khi file nằm trong `wwwroot/image/` và không còn sản phẩm nào dùng; không xóa ảnh mẫu hoặc ảnh đang được dùng.

## 6. Quy ước code
- Bật `Nullable` và `ImplicitUsings`. Tránh cảnh báo nullable mới.
- Giữ phong cách hiện có: namespace file-scoped, controller dùng `AppDbContext` inject qua constructor, action bất đồng bộ dùng `async/await`.
- Form POST dùng tag helper (`asp-action`, `method="post"`) để có antiforgery token, controller giữ `[ValidateAntiForgeryToken]`.
- Mật khẩu chỉ lưu dạng hash (`PasswordHasher<User>`). Không bao giờ ghi mật khẩu thô vào code hay log.
- Giá khi đặt hàng luôn lấy lại từ database, không tin giá từ giỏ hàng (xem `OrdersController.Checkout`).
- CSS viết trong `wwwroot/css/site.css`, dùng biến `:root` có sẵn (`--red`, `--ink`, `--line`...). Không thêm framework CSS/JS mới nếu không được yêu cầu.

## 7. Giới hạn phạm vi
- Chỉ sửa các file liên quan đến yêu cầu. Nếu cần sửa thêm file khác, nêu rõ lý do trước.
- Không đổi tên bảng/cột đang có, không đổi route, không thêm NuGet package nếu không được yêu cầu.
- Không sửa logic đăng nhập, đặt hàng khi nhiệm vụ không nói tới.
- Không commit hay push git nếu người dùng không yêu cầu.
- Mọi controller trong `Areas/Admin/Controllers` phải kế thừa `BaseAdminController`, có `[Area("Admin")]` và `[Authorize(Roles = "Admin")]` áp dụng từ lớp nền.

## 8. Vấn đề đã biết
- Khu vực Admin hiện có Dashboard và quản lý danh mục/sản phẩm/đơn hàng/tài khoản/giỏ hàng.
- `Carts` và `CartItems` đã có trong database/model; ứng dụng lưu giỏ trong database, không dùng session.

## 9. Tài khoản thử
`demo@canifa.vn` / `Demo@123`

## 10. Định nghĩa "xong việc"
1. `dotnet build` không lỗi.
2. Chạy `dotnet run` và kiểm tra tay các trang bị ảnh hưởng: trang chủ, danh sách sản phẩm, chi tiết, giỏ hàng, đặt hàng.
3. Nếu có thay đổi cấu trúc database: đã đưa câu SQL và cập nhật `database.md`.
4. Báo cáo: liệt kê file đã sửa, tóm tắt thay đổi từng file, và nêu việc người dùng cần tự làm (ví dụ chạy SQL).
