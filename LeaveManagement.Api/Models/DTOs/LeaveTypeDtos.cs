namespace LeaveManagement.Api.Models.DTOs;

public class CreateLeaveTypeDto
{
    public required string Name { get; set; }
    public decimal DefaultDaysPerYear { get; set; }
    public string? ColorCode { get; set; }
}

public class UpdateLeaveTypeDto
{
    public required string Name { get; set; }
    public decimal DefaultDaysPerYear { get; set; }
    public string? ColorCode { get; set; }
}

public class LeaveTypeDto
{
    public int LeaveTypeId { get; set; }
    public required string Name { get; set; }
    public decimal DefaultDaysPerYear { get; set; }
    public string? ColorCode { get; set; }
}
