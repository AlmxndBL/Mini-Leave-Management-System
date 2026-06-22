namespace LeaveManagement.Api.Models.Entities;

public class Department
{
    public int DepartmentId { get; set; }
    public required string Name { get; set; }
    public int? ManagerId { get; set; }

    public User? Manager { get; set; }
    public ICollection<User> Users { get; set; } = [];
}
