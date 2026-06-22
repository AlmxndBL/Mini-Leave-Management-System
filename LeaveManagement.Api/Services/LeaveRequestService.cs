using LeaveManagement.Api.Data;
using LeaveManagement.Api.Helpers;
using LeaveManagement.Api.Models.DTOs;
using LeaveManagement.Api.Models.Entities;
using LeaveManagement.Api.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LeaveManagement.Api.Services;

public class LeaveRequestService : ILeaveRequestService
{
    private readonly AppDbContext _context;

    public LeaveRequestService(AppDbContext context)
    {
        _context = context;
    }

    private decimal CalculateTotalDays(DateTime startDate, DateTime endDate)
    {
        decimal totalDays = 0;
        for (var date = startDate; date <= endDate; date = date.AddDays(1))
        {
            if (date.DayOfWeek != DayOfWeek.Saturday && date.DayOfWeek != DayOfWeek.Sunday)
            {
                totalDays += 1;
            }
        }
        return totalDays;
    }

    public async Task<LeaveRequestDto?> CreateLeaveRequestAsync(int userId, CreateLeaveRequestDto dto)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null) return null;

        var leaveType = await _context.LeaveTypes.FindAsync(dto.LeaveTypeId);
        if (leaveType == null) return null;

        var totalDays = CalculateTotalDays(dto.StartDate, dto.EndDate);
        var currentYear = DateTime.Now.Year;

        var balance = await _context.LeaveBalances
            .FirstOrDefaultAsync(lb => lb.UserId == userId
                && lb.LeaveTypeId == dto.LeaveTypeId
                && lb.Year == currentYear);

        if (balance == null || (balance.TotalDays - balance.UsedDays) < totalDays)
        {
            return null;
        }

        var leaveRequest = new LeaveRequest
        {
            UserId = userId,
            LeaveTypeId = dto.LeaveTypeId,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            TotalDays = totalDays,
            Reason = dto.Reason,
            Status = LeaveRequestStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.LeaveRequests.Add(leaveRequest);
        await _context.SaveChangesAsync();

        return new LeaveRequestDto
        {
            LeaveRequestId = leaveRequest.LeaveRequestId,
            EmployeeName = $"{user.FirstName} {user.LastName}",
            LeaveTypeName = leaveType.Name,
            StartDate = leaveRequest.StartDate,
            EndDate = leaveRequest.EndDate,
            TotalDays = leaveRequest.TotalDays,
            Reason = leaveRequest.Reason,
            Status = leaveRequest.Status,
            CreatedAt = leaveRequest.CreatedAt
        };
    }

    public async Task<LeaveRequestDto?> GetLeaveRequestByIdAsync(int id)
    {
        var leaveRequest = await _context.LeaveRequests
            .Include(lr => lr.User)
            .Include(lr => lr.LeaveType)
            .FirstOrDefaultAsync(lr => lr.LeaveRequestId == id);

        if (leaveRequest == null) return null;

        return new LeaveRequestDto
        {
            LeaveRequestId = leaveRequest.LeaveRequestId,
            EmployeeName = $"{leaveRequest.User.FirstName} {leaveRequest.User.LastName}",
            LeaveTypeName = leaveRequest.LeaveType.Name,
            StartDate = leaveRequest.StartDate,
            EndDate = leaveRequest.EndDate,
            TotalDays = leaveRequest.TotalDays,
            Reason = leaveRequest.Reason,
            Status = leaveRequest.Status,
            CreatedAt = leaveRequest.CreatedAt,
            ApproverComment = leaveRequest.ApproverComment
        };
    }

    public async Task<List<LeaveRequestDto>> GetMyLeaveRequestsAsync(int userId)
    {
        return await _context.LeaveRequests
            .Where(lr => lr.UserId == userId)
            .Include(lr => lr.User)
            .Include(lr => lr.LeaveType)
            .OrderByDescending(lr => lr.CreatedAt)
            .Select(lr => new LeaveRequestDto
            {
                LeaveRequestId = lr.LeaveRequestId,
                EmployeeName = $"{lr.User.FirstName} {lr.User.LastName}",
                LeaveTypeName = lr.LeaveType.Name,
                StartDate = lr.StartDate,
                EndDate = lr.EndDate,
                TotalDays = lr.TotalDays,
                Reason = lr.Reason,
                Status = lr.Status,
                CreatedAt = lr.CreatedAt,
                ApproverComment = lr.ApproverComment
            })
            .ToListAsync();
    }

    public async Task<List<LeaveRequestDto>> GetPendingLeaveRequestsAsync(int managerId)
    {
        var department = await _context.Departments
            .FirstOrDefaultAsync(d => d.ManagerId == managerId);

        if (department == null) return [];

        return await _context.LeaveRequests
            .Where(lr => lr.User.DepartmentId == department.DepartmentId
                && lr.Status == LeaveRequestStatus.Pending)
            .Include(lr => lr.User)
            .Include(lr => lr.LeaveType)
            .OrderByDescending(lr => lr.CreatedAt)
            .Select(lr => new LeaveRequestDto
            {
                LeaveRequestId = lr.LeaveRequestId,
                EmployeeName = $"{lr.User.FirstName} {lr.User.LastName}",
                LeaveTypeName = lr.LeaveType.Name,
                StartDate = lr.StartDate,
                EndDate = lr.EndDate,
                TotalDays = lr.TotalDays,
                Reason = lr.Reason,
                Status = lr.Status,
                CreatedAt = lr.CreatedAt
            })
            .ToListAsync();
    }

    public async Task<bool> ApproveLeaveRequestAsync(int id, int approverId, ApproveLeaveRequestDto dto)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var leaveRequest = await _context.LeaveRequests
                .Include(lr => lr.User)
                .ThenInclude(u => u.Department)
                .FirstOrDefaultAsync(lr => lr.LeaveRequestId == id);

            if (leaveRequest == null) return false;

            var approver = await _context.Users.FindAsync(approverId);
            if (approver == null) return false;

            if (leaveRequest.User.Department?.ManagerId != approverId)
            {
                return false;
            }

            if (leaveRequest.Status != LeaveRequestStatus.Pending)
            {
                return false;
            }

            var currentYear = DateTime.Now.Year;
            var balance = await _context.LeaveBalances
                .FirstOrDefaultAsync(lb => lb.UserId == leaveRequest.UserId
                    && lb.LeaveTypeId == leaveRequest.LeaveTypeId
                    && lb.Year == currentYear);

            if (balance == null || (balance.TotalDays - balance.UsedDays) < leaveRequest.TotalDays)
            {
                return false;
            }

            leaveRequest.Status = LeaveRequestStatus.Approved;
            leaveRequest.ApproverId = approverId;
            leaveRequest.ApproverComment = dto.Comment;
            leaveRequest.UpdatedAt = DateTime.UtcNow;

            balance.UsedDays += leaveRequest.TotalDays;

            _context.LeaveRequests.Update(leaveRequest);
            _context.LeaveBalances.Update(balance);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return true;
        }
        catch
        {
            await transaction.RollbackAsync();
            return false;
        }
    }

    public async Task<bool> RejectLeaveRequestAsync(int id, int approverId, RejectLeaveRequestDto dto)
    {
        var leaveRequest = await _context.LeaveRequests
            .Include(lr => lr.User)
            .ThenInclude(u => u.Department)
            .FirstOrDefaultAsync(lr => lr.LeaveRequestId == id);

        if (leaveRequest == null) return false;

        if (leaveRequest.User.Department?.ManagerId != approverId)
        {
            return false;
        }

        if (leaveRequest.Status != LeaveRequestStatus.Pending)
        {
            return false;
        }

        leaveRequest.Status = LeaveRequestStatus.Rejected;
        leaveRequest.ApproverId = approverId;
        leaveRequest.ApproverComment = dto.Comment;
        leaveRequest.UpdatedAt = DateTime.UtcNow;

        _context.LeaveRequests.Update(leaveRequest);
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> CancelLeaveRequestAsync(int id, int userId)
    {
        var leaveRequest = await _context.LeaveRequests.FindAsync(id);
        if (leaveRequest == null || leaveRequest.UserId != userId) return false;

        if (leaveRequest.Status != LeaveRequestStatus.Pending)
        {
            return false;
        }

        leaveRequest.Status = LeaveRequestStatus.Cancelled;
        leaveRequest.UpdatedAt = DateTime.UtcNow;

        _context.LeaveRequests.Update(leaveRequest);
        await _context.SaveChangesAsync();

        return true;
    }
}
