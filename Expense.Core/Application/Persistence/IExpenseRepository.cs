using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Expense.Core.Domain.Entities;
using Expense.Core.DTOs.Insights;

namespace Expense.Core.Application.Persistence
{
    public interface IExpenseRepository : IRepository<Expense.Core.Domain.Entities.Expense>
    {
        Task<IEnumerable<Expense.Core.Domain.Entities.Expense>> GetExpensesByGroupAsync(Guid groupId);
        Task<IEnumerable<CategoryStatistics>> GetInsightsByCategoryAsync(Guid groupId, DateTime startDate, DateTime endDate);
    }
}
