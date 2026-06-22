using LeaveManagement.Api.Data;
using LeaveManagement.Api.Helpers;
using LeaveManagement.Api.Models.DTOs;
using LeaveManagement.Api.Models.Entities;
using LeaveManagement.Api.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LeaveManagement.Api.Services;

public class UserService : IUserService
{
    private readonly AppDbContext _context;
    private readonly ILeaveTypeService _leaveTypeService;

    public UserService(AppDbContext context, ILeaveTypeService leaveTypeService)
    {
        _context = context;
        _leaveTypeService = leaveTypeService;
    }

    public async Task<UserDto?> CreateUserAsync(CreateUserDto dto)
    {
        var existingUser = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == dto.Email);
        if (existingUser != null) return null;

        var passwordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);
        var user = new User
        {
            Email = dto.Email,
            PasswordHash = passwordHash,
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            Role = dto.Role,
            DepartmentId = dto.DepartmentId,
            HireDate = dto.HireDate,
            IsActive = true
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var currentYear = DateTime.Now.Year;
        var leaveTypes = await _leaveTypeService.GetAllLeaveTypesAsync();
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
            _context.LeaveBalances.Add(balance);
        }
        await _context.SaveChangesAsync();

        return new UserDto
        {
            UserId = user.UserId,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Role = user.Role,
            DepartmentId = user.DepartmentId,
            HireDate = user.HireDate,
            IsActive = user.IsActive
        };
    }

    public async Task<UserDto?> GetUserByIdAsync(int id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null) return null;

        return new UserDto
        {
            UserId = user.UserId,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Role = user.Role,
            DepartmentId = user.DepartmentId,
            HireDate = user.HireDate,
            IsActive = user.IsActive
        };
    }

    public async Task<UserDto?> GetUserByEmailAsync(string email)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == email);
        if (user == null) return null;

        return new UserDto
        {
            UserId = user.UserId,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Role = user.Role,
            DepartmentId = user.DepartmentId,
            HireDate = user.HireDate,
            IsActive = user.IsActive
        };
    }

    public async Task<bool> UpdateUserAsync(int id, UpdateUserDto dto)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null) return false;

        user.Role = dto.Role;
        user.DepartmentId = dto.DepartmentId;
        user.IsActive = dto.IsActive;

        _context.Users.Update(user);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<DashboardSummaryDto> GetDashboardSummaryAsync(int managerId)
    {
        var department = await _context.Departments
            .FirstOrDefaultAsync(d => d.ManagerId == managerId);

        if (department == null)
        {
            return new DashboardSummaryDto();
        }

        var pendingCount = await _context.LeaveRequests
            .CountAsync(lr => lr.User.DepartmentId == department.DepartmentId
                && lr.Status == LeaveRequestStatus.Pending);

        var approvedCount = await _context.LeaveRequests
            .CountAsync(lr => lr.User.DepartmentId == department.DepartmentId
                && lr.Status == LeaveRequestStatus.Approved);

        var rejectedCount = await _context.LeaveRequests
            .CountAsync(lr => lr.User.DepartmentId == department.DepartmentId
                && lr.Status == LeaveRequestStatus.Rejected);

        return new DashboardSummaryDto
        {
            PendingCount = pendingCount,
            ApprovedCount = approvedCount,
            RejectedCount = rejectedCount
        };
    }
}
