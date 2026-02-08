using System;

namespace Expense.Core.Domain.Entities
{
    public class ExpenseCategory
    {
        public Guid Id { get; set; }
        public Guid GroupId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsSystem { get; set; } = false; // For default categories
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public Group? Group { get; set; }
    }
}