using LeaveManagement.Api.Models.DTOs;

namespace LeaveManagement.Api.Services.Interfaces;

public interface IUserService
{
    Task<UserDto?> CreateUserAsync(CreateUserDto dto);
    Task<UserDto?> GetUserByIdAsync(int id);
    Task<UserDto?> GetUserByEmailAsync(string email);
    Task<bool> UpdateUserAsync(int id, UpdateUserDto dto);
    Task<DashboardSummaryDto> GetDashboardSummaryAsync(int managerId);
}
