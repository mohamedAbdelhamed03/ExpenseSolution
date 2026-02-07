using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Expense.Core.Abstractions.Balances;
using Expense.Core.Abstractions.Persistence;
using Expense.Core.Domain.Entities;
using Expense.Core.DTOs.Balances;
using Expense.Core.Common.Exceptions;
using ExpenseEntity = Expense.Core.Domain.Entities.Expense;

namespace Expense.Infrastructure.Balances
{
    public class BalanceService : IBalanceService
    {
        private readonly IUnitOfWork _unitOfWork;
        
        public BalanceService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<IEnumerable<BalanceDto>> GetGroupBalancesAsync(Guid groupId, string requesterUserId, CancellationToken cancellationToken)
        {
            var isMember = await _unitOfWork.Groups.IsMemberAsync(groupId, requesterUserId, cancellationToken);
            if (!isMember) throw new BusinessException("Not a group member");
            
            var members = (await _unitOfWork.Balances.GetMembersAsync(groupId, cancellationToken))
                .Select(m => m.UserId)
                .ToList();

            // Fetch all expenses for the group with splits
            var expenses = await _unitOfWork.Balances.GetExpensesWithSplitsAsync(groupId, cancellationToken);
            
            var totalPaid = expenses
                .GroupBy(e => e.PaidByUserId)
                .Select(g => new { UserId = g.Key, Total = g.Sum(x => x.Amount) })
                .ToList();

            var totalShared = expenses
                .SelectMany(e => e.Splits)
                .GroupBy(s => s.UserId)
                .Select(g => new { UserId = g.Key, Total = g.Sum(x => x.Amount) })
                .ToList();
                
            var paidDict = totalPaid.ToDictionary(x => x.UserId, x => x.Total);
            var sharedDict = totalShared.ToDictionary(x => x.UserId, x => x.Total);
            
            var balances = members.Select(u => new BalanceDto
            {
                UserId = u,
                TotalPaid = paidDict.TryGetValue(u, out var p) ? p : 0m,
                TotalShared = sharedDict.TryGetValue(u, out var s) ? s : 0m,
                Balance = (paidDict.TryGetValue(u, out var pp) ? pp : 0m) - (sharedDict.TryGetValue(u, out var ss) ? ss : 0m)
            }).ToList();
            
            return balances;
        }
    }
}
