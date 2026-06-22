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
public class LeaveRequestsController : ControllerBase
{
    private readonly ILeaveRequestService _leaveRequestService;

    public LeaveRequestsController(ILeaveRequestService leaveRequestService)
    {
        _leaveRequestService = leaveRequestService;
    }

    private int GetUserId()
    {
        return int.Parse(User.FindFirst("userId")?.Value ?? "0");
    }

    private int GetUserRole()
    {
        return int.Parse(User.FindFirst(ClaimTypes.Role)?.Value ?? "0");
    }

    [HttpPost]
    public async Task<ActionResult<LeaveRequestDto>> CreateLeaveRequest([FromBody] CreateLeaveRequestDto dto)
    {
        if (GetUserRole() != UserRoles.Employee)
        {
            return Forbid();
        }

        var result = await _leaveRequestService.CreateLeaveRequestAsync(GetUserId(), dto);
        if (result == null)
        {
            return BadRequest("Insufficient leave balance or invalid leave type");
        }

        return CreatedAtAction(nameof(GetLeaveRequest), new { id = result.LeaveRequestId }, result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<LeaveRequestDto>> GetLeaveRequest(int id)
    {
        var result = await _leaveRequestService.GetLeaveRequestByIdAsync(id);
        if (result == null)
        {
            return NotFound();
        }

        return Ok(result);
    }

    [HttpGet("me")]
    public async Task<ActionResult<List<LeaveRequestDto>>> GetMyLeaveRequests()
    {
        var result = await _leaveRequestService.GetMyLeaveRequestsAsync(GetUserId());
        return Ok(result);
    }

    [HttpGet("pending")]
    public async Task<ActionResult<List<LeaveRequestDto>>> GetPendingLeaveRequests()
    {
        if (GetUserRole() != UserRoles.Manager)
        {
            return Forbid();
        }

        var result = await _leaveRequestService.GetPendingLeaveRequestsAsync(GetUserId());
        return Ok(result);
    }

    [HttpPut("{id}/approve")]
    public async Task<IActionResult> ApproveLeaveRequest(int id, [FromBody] ApproveLeaveRequestDto dto)
    {
        if (GetUserRole() != UserRoles.Manager)
        {
            return Forbid();
        }

        var result = await _leaveRequestService.ApproveLeaveRequestAsync(id, GetUserId(), dto);
        if (!result)
        {
            return BadRequest("Unable to approve leave request");
        }

        return NoContent();
    }

    [HttpPut("{id}/reject")]
    public async Task<IActionResult> RejectLeaveRequest(int id, [FromBody] RejectLeaveRequestDto dto)
    {
        if (GetUserRole() != UserRoles.Manager)
        {
            return Forbid();
        }

        var result = await _leaveRequestService.RejectLeaveRequestAsync(id, GetUserId(), dto);
        if (!result)
        {
            return BadRequest("Unable to reject leave request");
        }

        return NoContent();
    }

    [HttpPut("{id}/cancel")]
    public async Task<IActionResult> CancelLeaveRequest(int id)
    {
        var result = await _leaveRequestService.CancelLeaveRequestAsync(id, GetUserId());
        if (!result)
        {
            return BadRequest("Unable to cancel leave request");
        }

        return NoContent();
    }
}
