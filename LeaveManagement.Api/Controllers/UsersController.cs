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
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
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

    [HttpPost]
    public async Task<ActionResult<UserDto>> CreateUser([FromBody] CreateUserDto dto)
    {
        if (GetUserRole() != UserRoles.Admin)
        {
            return Forbid();
        }

        var result = await _userService.CreateUserAsync(dto);
        if (result == null)
        {
            return BadRequest("Email already exists");
        }

        return CreatedAtAction(nameof(GetUser), new { id = result.UserId }, result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<UserDto>> GetUser(int id)
    {
        var result = await _userService.GetUserByIdAsync(id);
        if (result == null)
        {
            return NotFound();
        }

        return Ok(result);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateUser(int id, [FromBody] UpdateUserDto dto)
    {
        if (GetUserRole() != UserRoles.Admin)
        {
            return Forbid();
        }

        var result = await _userService.UpdateUserAsync(id, dto);
        if (!result)
        {
            return NotFound();
        }

        return NoContent();
    }
}
