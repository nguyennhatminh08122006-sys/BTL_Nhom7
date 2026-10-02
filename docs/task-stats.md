# Nhiệm vụ: trang Thống kê trong khu vực Admin (biểu đồ)

Đọc `AGENTS.md` và `database.md` trước.

Các use case admin đã xong. Thêm mục **Thống kê** vào sidebar, trỏ tới trang có thẻ số liệu và biểu đồ. Làm theo cùng cách các controller admin khác: kế thừa `BaseAdminController`, view model riêng. Không sửa phần cửa hàng, giỏ hàng, đặt hàng.

## Quy ước tính số liệu (bắt buộc làm đúng)
- **Doanh thu = tổng `Orders.Total` của các đơn có `Status = "Hoàn thành"`** (dùng hằng số trong `OrderStatus`, không viết cứng chuỗi). Không tính các trạng thái khác.
- Bảng `Orders` chỉ có `CreatedAt`, **không có ngày hoàn thành**. Mọi biểu đồ theo thời gian dùng `CreatedAt` và ghi chú trên giao diện: "Tính theo ngày đặt hàng".
- **Doanh thu theo sản phẩm / danh mục** = tổng (`OrderItems.UnitPrice` × `Quantity`) của các dòng thuộc đơn `Hoàn thành` (không gồm phí giao hàng và giảm giá). Ghi chú ngắn trên giao diện để không gây nhầm với doanh thu tổng.
- Danh mục của một dòng hàng lấy qua `OrderItems.ProductId` → `Products.CategoryId` → `Categories`.
- Mọi số liệu tính bằng truy vấn tổng hợp trong database (`GroupBy`, `Sum`, `Count`), **không tải toàn bộ đơn hàng về bộ nhớ rồi mới cộng**.

## 1. Bộ lọc thời gian (áp dụng cho cả trang)
- Các nút: 7 ngày, 30 ngày (mặc định), Tháng này, và ô chọn Từ ngày / Đến ngày.
- Truyền qua query string (`from`, `to`) để có thể gửi link. Khoảng thời gian là `CreatedAt >= from` và `CreatedAt < to + 1 ngày` (bao gồm cả ngày "đến").
- Kiểm tra đầu vào: ngày sai định dạng thì quay về mặc định; `from` lớn hơn `to` thì đổi chỗ hoặc báo lỗi; tối đa 366 ngày.
- Hiển thị khoảng đang xem (ví dụ "01/09/2026 – 30/09/2026").

## 2. Thẻ số liệu (hàng trên cùng)
1. **Doanh thu** (đơn hoàn thành trong kỳ).
2. **Đơn hoàn thành** trong kỳ.
3. **Giá trị trung bình mỗi đơn** = doanh thu / số đơn hoàn thành (0 đơn thì hiện "—").
4. **Tỉ lệ hủy** = số đơn `Đã hủy` / tổng số đơn đặt trong kỳ (mọi trạng thái); tổng bằng 0 thì hiện "—".
5. **Đơn đang chờ xử lý** (`Mới` + `Đã xác nhận`), **không phụ thuộc bộ lọc thời gian**, ghi rõ "hiện tại". Bấm vào chuyển sang `Admin/Orders` đã lọc theo trạng thái.

Thẻ 1 đến 3 hiển thị thêm **% thay đổi so với kỳ trước** (kỳ trước có độ dài bằng kỳ đang xem và kết thúc ngay trước ngày `from`). Tăng màu xanh, giảm màu đỏ. Kỳ trước bằng 0 thì hiện "—", tránh chia cho 0.

## 3. Biểu đồ (dùng Chart.js)
Tải Chart.js bằng thẻ `<script>` từ CDN, ghim phiên bản cụ thể: `https://cdnjs.cloudflare.com/ajax/libs/Chart.js/4.4.1/chart.umd.min.js`. Không thêm gói NuGet/npm. Chỉ nạp script này trong trang Thống kê, không nạp ở layout chung.

