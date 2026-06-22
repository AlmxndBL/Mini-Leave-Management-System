namespace LeaveManagement.Api.Models.Entities;

public class LeaveRequest
{
    public int LeaveRequestId { get; set; }
    public int UserId { get; set; }
    public int LeaveTypeId { get; set; }
    public required DateTime StartDate { get; set; }
    public required DateTime EndDate { get; set; }
    public decimal TotalDays { get; set; }
    public string? Reason { get; set; }
    public int Status { get; set; }
    public int? ApproverId { get; set; }
    public string? ApproverComment { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public User User { get; set; } = null!;
    public LeaveType LeaveType { get; set; } = null!;
    public User? Approver { get; set; }
}
