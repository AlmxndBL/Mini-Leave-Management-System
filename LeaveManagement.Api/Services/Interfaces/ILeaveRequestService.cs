using LeaveManagement.Api.Models.DTOs;

namespace LeaveManagement.Api.Services.Interfaces;

public interface ILeaveRequestService
{
    Task<LeaveRequestDto?> CreateLeaveRequestAsync(int userId, CreateLeaveRequestDto dto);
    Task<LeaveRequestDto?> GetLeaveRequestByIdAsync(int id);
    Task<List<LeaveRequestDto>> GetMyLeaveRequestsAsync(int userId);
    Task<List<LeaveRequestDto>> GetPendingLeaveRequestsAsync(int managerId);
    Task<bool> ApproveLeaveRequestAsync(int id, int approverId, ApproveLeaveRequestDto dto);
    Task<bool> RejectLeaveRequestAsync(int id, int approverId, RejectLeaveRequestDto dto);
    Task<bool> CancelLeaveRequestAsync(int id, int userId);
}
