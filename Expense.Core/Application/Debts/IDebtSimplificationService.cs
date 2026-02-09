using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Expense.Core.DTOs.Debts;

namespace Expense.Core.Application.Debts
{
    public interface IDebtSimplificationService
    {
        Task<IEnumerable<SimplifiedDebtGroupDto>> GetSimplifiedDebtsAsync(Guid groupId, string userId, CancellationToken cancellationToken);
    }
}
