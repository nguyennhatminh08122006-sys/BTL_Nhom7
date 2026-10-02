using CanifaShop.Models;
using Microsoft.AspNetCore.Identity;

namespace CanifaShop.Data;

public static class SeedData
{
    public static void Init(AppDbContext db)
    {
        if (db.Categories.Any()) return;

        var nu = new Category { Name = "Nữ", Slug = "nu", Icon = "👚" };
        var nam = new Category { Name = "Nam", Slug = "nam", Icon = "👔" };
        var tre = new Category { Name = "Trẻ em", Slug = "tre-em", Icon = "🧒" };
        var pk = new Category { Name = "Phụ kiện", Slug = "phu-kien", Icon = "🧣" };

        const string adult = "S,M,L,XL";
        const string kids = "3-4,5-6,7-8,9-10";
        const string one = "Free size";

        db.Products.AddRange(
            P("Áo thun nữ cotton cổ tròn", "Áo thun cotton mềm mịn, thoáng mát, form regular dễ phối đồ hằng ngày.", 199000, 249000, adult, "#F6C1CC", nu, true),
            P("Áo sơ mi nữ tay dài", "Sơ mi chất liệu nhẹ, ít nhăn, phù hợp đi làm và đi học.", 349000, null, adult, "#CFE3F5", nu, true),
            P("Chân váy chữ A", "Chân váy dáng chữ A lưng cao, tôn dáng, chất vải đứng form.", 299000, 379000, adult, "#D9C7F0", nu, true),
            P("Quần jeans nữ ống đứng", "Jeans co giãn nhẹ, ống đứng, tông xanh basic.", 449000, null, adult, "#9DB8D9", nu),
            P("Áo khoác nữ nhẹ", "Áo khoác mỏng nhẹ, chống gió nhẹ, gấp gọn dễ mang theo.", 499000, 599000, adult, "#F8D9A8", nu),
            P("Áo polo nam pique", "Polo vải pique thoáng khí, cổ bo bền form.", 279000, 329000, adult, "#B7D9C4", nam, true),
            P("Áo sơ mi nam oxford", "Sơ mi oxford dày dặn, dễ ủi, lịch sự mà vẫn thoải mái.", 399000, null, adult, "#C9D6EA", nam, true),
            P("Quần kaki nam slim fit", "Quần kaki co giãn, slim fit gọn gàng, nhiều dịp mặc.", 429000, 499000, adult, "#D8CBB0", nam),
            P("Áo hoodie nam nỉ", "Hoodie nỉ bông ấm, có mũ và túi kangaroo.", 459000, null, adult, "#A9B4C8", nam, true),
            P("Áo thun nam basic", "Áo thun cotton 100%, cổ tròn, màu trơn dễ phối.", 179000, 219000, adult, "#EFD9A0", nam),
            P("Bộ đồ bé trai cotton", "Bộ quần áo cotton mềm, an toàn cho da bé.", 259000, null, kids, "#BFE3F0", tre, true),
            P("Váy bé gái họa tiết hoa", "Váy cotton họa tiết hoa nhí, bé thoải mái vận động.", 289000, 339000, kids, "#F9C7D8", tre),
            P("Áo thun bé trai in hình", "Áo thun in hình ngộ nghĩnh, vải thấm hút tốt.", 149000, null, kids, "#C6E8C0", tre),
            P("Khăn quàng len", "Khăn len mềm, giữ ấm tốt cho mùa lạnh.", 189000, null, one, "#F2B8A8", pk),
            P("Mũ lưỡi trai", "Mũ lưỡi trai cotton, khóa điều chỉnh phía sau.", 129000, 159000, one, "#C8C2E6", pk, true),
            P("Tất cotton (set 3 đôi)", "Set 3 đôi tất cotton co giãn, thoáng chân.", 99000, null, one, "#E6DCC3", pk)
        );

        var demo = new User { FullName = "Khách Demo", Email = "demo@canifa.vn", Phone = "0900000000" };
        demo.PasswordHash = new PasswordHasher<User>().HashPassword(demo, "Demo@123");
        db.Users.Add(demo);

        db.SaveChanges();
    }

    private static Product P(string name, string desc, decimal price, decimal? old, string sizes, string color, Category c, bool featured = false)
        => new() { Name = name, Description = desc, Price = price, OldPrice = old, Sizes = sizes, Color = color, Category = c, IsFeatured = featured };
}
