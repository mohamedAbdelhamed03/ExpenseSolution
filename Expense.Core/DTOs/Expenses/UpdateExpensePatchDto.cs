using System;

namespace Expense.Core.DTOs.Expenses
{
    public class UpdateExpensePatchDto
    {
        public string? Description { get; set; }
        public Guid? CategoryId { get; set; }
        public DateTime? ExpenseDate { get; set; }
    }
}
