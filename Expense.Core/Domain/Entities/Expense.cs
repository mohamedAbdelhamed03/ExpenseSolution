using System;
using System.Collections.Generic;

namespace Expense.Core.Domain.Entities
{
    public class Expense
    {
        public Guid Id { get; set; }
        public Guid GroupId { get; set; }
        public string PaidByUserId { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "EGP";
        public decimal? ExchangeRate { get; set; }
        public string? Description { get; set; }
        public DateTime ExpenseDate { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public Guid? CategoryId { get; set; }
        public ExpenseCategory? Category { get; set; }

        public Group? Group { get; set; }
        public ICollection<ExpenseSplit> Splits { get; set; } = new List<ExpenseSplit>();
    }
}
