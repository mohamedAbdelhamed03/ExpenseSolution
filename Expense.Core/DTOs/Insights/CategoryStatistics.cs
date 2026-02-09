using System;

namespace Expense.Core.DTOs.Insights
{
    public class CategoryStatistics
    {
        public Guid? CategoryId { get; set; }
        public decimal TotalAmount { get; set; }
        public string Currency { get; set; } = string.Empty;
    }
}
