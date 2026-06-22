using System.Security.Claims;
using LeaveManagement.Api.Helpers;
using LeaveManagement.Api.Models.DTOs;
using LeaveManagement.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LeaveManagement.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly IUserService _userService;

    public DashboardController(IUserService userService)
    {
        _userService = userService;
    }

    private int GetUserId()
    {
        return int.Parse(User.FindFirst("userId")?.Value ?? "0");
    }

    private int GetUserRole()
    {
        return int.Parse(User.FindFirst(ClaimTypes.Role)?.Value ?? "0");
    }

    [HttpGet("summary")]
    public async Task<ActionResult<DashboardSummaryDto>> GetSummary()
    {
        if (GetUserRole() != UserRoles.Manager)
        {
            return Forbid();
        }

        var result = await _userService.GetDashboardSummaryAsync(GetUserId());
        return Ok(result);
    }
}
