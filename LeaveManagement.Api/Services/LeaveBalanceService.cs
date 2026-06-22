using LeaveManagement.Api.Data;
using LeaveManagement.Api.Models.DTOs;
using LeaveManagement.Api.Models.Entities;
using LeaveManagement.Api.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LeaveManagement.Api.Services;

public class LeaveBalanceService : ILeaveBalanceService
{
    private readonly AppDbContext _context;

    public LeaveBalanceService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<LeaveBalanceDto>> GetMyLeaveBalancesAsync(int userId)
    {
        return await _context.LeaveBalances
            .Where(lb => lb.UserId == userId && lb.Year == DateTime.Now.Year)
            .Include(lb => lb.LeaveType)
            .Select(lb => new LeaveBalanceDto
            {
                LeaveBalanceId = lb.LeaveBalanceId,
                LeaveTypeName = lb.LeaveType.Name,
                ColorCode = lb.LeaveType.ColorCode,
                TotalDays = lb.TotalDays,
                UsedDays = lb.UsedDays,
                Year = lb.Year
            })
            .ToListAsync();
    }

    public async Task<bool> GenerateLeaveBalancesForYearAsync(int year)
    {
        var leaveTypes = await _context.LeaveTypes.ToListAsync();
        var users = await _context.Users.Where(u => u.IsActive).ToListAsync();

        foreach (var user in users)
        {
            foreach (var leaveType in leaveTypes)
            {
                var existing = await _context.LeaveBalances
                    .FirstOrDefaultAsync(lb => lb.UserId == user.UserId
                        && lb.LeaveTypeId == leaveType.LeaveTypeId
                        && lb.Year == year);

                if (existing == null)
                {
                    var balance = new LeaveBalance
                    {
                        UserId = user.UserId,
                        LeaveTypeId = leaveType.LeaveTypeId,
                        Year = year,
                        TotalDays = leaveType.DefaultDaysPerYear,
                        UsedDays = 0
                    };
                    _context.LeaveBalances.Add(balance);
                }
            }
        }

        await _context.SaveChangesAsync();
        return true;
    }
}
