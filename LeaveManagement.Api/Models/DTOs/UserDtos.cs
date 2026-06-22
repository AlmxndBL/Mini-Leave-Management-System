namespace LeaveManagement.Api.Models.DTOs;

public class CreateUserDto
{
    public required string Email { get; set; }
    public required string Password { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public int Role { get; set; }
    public int? DepartmentId { get; set; }
    public required DateTime HireDate { get; set; }
}

public class UpdateUserDto
{
    public int Role { get; set; }
    public int? DepartmentId { get; set; }
    public bool IsActive { get; set; }
}

public class UserDto
{
    public int UserId { get; set; }
    public required string Email { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public int Role { get; set; }
    public int? DepartmentId { get; set; }
    public DateTime HireDate { get; set; }
    public bool IsActive { get; set; }
}

public class DashboardSummaryDto
{
    public int PendingCount { get; set; }
    public int ApprovedCount { get; set; }
    public int RejectedCount { get; set; }
}
