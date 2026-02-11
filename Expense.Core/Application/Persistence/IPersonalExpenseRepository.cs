using Expense.Core.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Expense.Core.Application.Persistence
{
    public interface IPersonalExpenseRepository : IRepository<PersonalExpense>
    {
        Task<IEnumerable<PersonalExpense>> GetUserPersonalExpensesAsync(string userId, int page, int pageSize, CancellationToken cancellationToken = default);
        Task<IEnumerable<DTOs.Insights.CategoryStatistics>> GetPersonalExpensesByCategoryAsync(string userId, DateTime startDate, DateTime endDate);
    }
}
