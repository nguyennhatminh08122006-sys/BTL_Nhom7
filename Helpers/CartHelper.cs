using System.Globalization;

namespace CanifaShop.Helpers;

public static class Format
{
    private static readonly CultureInfo Vi = new("vi-VN");
    public static string Money(this decimal d) => d.ToString("N0", Vi) + "đ";
}
