namespace LeaveManagement.Api.Models.DTOs;

public class LoginRequest
{
    public required string Email { get; set; }
    public required string Password { get; set; }
}

public class LoginResponse
{
    public int UserId { get; set; }
    public required string Email { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public int Role { get; set; }
    public int? DepartmentId { get; set; }
    public required string AccessToken { get; set; }
}
