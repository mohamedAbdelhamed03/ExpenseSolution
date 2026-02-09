using System;

namespace Expense.Core.DTOs.Insights
{
    public class CategoryInsightDto
    {
        public Guid? CategoryId { get; set; }
        public string CategoryName { get; set; } = "Uncategorized";
        public decimal Amount { get; set; }
        public decimal Percentage { get; set; }
        public string Currency { get; set; } = string.Empty;
    }
}
