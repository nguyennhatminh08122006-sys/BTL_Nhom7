namespace CanifaShop.Models;

public static class OrderStatus
{
    public const string New = "Mới";
    public const string Confirmed = "Đã xác nhận";
    public const string Shipping = "Đang giao";
    public const string Completed = "Hoàn thành";
    public const string Cancelled = "Đã hủy";

    public static IReadOnlyList<string> All { get; } =
    [New, Confirmed, Shipping, Completed, Cancelled];

    private static readonly IReadOnlyDictionary<string, IReadOnlyList<string>> Transitions =
        new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal)
        {
            [New] = [Confirmed, Cancelled],
            [Confirmed] = [Shipping, Cancelled],
            [Shipping] = [Completed, Cancelled],
            [Completed] = [],
            [Cancelled] = []
        };

    public static bool CanTransition(string? from, string? to)
        => from != null && to != null && Transitions.TryGetValue(from, out var next) && next.Contains(to);

    public static IReadOnlyList<string> GetNextStatuses(string? status)
        => status != null && Transitions.TryGetValue(status, out var next) ? next : [];

    public static bool IsTerminal(string? status)
        => status is Completed or Cancelled;

    public static string GetBadgeClass(string? status)
        => status switch
        {
            New => "order-status-new",
            Confirmed => "order-status-confirmed",
            Shipping => "order-status-shipping",
            Completed => "order-status-completed",
            Cancelled => "order-status-cancelled",
            _ => "order-status-unknown"
        };

    public static string GetActionLabel(string nextStatus)
        => nextStatus switch
        {
            Confirmed => "Xác nhận đơn",
            Shipping => "Bắt đầu giao",
            Completed => "Hoàn thành",
            Cancelled => "Hủy đơn",
            _ => nextStatus
        };
}
