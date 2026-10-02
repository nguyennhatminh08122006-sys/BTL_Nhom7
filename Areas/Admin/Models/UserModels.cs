namespace CanifaShop.Areas.Admin.Models;

public class UserIndexVm
{
    public List<UserRowVm> Users { get; set; } = new();
    public string? Query { get; set; }
    public string? Role { get; set; }
    public string? Status { get; set; }
    public int Page { get; set; }
    public int TotalPages { get; set; }
    public int TotalCount { get; set; }
    public string ReturnUrl { get; set; } = "";
}

public class UserRowVm
{
    public int Id { get; set; }
    public string FullName { get; set; } = "";
    public string Email { get; set; } = "";
    public string? Phone { get; set; }
    public string Role { get; set; } = "";
    public bool IsLocked { get; set; }
    public int OrderCount { get; set; }
    public bool IsCurrentUser { get; set; }
    public bool DisableLock { get; set; }
    public string? LockDisabledReason { get; set; }
    public bool DisableDelete { get; set; }
    public string? DeleteDisabledReason { get; set; }
}

public class UserDetailsVm
{
    public int Id { get; set; }
    public string FullName { get; set; } = "";
    public string Email { get; set; } = "";
    public string? Phone { get; set; }
    public string Role { get; set; } = "";
    public bool IsLocked { get; set; }
    public int OrderCount { get; set; }
    public decimal NonCancelledOrderTotal { get; set; }
    public int CartProductTypeCount { get; set; }
    public List<UserOrderStatusCountVm> OrderStatusCounts { get; set; } = new();
    public List<UserRecentOrderVm> RecentOrders { get; set; } = new();
    public bool IsCurrentUser { get; set; }
    public bool DisableLock { get; set; }
    public string? LockDisabledReason { get; set; }
    public bool DisableDelete { get; set; }
    public string? DeleteDisabledReason { get; set; }
    public string ReturnUrl { get; set; } = "";
    public string DeleteReturnUrl { get; set; } = "";
}

public class UserOrderStatusCountVm
{
    public string Status { get; set; } = "";
    public int Count { get; set; }
}

public class UserRecentOrderVm
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public decimal Total { get; set; }
    public string Status { get; set; } = "";
}
