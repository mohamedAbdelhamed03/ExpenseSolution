using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Expense.Core.DTOs.Insights;

namespace Expense.Core.Abstractions.Insights
{
    public interface IInsightsService
    {
        Task<IEnumerable<InsightsSummaryDto>> GetInsightsAsync(Guid groupId, string period, string date, string userId, CancellationToken cancellationToken = default);
    }
}
