/* =====================================================================
   CanifaShop – SCRIPT DỰNG DATABASE ĐẦY ĐỦ (chạy MỘT LẦN trước khi dotnet run)

   HƯỚNG DẪN CHO THÀNH VIÊN NHÓM
   1. Cài .NET 8 SDK và SQL Server LocalDB (đi kèm Visual Studio) + SSMS hoặc Azure Data Studio.
   2. Mở file này trong SSMS, kết nối tới  (localdb)\MSSQLLocalDB  rồi bấm Execute.
   3. Mở terminal trong thư mục project, chạy  dotnet run  và vào http://localhost:5000

   Script tạo: database CanifaShopDb, 7 bảng, danh mục, 16 sản phẩm có ảnh,
   tài khoản mẫu và 1 đơn hàng mẫu.
     - Khách hàng : demo@canifa.vn  / Demo@123
     - Quản trị   : admin@canifa.vn / Admin@123

   Chạy lại nhiều lần được, không tạo trùng, không xóa dữ liệu đang có.
   Dành cho máy CHƯA có database. Nếu máy đã có database cấu trúc CŨ (thiếu cột
   StockQuantity, Role, bảng Carts...), hãy chạy CanifaShop_upgrade_admin.sql
   hoặc xóa database cũ rồi chạy lại file này:
       -- USE master; ALTER DATABASE CanifaShopDb SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
       -- DROP DATABASE CanifaShopDb;
   ===================================================================== */

IF DB_ID(N'CanifaShopDb') IS NULL CREATE DATABASE CanifaShopDb;
GO
USE CanifaShopDb;
GO

/* ---------- 1. BẢNG ---------- */
IF OBJECT_ID(N'dbo.Categories') IS NULL
CREATE TABLE dbo.Categories (
    Id    INT IDENTITY(1,1) CONSTRAINT PK_Categories PRIMARY KEY,
    Name  NVARCHAR(100) NOT NULL,
    Slug  NVARCHAR(100) NOT NULL,
    Icon  NVARCHAR(10)  NOT NULL
);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Categories_Slug')
    CREATE UNIQUE INDEX IX_Categories_Slug ON dbo.Categories(Slug);

IF OBJECT_ID(N'dbo.Products') IS NULL
CREATE TABLE dbo.Products (
    Id            INT IDENTITY(1,1) CONSTRAINT PK_Products PRIMARY KEY,
    Name          NVARCHAR(200)  NOT NULL,
    Description   NVARCHAR(2000) NOT NULL,
    Price         DECIMAL(18,0)  NOT NULL,
    OldPrice      DECIMAL(18,0)  NULL,
    Sizes         NVARCHAR(100)  NOT NULL,
    Color         NVARCHAR(20)   NOT NULL,
    IsFeatured    BIT            NOT NULL CONSTRAINT DF_Products_IsFeatured DEFAULT 0,
    CategoryId    INT            NOT NULL,
    ImageUrl      NVARCHAR(300)  NULL,
    StockQuantity INT            NOT NULL CONSTRAINT DF_Products_StockQuantity DEFAULT 50,
    IsActive      BIT            NOT NULL CONSTRAINT DF_Products_IsActive DEFAULT 1,
    CONSTRAINT FK_Products_Categories FOREIGN KEY (CategoryId) REFERENCES dbo.Categories(Id) ON DELETE NO ACTION
);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Products_CategoryId')
    CREATE INDEX IX_Products_CategoryId ON dbo.Products(CategoryId);

IF OBJECT_ID(N'dbo.Users') IS NULL
CREATE TABLE dbo.Users (
    Id           INT IDENTITY(1,1) CONSTRAINT PK_Users PRIMARY KEY,
    FullName     NVARCHAR(100) NOT NULL,
    Email        NVARCHAR(200) NOT NULL,
    Phone        NVARCHAR(20)  NULL,
    PasswordHash NVARCHAR(MAX) NOT NULL,
    [Role]       NVARCHAR(20)  NOT NULL CONSTRAINT DF_Users_Role DEFAULT N'Customer',
    IsLocked     BIT           NOT NULL CONSTRAINT DF_Users_IsLocked DEFAULT 0
);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Users_Email')
    CREATE UNIQUE INDEX IX_Users_Email ON dbo.Users(Email);

