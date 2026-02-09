using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Expense.Core.Application.Balances;
using Expense.Core.Application.Persistence;
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
            
            // Fetch all settlements for the group
            var settlements = await _unitOfWork.Balances.GetSettlementsAsync(groupId, cancellationToken);

            var totalPaid = expenses
                .GroupBy(e => e.PaidByUserId)
                .Select(g => new { UserId = g.Key, Total = g.Sum(x => x.Amount * (x.ExchangeRate ?? 1m)) })
                .ToList();

            var totalShared = expenses
                .SelectMany(e => e.Splits.Select(s => new { Split = s, Rate = e.ExchangeRate ?? 1m }))
                .GroupBy(x => x.Split.UserId)
                .Select(g => new { UserId = g.Key, Total = g.Sum(x => x.Split.Amount * x.Rate) })
                .ToList();

            // Calculate Settlements
            // If User A pays User B (Settlement), A's "TotalPaid" effectively increases (or debt decreases), B's "TotalPaid" effectively decreases (or debt increases).
            // Actually, it's easier to think about Balance = (Paid - Shared).
            // Settlement: Payer pays Payee.
            // Payer's balance increases (gets closer to positive). Payee's balance decreases (gets closer to negative or 0).
            // Example: A owes B 100. A Balance = -100. B Balance = +100.
            // A pays B 100 via Settlement.
            // A Balance should become 0. B Balance should become 0.
            // So Settlement Amount is added to Payer's Balance and subtracted from Payee's Balance.
            
            var settlementPayerAdjustment = settlements
                .GroupBy(s => s.PayerUserId)
                .ToDictionary(g => g.Key, g => g.Sum(s => s.Amount * (s.ExchangeRate ?? 1m)));

            var settlementPayeeAdjustment = settlements
                .GroupBy(s => s.PayeeUserId)
                .ToDictionary(g => g.Key, g => g.Sum(s => s.Amount * (s.ExchangeRate ?? 1m)));

            var paidDict = totalPaid.ToDictionary(x => x.UserId, x => x.Total);
            var sharedDict = totalShared.ToDictionary(x => x.UserId, x => x.Total);
            
            var balances = members.Select(u => {
                var paid = paidDict.TryGetValue(u, out var p) ? p : 0m;
                var shared = sharedDict.TryGetValue(u, out var s) ? s : 0m;
                var expenseBalance = paid - shared;

                var sent = settlementPayerAdjustment.TryGetValue(u, out var st) ? st : 0m;
                var received = settlementPayeeAdjustment.TryGetValue(u, out var r) ? r : 0m;
                
                // Final Balance = (Expense Paid - Expense Share) + (Settlements Sent - Settlements Received)
                // Let's verify:
                // A owes B 100. A: -100, B: +100.
                // A sends 100 to B. A Sent = 100. B Received = 100.
                // A Final = -100 + (100 - 0) = 0. Correct.
                // B Final = 100 + (0 - 100) = 0. Correct.

                return new BalanceDto
                {
                    UserId = u,
                    TotalPaid = paid, // This is Expense Paid only
                    TotalShared = shared, // This is Expense Shared only
                    Balance = expenseBalance + sent - received
                };
            }).ToList();
            
            return balances;
        }
    }
}
