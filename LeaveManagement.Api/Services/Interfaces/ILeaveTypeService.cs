using LeaveManagement.Api.Models.DTOs;

namespace LeaveManagement.Api.Services.Interfaces;

public interface ILeaveTypeService
{
    Task<LeaveTypeDto?> CreateLeaveTypeAsync(CreateLeaveTypeDto dto);
    Task<LeaveTypeDto?> GetLeaveTypeByIdAsync(int id);
    Task<List<LeaveTypeDto>> GetAllLeaveTypesAsync();
    Task<bool> UpdateLeaveTypeAsync(int id, UpdateLeaveTypeDto dto);
}
