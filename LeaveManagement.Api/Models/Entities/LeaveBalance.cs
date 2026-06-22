namespace LeaveManagement.Api.Models.Entities;

public class LeaveBalance
{
    public int LeaveBalanceId { get; set; }
    public int UserId { get; set; }
    public int LeaveTypeId { get; set; }
    public int Year { get; set; }
    public decimal TotalDays { get; set; }
    public decimal UsedDays { get; set; }

    public User User { get; set; } = null!;
    public LeaveType LeaveType { get; set; } = null!;
}
