namespace LeaveManagement.Api.Models.DTOs;

public class NotificationDto
{
    public int NotificationId { get; set; }
    public required string Type { get; set; }
    public required string Title { get; set; }
    public required string Message { get; set; }
    public bool IsRead { get; set; }
    public string? RelatedEntityType { get; set; }
    public int? RelatedEntityId { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class UnreadCountDto { public int Count { get; set; } }
