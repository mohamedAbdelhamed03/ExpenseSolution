using System;
using Expense.Core.Domain.Enums;

namespace Expense.Core.DTOs.ActivityLogs
{
    public class ActivityLogDto
    {
        public Guid Id { get; set; }
        public Guid GroupId { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string EntityType { get; set; } = string.Empty;
        public string EntityId { get; set; } = string.Empty;
        public string? Details { get; set; }
        public DateTime Timestamp { get; set; }
    }
}