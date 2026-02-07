using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Expense.Core.Domain.Entities;

namespace Expense.Core.Abstractions.Persistence
{
    public interface IExpenseRepository : IRepository<Expense.Core.Domain.Entities.Expense>
    {
        Task<IEnumerable<Expense.Core.Domain.Entities.Expense>> GetExpensesByGroupAsync(Guid groupId);
    }
}
