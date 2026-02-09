using System;
using System.Collections.Generic;

namespace Expense.Core.DTOs.Insights
{
    public class InsightsSummaryDto
    {
        public Guid GroupId { get; set; }
        public string Period { get; set; } = string.Empty;
        public string Date { get; set; } = string.Empty;
        public string Currency { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public List<CategoryInsightDto> Categories { get; set; } = new();
    }
}
