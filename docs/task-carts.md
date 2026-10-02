# Nhiệm vụ: quản lý giỏ hàng trong khu vực Admin (xem, xóa)

Đọc `AGENTS.md` và `database.md` trước.

Quản lý danh mục, sản phẩm, đơn hàng, tài khoản đã xong. Làm theo cùng cách: kế thừa `BaseAdminController`, view model riêng, `TempData` cho thông báo, form POST có antiforgery. Đây là chức năng admin cuối cùng. Không sửa phần cửa hàng, đặt hàng.

## Dữ liệu liên quan
- `Carts(Id, UserId UNIQUE, CreatedAt)`: mỗi tài khoản một giỏ.
- `CartItems(Id, CartId, ProductId, Size, Quantity)`: không lưu tên và giá, luôn lấy từ `Products`.
- Xóa `Carts` thì `CartItems` tự xóa theo cascade.

## 1. Danh sách (`Areas/Admin/Controllers/CartsController.cs`, action `Index`)
- Cột: Mã giỏ, Khách hàng (họ tên và email), Số dòng hàng, Tổng số lượng, Giá trị ước tính, Ngày tạo, thao tác (Xem / Xóa).
- **Giá trị ước tính** = tổng (`Products.Price` × `Quantity`) theo giá hiện tại. Ghi chú trên giao diện: "Giá trị theo giá hiện tại, chưa phải đơn hàng".
- Giỏ trống (không có dòng nào) vẫn hiển thị, có nhãn "Giỏ trống".
- Bộ lọc qua query string: tìm theo họ tên / email (`q`), và trạng thái (Tất cả / Có hàng / Trống). Giữ bộ lọc khi chuyển trang.
- Sắp xếp theo `CreatedAt` mới nhất. Phân trang 10 giỏ mỗi trang, xử lý số trang không hợp lệ không gây lỗi.
- Dùng projection (`Select`) sang view model, tính tổng bằng truy vấn trong database, không tải toàn bộ `CartItems` về bộ nhớ rồi mới cộng.
- Không hiển thị `PasswordHash` hay bất kỳ dữ liệu nhạy cảm nào của tài khoản.

## 2. Xem chi tiết (`Details`)
- Thông tin giỏ: mã giỏ, ngày tạo, chủ giỏ (họ tên, email, link sang `Admin/Users/Details/{id}`).
- Bảng các dòng hàng: ảnh nhỏ (`ImageUrl`, nếu chưa có thì ô màu + icon dự phòng), tên sản phẩm, size, số lượng, đơn giá hiện tại, thành tiền, tồn kho hiện tại.
- Mỗi dòng gắn **cảnh báo** khi cần:
  - "Ngừng bán" nếu sản phẩm `IsActive = false`.
  - "Hết hàng" nếu `StockQuantity = 0`.
  - "Vượt tồn kho" nếu `Quantity` lớn hơn `StockQuantity`.
  - "Size không còn" nếu size trong giỏ không còn nằm trong `Products.Sizes`.
- Tổng cộng ở cuối bảng, dùng `Format.Money()`.
- Nút: Xóa toàn bộ giỏ, Quay lại danh sách.

## 3. Xóa
Có hai hành động, đều là **POST**, có `[ValidateAntiForgeryToken]` và bước xác nhận (hộp thoại) nêu rõ chủ giỏ:
- **Xóa một dòng hàng** (`DeleteItem`): nhận `cartId` và `itemId`, chỉ xóa nếu dòng đó thuộc đúng giỏ (không tin `itemId` đứng một mình). Xóa xong quay lại trang chi tiết.
- **Xóa toàn bộ giỏ** (`Delete`): xóa `Carts` kèm các dòng (cascade). Quay lại danh sách và giữ nguyên bộ lọc.
- Nếu bản ghi đã bị xóa từ trước (hai admin cùng thao tác, hoặc bấm trùng), không báo lỗi hệ thống: hiện thông báo "Giỏ hàng không còn tồn tại" bằng `TempData`.
- Mục tiêu là làm sạch giỏ, **không** làm thay đổi tồn kho, vì giỏ hàng chưa trừ kho.

## 4. Kiểm tra tương tác với phía khách hàng
Sau khi admin xóa giỏ của một khách, khách vẫn phải dùng bình thường: vào `Cart/Index` thấy giỏ trống, và `Cart/Add` tạo lại giỏ mới được. Đọc `CartController` để xác nhận. Nếu có lỗi (ví dụ giả định giỏ luôn tồn tại), chỉ sửa **tối thiểu** chỗ đó và ghi rõ trong báo cáo. Nếu không có lỗi thì không đụng vào file.

## 5. Không làm những việc ngoài use case
Use case chỉ gồm xem và xóa. Không thêm tạo giỏ, sửa số lượng, chuyển giỏ thành đơn hàng, hay gửi thông báo cho khách.

## 6. Sidebar và trang tổng quan
- `_AdminLayout.cshtml`: bật mục "Giỏ hàng" trỏ tới `Admin/Carts`. Sau bước này không còn mục nào ở trạng thái "Sắp có", hãy bỏ nhãn đó khỏi code nếu không còn dùng.
- `Dashboard`: thêm thẻ số liệu: tổng số giỏ, số giỏ có hàng, và link sang danh sách.

## Ràng buộc
- Không đổi cấu trúc database, không thêm migrations. Nếu thấy cần (ví dụ thêm cột ngày cập nhật cho giỏ), dừng lại và hỏi.
- Không sửa `AccountController`, `OrdersController`, và các controller admin đã làm, ngoài phần dashboard và sidebar nêu trên.
- Mọi action ghi dữ liệu là POST có `[ValidateAntiForgeryToken]`. Không đặt `@if` dính liền chữ phía trước trong Razor.
- Giữ code style, giao diện admin hiện có, thông báo và nhãn bằng tiếng Việt có dấu.

## Khi xong
1. `dotnet build` sạch, không cảnh báo mới.
2. Cập nhật `AGENTS.md` và `database.md` (bảng trạng thái code), ghi chú giỏ hàng nay đã có trang admin.
3. Báo cáo file đã tạo/sửa, tóm tắt từng file và các điểm chưa chắc chắn.
