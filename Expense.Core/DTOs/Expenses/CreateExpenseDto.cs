using System;
using System.Collections.Generic;

namespace Expense.Core.DTOs.Expenses
{
    public class CreateExpenseDto
    {
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "EGP";
        public decimal? ExchangeRate { get; set; }
        public string? Description { get; set; }
        public Guid? CategoryId { get; set; }
        public DateTime ExpenseDate { get; set; }
        public IEnumerable<ExpenseSplitDto>? Splits { get; set; } = Array.Empty<ExpenseSplitDto>();
    }

    public class ExpenseSplitDto
    {
        public string UserId { get; set; } = string.Empty;
        public decimal Amount { get; set; }
    }
}
