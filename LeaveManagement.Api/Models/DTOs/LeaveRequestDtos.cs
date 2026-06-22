namespace LeaveManagement.Api.Models.DTOs;

public class CreateLeaveRequestDto
{
    public int LeaveTypeId { get; set; }
    public required DateTime StartDate { get; set; }
    public required DateTime EndDate { get; set; }
    public string? Reason { get; set; }
}

public class LeaveRequestDto
{
    public int LeaveRequestId { get; set; }
    public required string EmployeeName { get; set; }
    public required string LeaveTypeName { get; set; }
    public required DateTime StartDate { get; set; }
    public required DateTime EndDate { get; set; }
    public decimal TotalDays { get; set; }
    public string? Reason { get; set; }
    public int Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? ApproverComment { get; set; }
}

public class ApproveLeaveRequestDto
{
    public string? Comment { get; set; }
}

public class RejectLeaveRequestDto
{
    public required string Comment { get; set; }
}
