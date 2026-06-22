using LeaveManagement.Api.Models.DTOs;

namespace LeaveManagement.Api.Services.Interfaces;

public interface IAuthService
{
    Task<LoginResponse?> LoginAsync(LoginRequest request);
}