1. **Doanh thu theo thời gian** (cột hoặc đường): theo ngày; nếu khoảng lớn hơn 92 ngày thì gộp theo tháng. **Ngày không có đơn phải hiện giá trị 0**, không bỏ trống.
2. **Đơn hàng theo trạng thái** (doughnut): đủ 5 trạng thái của đơn đặt trong kỳ, kể cả trạng thái có 0 đơn (nhưng chỉ vẽ lát cắt khi có số liệu, giữ chú thích đủ 5). Màu nhất quán với nhãn trạng thái đã dùng ở trang Đơn hàng.
3. **Top 5 sản phẩm bán chạy** (cột ngang): theo **số lượng bán** (đơn hoàn thành), tooltip hiện thêm doanh thu của sản phẩm đó. Nhóm theo `ProductId`, lấy tên từ `Products`.
4. **Doanh thu theo danh mục** (cột hoặc doughnut): đủ 4 danh mục hiện có, theo quy ước doanh thu theo sản phẩm ở trên.

Yêu cầu chung cho biểu đồ:
- Tooltip và trục tiền tệ định dạng kiểu Việt Nam (`toLocaleString("vi-VN")` kèm "đ").
- Chart.js là script chạy ở trình duyệt. Truyền dữ liệu từ server bằng cách serialize an toàn (ví dụ `System.Text.Json` hoặc `Json.Serialize` đặt vào thuộc tính `data-*` hoặc thẻ `<script type="application/json">`), **không nối chuỗi HTML hoặc chuỗi JS thủ công** (tránh XSS với tên sản phẩm).
- Mỗi biểu đồ nằm trong khung có tiêu đề và `canvas` có `aria-label` mô tả, kèm tóm tắt bằng chữ ngắn bên dưới hoặc bảng số liệu có thể mở rộng.
- Responsive, `maintainAspectRatio` hợp lý, không tràn ngang trên màn hình nhỏ.
- Khoảng thời gian không có dữ liệu thì hiện thông báo "Chưa có dữ liệu trong khoảng thời gian này" thay vì biểu đồ trống.

## 4. Bảng sản phẩm sắp hết hàng
- Sản phẩm `IsActive = true` có `StockQuantity <= 10` (đặt ngưỡng là hằng số/biến cấu hình dễ đổi), sắp xếp tồn kho tăng dần, tối đa 10 dòng.
- Cột: ảnh nhỏ (có nhánh dự phòng ô màu + icon), tên, danh mục, tồn kho (tồn bằng 0 có nhãn "Hết hàng"), link "Sửa" sang `Admin/Products/Edit/{id}`.
- Không phụ thuộc bộ lọc thời gian, ghi rõ "tồn kho hiện tại".

## 5. Sidebar
Trong `_AdminLayout.cshtml`, thêm mục **Thống kê** trỏ tới `Admin/Stats`, đặt ngay sau "Tổng quan". Trang tổng quan (Dashboard) giữ nguyên.

## Ràng buộc
- Không đổi cấu trúc database, không thêm migrations. Nếu thấy cần (ví dụ thêm ngày hoàn thành đơn, ngày tạo tài khoản), dừng lại và hỏi.
- Các biểu đồ khách hàng mới theo thời gian, size bán chạy, top khách hàng **chưa làm ở bước này**.
- Chỉ đọc dữ liệu, không có action ghi nào trong trang này.
- Không đặt `@if` dính liền chữ phía trước trong Razor.
- Giữ code style, giao diện admin hiện có, nhãn bằng tiếng Việt có dấu.

## Khi xong
1. `dotnet build` sạch, không cảnh báo mới.
2. Cập nhật `AGENTS.md`: ghi quy ước doanh thu (đơn `Hoàn thành`, theo ngày đặt) và việc dùng Chart.js qua CDN.
3. Báo cáo file đã tạo/sửa, tóm tắt từng file và các điểm chưa chắc chắn.