IF OBJECT_ID(N'dbo.Orders') IS NULL
CREATE TABLE dbo.Orders (
    Id             INT IDENTITY(1,1) CONSTRAINT PK_Orders PRIMARY KEY,
    UserId         INT           NOT NULL,
    CreatedAt      DATETIME2     NOT NULL CONSTRAINT DF_Orders_CreatedAt DEFAULT SYSDATETIME(),
    ReceiverName   NVARCHAR(100) NOT NULL,
    Phone          NVARCHAR(20)  NOT NULL,
    Address        NVARCHAR(300) NOT NULL,
    Note           NVARCHAR(500) NULL,
    Total          DECIMAL(18,0) NOT NULL,
    Status         NVARCHAR(30)  NOT NULL CONSTRAINT DF_Orders_Status DEFAULT N'Mới',
    PaymentMethod  NVARCHAR(30)  NOT NULL CONSTRAINT DF_Orders_PaymentMethod DEFAULT N'COD',
    ShippingFee    DECIMAL(18,0) NOT NULL CONSTRAINT DF_Orders_ShippingFee DEFAULT 0,
    DiscountCode   NVARCHAR(50)  NULL,
    DiscountAmount DECIMAL(18,0) NOT NULL CONSTRAINT DF_Orders_DiscountAmount DEFAULT 0,
    CONSTRAINT FK_Orders_Users FOREIGN KEY (UserId) REFERENCES dbo.Users(Id) ON DELETE NO ACTION
);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Orders_UserId')
    CREATE INDEX IX_Orders_UserId ON dbo.Orders(UserId);

IF OBJECT_ID(N'dbo.OrderItems') IS NULL
CREATE TABLE dbo.OrderItems (
    Id          INT IDENTITY(1,1) CONSTRAINT PK_OrderItems PRIMARY KEY,
    OrderId     INT           NOT NULL,
    ProductId   INT           NOT NULL,
    ProductName NVARCHAR(200) NOT NULL,
    Size        NVARCHAR(20)  NOT NULL,
    Quantity    INT           NOT NULL,
    UnitPrice   DECIMAL(18,0) NOT NULL,
    CONSTRAINT FK_OrderItems_Orders   FOREIGN KEY (OrderId)   REFERENCES dbo.Orders(Id)   ON DELETE CASCADE,
    CONSTRAINT FK_OrderItems_Products FOREIGN KEY (ProductId) REFERENCES dbo.Products(Id) ON DELETE NO ACTION
);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_OrderItems_OrderId')
    CREATE INDEX IX_OrderItems_OrderId ON dbo.OrderItems(OrderId);

IF OBJECT_ID(N'dbo.Carts') IS NULL
CREATE TABLE dbo.Carts (
    Id        INT IDENTITY(1,1) CONSTRAINT PK_Carts PRIMARY KEY,
    UserId    INT       NOT NULL,
    CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_Carts_CreatedAt DEFAULT SYSDATETIME(),
    CONSTRAINT FK_Carts_Users FOREIGN KEY (UserId) REFERENCES dbo.Users(Id) ON DELETE CASCADE
);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Carts_UserId')
    CREATE UNIQUE INDEX IX_Carts_UserId ON dbo.Carts(UserId);

IF OBJECT_ID(N'dbo.CartItems') IS NULL
CREATE TABLE dbo.CartItems (
    Id        INT IDENTITY(1,1) CONSTRAINT PK_CartItems PRIMARY KEY,
    CartId    INT          NOT NULL,
    ProductId INT          NOT NULL,
    Size      NVARCHAR(20) NOT NULL,
    Quantity  INT          NOT NULL CONSTRAINT CK_CartItems_Quantity CHECK (Quantity > 0),
    CONSTRAINT FK_CartItems_Carts    FOREIGN KEY (CartId)    REFERENCES dbo.Carts(Id)    ON DELETE CASCADE,
    CONSTRAINT FK_CartItems_Products FOREIGN KEY (ProductId) REFERENCES dbo.Products(Id) ON DELETE CASCADE
);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_CartItems_Cart_Product_Size')
    CREATE UNIQUE INDEX IX_CartItems_Cart_Product_Size ON dbo.CartItems(CartId, ProductId, Size);
GO

