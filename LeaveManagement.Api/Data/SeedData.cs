using LeaveManagement.Api.Helpers;
using LeaveManagement.Api.Models.Entities;

namespace LeaveManagement.Api.Data;

public static class SeedData
{
    public static void Initialize(AppDbContext context)
    {
        if (context.LeaveTypes.Any()) return;

        var leaveTypes = new LeaveType[]
        {
            new LeaveType { Name = "ลาป่วย", DefaultDaysPerYear = 30, ColorCode = "#FF5A5A" },
            new LeaveType { Name = "ลากิจ", DefaultDaysPerYear = 10, ColorCode = "#FFA500" },
            new LeaveType { Name = "ลาพักร้อน", DefaultDaysPerYear = 10, ColorCode = "#4CAF50" }
        };

        foreach (var type in leaveTypes)
        {
            context.LeaveTypes.Add(type);
        }
        context.SaveChanges();

        var departments = new Department[]
        {
            new Department { Name = "IT" },
            new Department { Name = "HR" },
            new Department { Name = "Sales" }
        };

        foreach (var dept in departments)
        {
            context.Departments.Add(dept);
        }
        context.SaveChanges();

        var currentYear = DateTime.Now.Year;
        var admin = new User
        {
            Email = "admin@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
            FirstName = "Admin",
            LastName = "User",
            Role = UserRoles.Admin,
            HireDate = new DateTime(2020, 1, 1),
            IsActive = true
        };
        context.Users.Add(admin);
        context.SaveChanges();

        var manager = new User
        {
            Email = "manager@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Manager@123"),
            FirstName = "Manager",
            LastName = "One",
            Role = UserRoles.Manager,
            DepartmentId = departments[0].DepartmentId,
            HireDate = new DateTime(2021, 1, 1),
            IsActive = true
        };
        context.Users.Add(manager);
        context.SaveChanges();

        departments[0].ManagerId = manager.UserId;
        context.Departments.Update(departments[0]);

        var employee = new User
        {
            Email = "employee@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Employee@123"),
            FirstName = "Employee",
            LastName = "One",
            Role = UserRoles.Employee,
            DepartmentId = departments[0].DepartmentId,
            HireDate = new DateTime(2022, 1, 1),
            IsActive = true
        };
        context.Users.Add(employee);
        context.SaveChanges();

        foreach (var user in new[] { admin, manager, employee })
        {
            foreach (var leaveType in leaveTypes)
            {
                var balance = new LeaveBalance
                {
                    UserId = user.UserId,
                    LeaveTypeId = leaveType.LeaveTypeId,
                    Year = currentYear,
                    TotalDays = leaveType.DefaultDaysPerYear,
                    UsedDays = 0
                };
                context.LeaveBalances.Add(balance);
            }
        }
        context.SaveChanges();
    }
}
