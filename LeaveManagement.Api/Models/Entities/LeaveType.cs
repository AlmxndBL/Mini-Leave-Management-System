namespace LeaveManagement.Api.Models.Entities;

public class LeaveType
{
    public int LeaveTypeId { get; set; }
    public required string Name { get; set; }
    public decimal DefaultDaysPerYear { get; set; }
    public string? ColorCode { get; set; }

    public ICollection<LeaveRequest> LeaveRequests { get; set; } = [];
    public ICollection<LeaveBalance> LeaveBalances { get; set; } = [];
}
