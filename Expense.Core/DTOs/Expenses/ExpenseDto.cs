using System;
using System.Collections.Generic;

namespace Expense.Core.DTOs.Expenses
{
    public class ExpenseDto
    {
        public Guid Id { get; set; }
        public Guid GroupId { get; set; }
        public string PaidByUserId { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Currency { get; set; } = string.Empty;
        public decimal? ExchangeRate { get; set; }
        public string? Description { get; set; }
        public Guid? CategoryId { get; set; }
        public DateTime ExpenseDate { get; set; }
        public IEnumerable<ExpenseSplitDto> Splits { get; set; } = Array.Empty<ExpenseSplitDto>();
    }
}
