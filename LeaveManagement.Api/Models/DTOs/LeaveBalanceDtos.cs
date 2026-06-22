namespace LeaveManagement.Api.Models.DTOs;

public class LeaveBalanceDto
{
    public int LeaveBalanceId { get; set; }
    public required string LeaveTypeName { get; set; }
    public string? ColorCode { get; set; }
    public decimal TotalDays { get; set; }
    public decimal UsedDays { get; set; }
    public decimal RemainingDays => TotalDays - UsedDays;
    public int Year { get; set; }
}
