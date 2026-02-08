using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Expense.Core.Domain.Entities;

namespace Expense.Core.Abstractions.Persistence
{
    public interface IExpenseCategoryRepository : IRepository<ExpenseCategory>
    {
        Task<IEnumerable<ExpenseCategory>> GetCategoriesForGroupAsync(Guid groupId, CancellationToken cancellationToken);
        Task<ExpenseCategory?> GetByNameAsync(Guid groupId, string name, CancellationToken cancellationToken);
    }
}