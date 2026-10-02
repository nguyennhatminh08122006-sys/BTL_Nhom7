# Nhiệm vụ: quản lý tài khoản trong khu vực Admin (xem, khóa/mở khóa, xóa)

Đọc `AGENTS.md` và `database.md` trước.

Quản lý danh mục, sản phẩm, đơn hàng đã xong. Làm theo cùng cách: kế thừa `BaseAdminController`, view model riêng, `TempData` cho thông báo, form POST có antiforgery. Không sửa phần cửa hàng, giỏ hàng, đặt hàng.

## 1. Quy tắc bảo vệ (đặt tập trung, một chỗ duy nhất)
Tạo lớp/dịch vụ nhỏ (ví dụ `Areas/Admin/Services/UserGuard.cs` hoặc hàm dùng chung) chứa các kiểm tra sau. Mọi action khóa/xóa đều gọi:
- Admin **không được khóa hoặc xóa chính tài khoản đang đăng nhập** (so sánh với claim `NameIdentifier`).
- Hệ thống phải luôn còn **ít nhất một tài khoản Admin đang hoạt động** (`Role = "Admin"` và `IsLocked = false`). Không được khóa hoặc xóa tài khoản nếu làm cho số admin hoạt động còn lại bằng 0.
- Thông báo lỗi bằng tiếng Việt, nêu rõ lý do.

Các kiểm tra này phải chạy ở server, không dựa vào việc ẩn nút trên giao diện (ẩn nút chỉ là phụ).

## 2. Danh sách (`Areas/Admin/Controllers/UsersController.cs`, action `Index`)
- Cột: Id, Họ tên, Email, Số điện thoại, Vai trò (nhãn `Admin` / `Customer`), Trạng thái (nhãn "Hoạt động" / "Đã khóa" có màu khác nhau), Số đơn hàng, thao tác (Xem / Khóa-Mở khóa / Xóa).
- Bộ lọc qua query string: tìm theo họ tên / email / số điện thoại (`q`), theo vai trò (Tất cả / Admin / Customer), theo trạng thái (Tất cả / Hoạt động / Đã khóa). Giữ bộ lọc khi chuyển trang.
- Sắp xếp Id giảm dần. Phân trang 10 tài khoản mỗi trang, xử lý số trang không hợp lệ không gây lỗi.
- Với tài khoản đang đăng nhập (chính admin), hiện nhãn "Bạn" và **ẩn nút Khóa/Xóa**. Với các trường hợp vi phạm quy tắc ở mục 1, vô hiệu hóa nút kèm gợi ý ngắn.
- **Tuyệt đối không đọc hoặc hiển thị `PasswordHash`.** Dùng projection (`Select`) sang view model chỉ gồm các cột cần thiết, không tải toàn bộ entity `User` ra view.

## 3. Xem chi tiết (`Details`)
- Thông tin tài khoản: Id, họ tên, email, số điện thoại, vai trò, trạng thái.
- Thống kê: tổng số đơn hàng, tổng tiền các đơn không bị hủy (bỏ qua đơn `Đã hủy`), số đơn theo từng trạng thái, số loại sản phẩm đang có trong giỏ hàng (từ `CartItems`).
- Danh sách tối đa 10 đơn hàng gần nhất (mã đơn, ngày, tổng tiền, trạng thái). Mỗi đơn có link sang `Admin/Orders/Details/{id}`.
- Khu vực thao tác: nút Khóa hoặc Mở khóa, nút Xóa (theo quy tắc mục 1), nút Quay lại. Không hiển thị `PasswordHash`.

## 4. Khóa / Mở khóa (`ToggleLock`)
- Chỉ POST, `[ValidateAntiForgeryToken]`. Nhận `id` và hành động (khóa hoặc mở khóa).
- Áp dụng quy tắc mục 1 khi **khóa**. Mở khóa luôn được phép.
- Cập nhật có điều kiện để chống bấm trùng/ghi đè, ví dụ `ExecuteUpdateAsync` với `Where(u => u.Id == id && u.IsLocked == trangThaiHienTai)` rồi kiểm tra số dòng bị ảnh hưởng. Nếu bằng 0, báo "Tài khoản vừa được cập nhật bởi người khác, vui lòng tải lại".
- Sau khi khóa, người đó không đăng nhập được, và nếu đang đăng nhập ở nơi khác thì bị đăng xuất ở request kế tiếp. Cơ chế kiểm tra cookie đã làm ở bước phân quyền, **không viết lại**, chỉ xác nhận nó vẫn hoạt động.
- Quay lại đúng trang đang xem (danh sách giữ nguyên bộ lọc, hoặc trang chi tiết) và hiện thông báo thành công bằng `TempData`.

## 5. Xóa (`Delete`)
- Chỉ POST, `[ValidateAntiForgeryToken]`, có bước xác nhận (hộp thoại) nêu rõ họ tên và email của tài khoản.
- Áp dụng quy tắc mục 1.
- **Tài khoản đã có đơn hàng: không xóa.** Báo "Tài khoản đã có X đơn hàng, không thể xóa. Hãy dùng chức năng Khóa." Kiểm tra bằng cách đếm trước, và bắt `DbUpdateException` làm lớp bảo vệ thứ hai (database dùng NO ACTION giữa `Orders` và `Users`).
- **Tài khoản chưa có đơn hàng: xóa được.** Giỏ hàng và các dòng giỏ của họ tự bị xóa theo cascade. Dùng transaction nếu cần.

## 6. Không làm những việc ngoài use case
Không thêm đổi vai trò, đặt lại mật khẩu, tạo tài khoản từ trang admin, hay chỉnh sửa thông tin tài khoản. Use case chỉ gồm xem, khóa và xóa.

## 7. Sidebar và trang tổng quan
- `_AdminLayout.cshtml`: bật mục "Tài khoản" trỏ tới `Admin/Users`. Chỉ còn "Giỏ hàng" ở trạng thái "Sắp có".
- `Dashboard`: các bộ đếm tài khoản (Customer / Admin / bị khóa) thêm link sang danh sách đã lọc tương ứng.

## Ràng buộc
- Không đổi cấu trúc database, không thêm migrations. Nếu thấy cần (ví dụ lưu ngày tạo tài khoản), dừng lại và hỏi.
- Không sửa `AccountController`, `CartController`, `OrdersController`.
- Mọi action ghi dữ liệu là POST có `[ValidateAntiForgeryToken]`. Không đặt `@if` dính liền chữ phía trước trong Razor.
- Giữ code style, giao diện admin hiện có, thông báo và nhãn bằng tiếng Việt có dấu.
- Không tạo trang Giỏ hàng ở bước này.

## Khi xong
1. `dotnet build` sạch, không cảnh báo mới.
2. Cập nhật `AGENTS.md`: ghi quy tắc bảo vệ tài khoản (không tự khóa/xóa mình, luôn còn ít nhất một admin hoạt động, không hiển thị `PasswordHash`).
3. Báo cáo file đã tạo/sửa, tóm tắt từng file và các điểm chưa chắc chắn.
