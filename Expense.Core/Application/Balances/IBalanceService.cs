using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Expense.Core.DTOs.Balances;

namespace Expense.Core.Application.Balances
{
    public interface IBalanceService
    {
        Task<IEnumerable<BalanceDto>> GetGroupBalancesAsync(Guid groupId, string requesterUserId, CancellationToken cancellationToken);
    }
}
