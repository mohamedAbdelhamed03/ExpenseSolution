using System;

namespace Expense.Core.DTOs.Notifications
{
    public class NotificationMessage
    {
        public string Type { get; set; } = string.Empty;
        public Guid GroupId { get; set; }
        public string ActorUserId { get; set; } = string.Empty;
        public object? Payload { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
