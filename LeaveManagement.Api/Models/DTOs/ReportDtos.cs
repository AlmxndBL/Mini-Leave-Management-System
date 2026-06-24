namespace LeaveManagement.Api.Models.DTOs;

public class ReportQuery
{
    public int? Year { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int? DepartmentId { get; set; }
    public int? LeaveTypeId { get; set; }
}

public class LeaveSummaryRowDto
{
    public string DepartmentName { get; set; } = string.Empty;
    public string LeaveTypeName { get; set; } = string.Empty;
    public string LeaveTypeColor { get; set; } = "#6B7280";
    public int RequestCount { get; set; }
    public int EmployeeCount { get; set; }
    public decimal TotalDays { get; set; }
}
