using LeaveManagement.Api.Models.DTOs;
using LeaveManagement.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LeaveManagement.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly INotificationService _notificationService;

    public NotificationsController(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    private int GetUserId() => int.Parse(User.FindFirst("userId")?.Value ?? "0");

    [HttpGet("me")]
    public async Task<ActionResult<List<NotificationDto>>> GetMyNotifications([FromQuery] bool unreadOnly = false)
    {
        var result = await _notificationService.GetMyNotificationsAsync(GetUserId(), unreadOnly);
        return Ok(result);
    }

    [HttpGet("me/unread-count")]
    public async Task<ActionResult<UnreadCountDto>> GetUnreadCount()
    {
        var count = await _notificationService.GetUnreadCountAsync(GetUserId());
        return Ok(new UnreadCountDto { Count = count });
    }

    [HttpPut("{id}/read")]
    public async Task<IActionResult> MarkAsRead(int id)
    {
        var success = await _notificationService.MarkAsReadAsync(id, GetUserId());
        if (!success) return NotFound();
        return NoContent();
    }

    [HttpPut("read-all")]
    public async Task<IActionResult> MarkAllAsRead()
    {
        await _notificationService.MarkAllAsReadAsync(GetUserId());
        return NoContent();
    }
}
