using System;
using System.Collections.Generic;

namespace Expense.Core.DTOs.Notifications
{
    public class MarkReadDto
    {
        public List<Guid> NotificationIds { get; set; } = new();
    }
}
