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
public class LeaveBalancesController : ControllerBase
{
    private readonly ILeaveBalanceService _leaveBalanceService;

    public LeaveBalancesController(ILeaveBalanceService leaveBalanceService)
    {
        _leaveBalanceService = leaveBalanceService;
    }

    private int GetUserId()
    {
        return int.Parse(User.FindFirst("userId")?.Value ?? "0");
    }

    private int GetUserRole()
    {
        return int.Parse(User.FindFirst(ClaimTypes.Role)?.Value ?? "0");
    }

    [HttpGet("me")]
    public async Task<ActionResult<List<LeaveBalanceDto>>> GetMyLeaveBalances()
    {
        var result = await _leaveBalanceService.GetMyLeaveBalancesAsync(GetUserId());
        return Ok(result);
    }

    [HttpPost("generate")]
    public async Task<IActionResult> GenerateLeaveBalances([FromQuery] int year)
    {
        if (GetUserRole() != UserRoles.Admin)
        {
            return Forbid();
        }

        var result = await _leaveBalanceService.GenerateLeaveBalancesForYearAsync(year);
        if (!result)
        {
            return BadRequest("Unable to generate leave balances");
        }

        return Ok("Leave balances generated successfully");
    }
}
