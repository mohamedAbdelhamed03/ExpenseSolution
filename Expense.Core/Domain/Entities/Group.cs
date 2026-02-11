using System;
using System.Collections.Generic;

namespace Expense.Core.Domain.Entities
{
    public class Group
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string CreatedByUserId { get; set; } = string.Empty;
        public string InviteCode { get; set; } = string.Empty;
        public string? LogoUrl { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public ICollection<GroupMember> Members { get; set; } = new List<GroupMember>();
        public ICollection<Expense> Expenses { get; set; } = new List<Expense>();
        public ICollection<ExpenseCategory> Categories { get; set; } = new List<ExpenseCategory>();
        public ICollection<ActivityLog> ActivityLogs { get; set; } = new List<ActivityLog>();
        public ICollection<Settlement> Settlements { get; set; } = new List<Settlement>();
    }
}