/* ---------- 2. DANH MỤC VÀ SẢN PHẨM (chỉ thêm khi chưa có danh mục) ---------- */
IF NOT EXISTS (SELECT 1 FROM dbo.Categories)
BEGIN
    INSERT dbo.Categories (Name, Slug, Icon) VALUES
        (N'Nữ',       N'nu',       N'👚'),
        (N'Nam',      N'nam',      N'👔'),
        (N'Trẻ em',   N'tre-em',   N'🧒'),
        (N'Phụ kiện', N'phu-kien', N'🧣');

    DECLARE @nu  INT = (SELECT Id FROM dbo.Categories WHERE Slug = N'nu');
    DECLARE @nam INT = (SELECT Id FROM dbo.Categories WHERE Slug = N'nam');
    DECLARE @tre INT = (SELECT Id FROM dbo.Categories WHERE Slug = N'tre-em');
    DECLARE @pk  INT = (SELECT Id FROM dbo.Categories WHERE Slug = N'phu-kien');

    -- Thứ tự chèn quyết định Id 1..16 (khớp với tên file ảnh trong wwwroot/image)
    INSERT dbo.Products (Name, Description, Price, OldPrice, Sizes, Color, IsFeatured, CategoryId, ImageUrl, StockQuantity, IsActive) VALUES
    (N'Áo thun nữ cotton cổ tròn', N'Áo thun cotton mềm mịn, thoáng mát, form regular dễ phối đồ hằng ngày.', 199000, 249000, N'S,M,L,XL', N'#F6C1CC', 1, @nu,  N'/image/Aothunnucotron_image.jpg',     50, 1),
    (N'Áo sơ mi nữ tay dài',       N'Sơ mi chất liệu nhẹ, ít nhăn, phù hợp đi làm và đi học.',                349000, NULL,   N'S,M,L,XL', N'#CFE3F5', 1, @nu,  N'/image/Aosominutaydai_image.jpg',     50, 1),
    (N'Chân váy chữ A',            N'Chân váy dáng chữ A lưng cao, tôn dáng, chất vải đứng form.',            299000, 379000, N'S,M,L,XL', N'#D9C7F0', 1, @nu,  N'/image/ChanvaychuA-image.jpg',        50, 1),
    (N'Quần jeans nữ ống đứng',    N'Jeans co giãn nhẹ, ống đứng, tông xanh basic.',                          449000, NULL,   N'S,M,L,XL', N'#9DB8D9', 0, @nu,  N'/image/Quanjeansnu_image.jpg',        50, 1),
    (N'Áo khoác nữ nhẹ',           N'Áo khoác mỏng nhẹ, chống gió nhẹ, gấp gọn dễ mang theo.',                499000, 599000, N'S,M,L,XL', N'#F8D9A8', 0, @nu,  N'/image/Aokhoacnunhe_image.jpg',       50, 1),
    (N'Áo polo nam pique',         N'Polo vải pique thoáng khí, cổ bo bền form.',                             279000, 329000, N'S,M,L,XL', N'#B7D9C4', 1, @nam, N'/image/Aopolonam_image.jpg',          50, 1),
    (N'Áo sơ mi nam oxford',       N'Sơ mi oxford dày dặn, dễ ủi, lịch sự mà vẫn thoải mái.',                 399000, NULL,   N'S,M,L,XL', N'#C9D6EA', 1, @nam, N'/image/Aosomioxford_image.jpg',       50, 1),
    (N'Quần kaki nam slim fit',    N'Quần kaki co giãn, slim fit gọn gàng, nhiều dịp mặc.',                   429000, 499000, N'S,M,L,XL', N'#D8CBB0', 0, @nam, N'/image/Quankakinam_image.jpg',        50, 1),
    (N'Áo hoodie nam nỉ',          N'Hoodie nỉ bông ấm, có mũ và túi kangaroo.',                              459000, NULL,   N'S,M,L,XL', N'#A9B4C8', 1, @nam, N'/image/Aohoodienamni_image.jpg',      50, 1),
    (N'Áo thun nam basic',         N'Áo thun cotton 100%, cổ tròn, màu trơn dễ phối.',                        179000, 219000, N'S,M,L,XL', N'#EFD9A0', 0, @nam, N'/image/Aothunnambasic_image.jpg',     50, 1),
    (N'Bộ đồ bé trai cotton',      N'Bộ quần áo cotton mềm, an toàn cho da bé.',                              259000, NULL,   N'3-4,5-6,7-8,9-10', N'#BFE3F0', 1, @tre, N'/image/Bodobetraicotton_image.jpg',   50, 1),
    (N'Váy bé gái họa tiết hoa',   N'Váy cotton họa tiết hoa nhí, bé thoải mái vận động.',                    289000, 339000, N'3-4,5-6,7-8,9-10', N'#F9C7D8', 0, @tre, N'/image/Vaybegaihoatiethoa_image.jpg', 50, 1),
    (N'Áo thun bé trai in hình',   N'Áo thun in hình ngộ nghĩnh, vải thấm hút tốt.',                          149000, NULL,   N'3-4,5-6,7-8,9-10', N'#C6E8C0', 0, @tre, N'/image/Aothunbetraiinhinh_image.jpg', 50, 1),
    (N'Khăn quàng len',            N'Khăn len mềm, giữ ấm tốt cho mùa lạnh.',                                 189000, NULL,   N'Free size', N'#F2B8A8', 0, @pk,  N'/image/Khanquanglen_image.jpg',       50, 1),
    (N'Mũ lưỡi trai',              N'Mũ lưỡi trai cotton, khóa điều chỉnh phía sau.',                         129000, 159000, N'Free size', N'#C8C2E6', 1, @pk,  N'/image/Muluoitrai_image.jpg',         50, 1),
    (N'Tất cotton (set 3 đôi)',    N'Set 3 đôi tất cotton co giãn, thoáng chân.',                              99000, NULL,   N'Free size', N'#E6DCC3', 0, @pk,  N'/image/Tatcotton_image.jpg',          50, 1);
