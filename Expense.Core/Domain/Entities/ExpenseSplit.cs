using System;

namespace Expense.Core.Domain.Entities
{
    public class ExpenseSplit
    {
        public Guid Id { get; set; }
        public Guid ExpenseId { get; set; }
        public string UserId { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public Expense? Expense { get; set; }
    }
}
