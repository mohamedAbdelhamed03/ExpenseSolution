using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Expense.Core.Domain.Entities;

namespace Expense.Core.Abstractions.Persistence
{
    public interface IBalanceRepository
    {
        Task<IEnumerable<GroupMember>> GetMembersAsync(Guid groupId, CancellationToken cancellationToken);
        Task<IEnumerable<Expense.Core.Domain.Entities.Expense>> GetExpensesWithSplitsAsync(Guid groupId, CancellationToken cancellationToken);
    }
}
