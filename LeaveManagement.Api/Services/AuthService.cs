using LeaveManagement.Api.Data;
using LeaveManagement.Api.Helpers;
using LeaveManagement.Api.Models.DTOs;
using LeaveManagement.Api.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LeaveManagement.Api.Services;

public class AuthService : IAuthService
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _configuration;

    public AuthService(AppDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    public async Task<LoginResponse?> LoginAsync(LoginRequest request)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email && u.IsActive);

        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            return null;
        }

        var secretKey = _configuration["Jwt:SecretKey"]
            ?? throw new InvalidOperationException("JWT SecretKey not configured");
        var token = JwtHelper.GenerateToken(user.UserId, user.Email, user.Role, secretKey);

        return new LoginResponse
        {
            UserId = user.UserId,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Role = user.Role,
            DepartmentId = user.DepartmentId,
            AccessToken = token
        };
    }
}
