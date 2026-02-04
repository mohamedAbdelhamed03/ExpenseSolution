using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Expense.Core.DTOs.Balances;

namespace Expense.Core.Abstractions.Balances
{
    public interface IBalanceService
    {
        Task<IEnumerable<BalanceDto>> GetGroupBalancesAsync(Guid groupId, string requesterUserId);
    }
}
