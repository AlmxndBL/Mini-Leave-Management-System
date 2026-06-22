using LeaveManagement.Api.Models.DTOs;

namespace LeaveManagement.Api.Services.Interfaces;

public interface ILeaveBalanceService
{
    Task<List<LeaveBalanceDto>> GetMyLeaveBalancesAsync(int userId);
    Task<bool> GenerateLeaveBalancesForYearAsync(int year);
}