END

/* ---------- 3. TÀI KHOẢN MẪU ---------- */
IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE Email = N'demo@canifa.vn')
    INSERT dbo.Users (FullName, Email, Phone, PasswordHash, [Role], IsLocked)
    VALUES (N'Khách Demo', N'demo@canifa.vn', N'0900000000',
            N'AQAAAAEAACcQAAAAEKoZoXwI7hElFeoiv72ErIfGfb/pWcZTzpuzR4e0TF9a/DCdkF/b5jI92ZFIf2BtyQ==', N'Customer', 0);

IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE Email = N'admin@canifa.vn')
    INSERT dbo.Users (FullName, Email, Phone, PasswordHash, [Role], IsLocked)
    VALUES (N'Quản trị viên', N'admin@canifa.vn', N'0900000001',
            N'AQAAAAEAACcQAAAAEMeBgK57VwWVjmuB38DOw7R3TXBzgGWw1ERTMZnEhQKR6fj++ENh7QnNy+jT7plzFQ==', N'Admin', 0);

/* ---------- 4. ĐƠN HÀNG MẪU (chỉ thêm khi chưa có đơn nào) ---------- */
IF NOT EXISTS (SELECT 1 FROM dbo.Orders)
BEGIN
    DECLARE @uid INT = (SELECT Id FROM dbo.Users WHERE Email = N'demo@canifa.vn');
    DECLARE @p1  INT = (SELECT TOP 1 Id FROM dbo.Products WHERE Name = N'Áo polo nam pique');
    DECLARE @p2  INT = (SELECT TOP 1 Id FROM dbo.Products WHERE Name = N'Mũ lưỡi trai');

    IF @uid IS NOT NULL AND @p1 IS NOT NULL AND @p2 IS NOT NULL
    BEGIN
        INSERT dbo.Orders (UserId, ReceiverName, Phone, Address, Note, Total, Status)
        VALUES (@uid, N'Khách Demo', N'0900000000', N'123 Cầu Giấy, Hà Nội', N'Giao giờ hành chính', 279000 * 2 + 129000, N'Mới');
        DECLARE @oid INT = SCOPE_IDENTITY();

        INSERT dbo.OrderItems (OrderId, ProductId, ProductName, Size, Quantity, UnitPrice) VALUES
            (@oid, @p1, N'Áo polo nam pique', N'M',         2, 279000),
            (@oid, @p2, N'Mũ lưỡi trai',      N'Free size', 1, 129000);
    END
END
GO

/* ---------- 5. KIỂM TRA ---------- */
SELECT N'Categories' AS Bang, COUNT(*) AS SoDong FROM dbo.Categories
UNION ALL SELECT N'Products',   COUNT(*) FROM dbo.Products
UNION ALL SELECT N'Users',      COUNT(*) FROM dbo.Users
UNION ALL SELECT N'Orders',     COUNT(*) FROM dbo.Orders
UNION ALL SELECT N'OrderItems', COUNT(*) FROM dbo.OrderItems
UNION ALL SELECT N'Carts',      COUNT(*) FROM dbo.Carts
UNION ALL SELECT N'CartItems',  COUNT(*) FROM dbo.CartItems;

SELECT Id, Name, ImageUrl, StockQuantity, IsActive FROM dbo.Products ORDER BY Id;
SELECT Id, Email, [Role], IsLocked FROM dbo.Users;
