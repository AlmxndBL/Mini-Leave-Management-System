using LeaveManagement.Api.Data;
using LeaveManagement.Api.Models.DTOs;
using LeaveManagement.Api.Models.Entities;
using LeaveManagement.Api.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LeaveManagement.Api.Services;

public class LeaveTypeService : ILeaveTypeService
{
    private readonly AppDbContext _context;

    public LeaveTypeService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<LeaveTypeDto?> CreateLeaveTypeAsync(CreateLeaveTypeDto dto)
    {
        var leaveType = new LeaveType
        {
            Name = dto.Name,
            DefaultDaysPerYear = dto.DefaultDaysPerYear,
            ColorCode = dto.ColorCode
        };

        _context.LeaveTypes.Add(leaveType);
        await _context.SaveChangesAsync();

        return new LeaveTypeDto
        {
            LeaveTypeId = leaveType.LeaveTypeId,
            Name = leaveType.Name,
            DefaultDaysPerYear = leaveType.DefaultDaysPerYear,
            ColorCode = leaveType.ColorCode
        };
    }

    public async Task<LeaveTypeDto?> GetLeaveTypeByIdAsync(int id)
    {
        var leaveType = await _context.LeaveTypes.FindAsync(id);
        if (leaveType == null) return null;

        return new LeaveTypeDto
        {
            LeaveTypeId = leaveType.LeaveTypeId,
            Name = leaveType.Name,
            DefaultDaysPerYear = leaveType.DefaultDaysPerYear,
            ColorCode = leaveType.ColorCode
        };
    }

    public async Task<List<LeaveTypeDto>> GetAllLeaveTypesAsync()
    {
        return await _context.LeaveTypes
            .Select(lt => new LeaveTypeDto
            {
                LeaveTypeId = lt.LeaveTypeId,
                Name = lt.Name,
                DefaultDaysPerYear = lt.DefaultDaysPerYear,
                ColorCode = lt.ColorCode
            })
            .ToListAsync();
    }

    public async Task<bool> UpdateLeaveTypeAsync(int id, UpdateLeaveTypeDto dto)
    {
        var leaveType = await _context.LeaveTypes.FindAsync(id);
        if (leaveType == null) return false;

        leaveType.Name = dto.Name;
        leaveType.DefaultDaysPerYear = dto.DefaultDaysPerYear;
        leaveType.ColorCode = dto.ColorCode;

        _context.LeaveTypes.Update(leaveType);
        await _context.SaveChangesAsync();
        return true;
    }
}
