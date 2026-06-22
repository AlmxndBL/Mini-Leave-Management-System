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
public class LeaveTypesController : ControllerBase
{
    private readonly ILeaveTypeService _leaveTypeService;

    public LeaveTypesController(ILeaveTypeService leaveTypeService)
    {
        _leaveTypeService = leaveTypeService;
    }

    private int GetUserRole()
    {
        return int.Parse(User.FindFirst(ClaimTypes.Role)?.Value ?? "0");
    }

    [HttpPost]
    public async Task<ActionResult<LeaveTypeDto>> CreateLeaveType([FromBody] CreateLeaveTypeDto dto)
    {
        if (GetUserRole() != UserRoles.Admin)
        {
            return Forbid();
        }

        var result = await _leaveTypeService.CreateLeaveTypeAsync(dto);
        return CreatedAtAction(nameof(GetLeaveType), new { id = result!.LeaveTypeId }, result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<LeaveTypeDto>> GetLeaveType(int id)
    {
        var result = await _leaveTypeService.GetLeaveTypeByIdAsync(id);
        if (result == null)
        {
            return NotFound();
        }

        return Ok(result);
    }

    [HttpGet]
    public async Task<ActionResult<List<LeaveTypeDto>>> GetAllLeaveTypes()
    {
        var result = await _leaveTypeService.GetAllLeaveTypesAsync();
        return Ok(result);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateLeaveType(int id, [FromBody] UpdateLeaveTypeDto dto)
    {
        if (GetUserRole() != UserRoles.Admin)
        {
            return Forbid();
        }

        var result = await _leaveTypeService.UpdateLeaveTypeAsync(id, dto);
        if (!result)
        {
            return NotFound();
        }

        return NoContent();
    }
}
