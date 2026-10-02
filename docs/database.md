/* DATABASE: CanifaShopDb (SQL Server). App: ASP.NET Core MVC .NET 10 + EF Core.
   Website bán thời trang. Giỏ hàng được lưu trong database theo từng User. */

/* TRẠNG THÁI CODE
   | Bảng     | Trạng thái |
   | Carts    | Đã có      |
   | CartItems| Đã có      |
   Schema Carts/CartItems đã tồn tại; ứng dụng dùng các bảng này, không dùng session.
   Trang quản lý giỏ hàng Admin đã có trong code. */

Categories(            -- danh mục: Nữ, Nam, Trẻ em, Phụ kiện
  Id INT PK IDENTITY,
  Name NVARCHAR(100), Slug NVARCHAR(100) UNIQUE,  -- slug dùng trên URL: nu, nam, tre-em, phu-kien
  Icon NVARCHAR(10)                               -- emoji dự phòng khi chưa có ảnh
)

Products(              -- sản phẩm, 16 dòng mẫu
  Id INT PK IDENTITY,
  Name NVARCHAR(200), Description NVARCHAR(2000),
  Price DECIMAL(18,0),                            -- VND
  OldPrice DECIMAL(18,0) NULL,                    -- giá gốc, NULL = không giảm giá
  Sizes NVARCHAR(100),                            -- danh sách cách nhau dấu phẩy: 'S,M,L,XL' | '3-4,5-6,7-8,9-10' | 'Free size'
  Color NVARCHAR(20),                             -- mã hex, màu nền dự phòng
  IsFeatured BIT,                                 -- 1 = hiện ở mục nổi bật trang chủ
  ImageUrl NVARCHAR(300) NULL,                    -- vd '/image/Aopolonam_image.jpg' (file trong wwwroot/image)
  StockQuantity INT,                              -- tồn kho
  IsActive BIT DEFAULT 1,                         -- 0 = ẩn khỏi cửa hàng
  CategoryId INT FK -> Categories.Id
)

Users(                 -- tài khoản khách hàng và quản trị viên
  Id INT PK IDENTITY,
  FullName NVARCHAR(100), Email NVARCHAR(200) UNIQUE, Phone NVARCHAR(20) NULL,
  PasswordHash NVARCHAR(MAX),                     -- hash ASP.NET Identity PasswordHasher, KHÔNG phải mật khẩu thô
  Role NVARCHAR(20) DEFAULT N'Customer',          -- 'Customer' hoặc 'Admin'
  IsLocked BIT                                    -- 1 = khóa đăng nhập
)

Orders(                -- đơn hàng, thanh toán COD
  Id INT PK IDENTITY,
  UserId INT FK -> Users.Id,
  CreatedAt DATETIME2,
  ReceiverName NVARCHAR(100), Phone NVARCHAR(20), Address NVARCHAR(300), Note NVARCHAR(500) NULL,
  Total DECIMAL(18,0),                            -- tổng đơn
  Status NVARCHAR(30),                            -- mặc định N'Mới'
  PaymentMethod NVARCHAR(30),                     -- mặc định 'COD'
  ShippingFee DECIMAL(18,0),
  DiscountAmount DECIMAL(18,0),
  DiscountCode NVARCHAR(50) NULL
)

OrderItems(            -- dòng chi tiết đơn, copy tên và giá tại thời điểm mua
  Id INT PK IDENTITY,
  OrderId INT FK -> Orders.Id,
  ProductId INT,
  ProductName NVARCHAR(200), Size NVARCHAR(20),
  Quantity INT, UnitPrice DECIMAL(18,0),
  FK ProductId -> Products.Id (ON DELETE NO ACTION / RESTRICT)
)

Carts(                 -- một giỏ database cho mỗi user
  Id INT PK IDENTITY,
  UserId INT FK -> Users.Id UNIQUE,
  CreatedAt DATETIME2
)

CartItems(             -- các dòng giỏ database
  Id INT PK IDENTITY,
  CartId INT FK -> Carts.Id,
  ProductId INT FK -> Products.Id,
  Size NVARCHAR(20), Quantity INT,
  UNIQUE(CartId, ProductId, Size)
)

/* QUAN HỆ: Categories 1-n Products | Users 1-n Orders | Orders 1-n OrderItems
   | Users 1-1 Carts | Carts 1-n CartItems.
   Product->Category, Order->User và OrderItem->Product dùng NO ACTION / RESTRICT.
   Carts được xóa cascade cùng User; CartItems được xóa cascade cùng Cart.
   Giỏ website được lưu trong database; không thay đổi cấu trúc bảng khi chuyển code.
   Tài khoản demo: demo@canifa.vn / Demo@123
   Database đã nâng cấp bằng CanifaShop_upgrade_admin.sql; không chạy lại script,
   không dùng migrations hoặc EnsureCreated để tạo/cập nhật schema. */
