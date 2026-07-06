using LeaveManagement.Api.Models.DTOs;

namespace LeaveManagement.Api.Services.Interfaces;

public interface INotificationService
{
    Task CreateAsync(int userId, string type, string title, string message, string? relatedEntityType = null, int? relatedEntityId = null);
    Task<List<NotificationDto>> GetMyNotificationsAsync(int userId, bool unreadOnly = false);
    Task<int> GetUnreadCountAsync(int userId);
    Task<bool> MarkAsReadAsync(int notificationId, int userId);
    Task MarkAllAsReadAsync(int userId);
}
