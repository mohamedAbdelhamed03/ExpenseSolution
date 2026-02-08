using System;
using Expense.Core.Domain.Enums;

namespace Expense.Core.Domain.Entities
{
    public class ActivityLog
    {
        public Guid Id { get; set; }
        public Guid GroupId { get; set; }
        public string UserId { get; set; } = string.Empty; // The actor
        public ActivityType Action { get; set; }
        public EntityType EntityType { get; set; }
        public string EntityId { get; set; } = string.Empty;
        public string? Details { get; set; } // JSON or description
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        public Group? Group { get; set; }
    }
}