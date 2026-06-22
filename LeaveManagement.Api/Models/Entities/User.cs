namespace LeaveManagement.Api.Models.Entities;

public class User
{
    public int UserId { get; set; }
    public required string Email { get; set; }
    public required string PasswordHash { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public int Role { get; set; }
    public int? DepartmentId { get; set; }
    public required DateTime HireDate { get; set; }
    public bool IsActive { get; set; } = true;

    public Department? Department { get; set; }
    public ICollection<LeaveRequest> LeaveRequests { get; set; } = [];
    public ICollection<LeaveRequest> ApprovedLeaveRequests { get; set; } = [];
    public ICollection<LeaveBalance> LeaveBalances { get; set; } = [];
    public ICollection<Department> ManagedDepartments { get; set; } = [];
}
