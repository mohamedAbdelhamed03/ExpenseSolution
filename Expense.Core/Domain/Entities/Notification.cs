using System;

namespace Expense.Core.Domain.Entities
{
    public class Notification
    {
        public Guid Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public Guid? GroupId { get; set; }
        public string Message { get; set; } = string.Empty; // Serialized JSON or simple text
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
