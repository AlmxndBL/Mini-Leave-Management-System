namespace LeaveManagement.Api.Helpers;

public static class UserRoles
{
    public const int Employee = 0;
    public const int Manager = 1;
    public const int Admin = 2;
}

public static class LeaveRequestStatus
{
    public const int Pending = 0;
    public const int Approved = 1;
    public const int Rejected = 2;
    public const int Cancelled = 3;
}
