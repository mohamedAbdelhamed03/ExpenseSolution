using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Expense.Core.Application.Debts;
using Expense.Core.Application.Persistence;
using Expense.Core.Common.Exceptions;
using Expense.Core.DTOs.Debts;

namespace Expense.Infrastructure.Debts
{
    public class DebtSimplificationService : IDebtSimplificationService
    {
        private readonly IUnitOfWork _unitOfWork;
        private const decimal Tolerance = 0.01m;

        public DebtSimplificationService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<IEnumerable<SimplifiedDebtGroupDto>> GetSimplifiedDebtsAsync(Guid groupId, string userId, CancellationToken cancellationToken)
        {
            // 1. Authorization: Check if user is a member
            var isMember = await _unitOfWork.Groups.IsMemberAsync(groupId, userId, cancellationToken);
            if (!isMember) throw new GroupAccessDeniedException("User is not a member of this group.");

            // 2. Fetch data (Expenses and Settlements)
            var expenses = await _unitOfWork.Balances.GetExpensesWithSplitsAsync(groupId, cancellationToken);
            var settlements = await _unitOfWork.Balances.GetSettlementsAsync(groupId, cancellationToken);
            var members = await _unitOfWork.Balances.GetMembersAsync(groupId, cancellationToken);
            var memberIds = new HashSet<string>(members.Select(m => m.UserId));

            // 3. Identify all currencies
            var currencies = expenses.Select(e => e.Currency)
                .Union(settlements.Select(s => s.Currency))
                .Distinct()
                .ToList();

            var result = new List<SimplifiedDebtGroupDto>();

            // 4. Process per currency
            foreach (var currency in currencies)
            {
                var currencyExpenses = expenses.Where(e => e.Currency == currency).ToList();
                var currencySettlements = settlements.Where(s => s.Currency == currency).ToList();

                // Calculate Net Balances
                var balances = new Dictionary<string, decimal>();

                foreach (var memberId in memberIds)
                {
                    balances[memberId] = 0m;
                }

                foreach (var expense in currencyExpenses)
                {
                    // Payer gets positive balance (is owed money)
                    if (balances.ContainsKey(expense.PaidByUserId))
                        balances[expense.PaidByUserId] += expense.Amount;

                    // Split users get negative balance (owe money)
                    foreach (var split in expense.Splits)
                    {
                        if (balances.ContainsKey(split.UserId))
                            balances[split.UserId] -= split.Amount;
                    }
                }

                foreach (var settlement in currencySettlements)
                {
                    // Payer gets positive balance (paid debt, so balance increases towards 0 or positive)
                    // Wait, logic check:
                    // If I owe 100 (Balance -100), and I pay 100. My balance should become 0. So +100.
                    // If I am owed 100 (Balance +100), and I receive 100. My balance should become 0. So -100.
                    if (balances.ContainsKey(settlement.PayerUserId))
                        balances[settlement.PayerUserId] += settlement.Amount;

                    if (balances.ContainsKey(settlement.PayeeUserId))
                        balances[settlement.PayeeUserId] -= settlement.Amount;
                }

                // Simplify
                var transfers = SimplifyDebts(balances, currency);
                if (transfers.Any())
                {
                    result.Add(new SimplifiedDebtGroupDto
                    {
                        Currency = currency,
                        Transfers = transfers
                    });
                }
            }

            return result;
        }

        private List<SimplifiedDebtDto> SimplifyDebts(Dictionary<string, decimal> balances, string currency)
        {
            var debtors = balances.Where(b => b.Value < -Tolerance)
                .Select(b => new BalanceNode { UserId = b.Key, Amount = b.Value })
                .OrderBy(b => b.Amount) // Most negative first (e.g. -100, -50)
                .ThenBy(b => b.UserId) // Deterministic tie-breaker
                .ToList();

            var creditors = balances.Where(b => b.Value > Tolerance)
                .Select(b => new BalanceNode { UserId = b.Key, Amount = b.Value })
                .OrderByDescending(b => b.Amount) // Most positive first (e.g. 100, 50)
                .ThenBy(b => b.UserId) // Deterministic tie-breaker
                .ToList();

            var transfers = new List<SimplifiedDebtDto>();
            int i = 0; // index for debtors
            int j = 0; // index for creditors

            while (i < debtors.Count && j < creditors.Count)
            {
                var debtor = debtors[i];
                var creditor = creditors[j];

                // Amount to transfer is min(abs(debtor), creditor)
                var amount = Math.Min(Math.Abs(debtor.Amount), creditor.Amount);

                // Create transfer
                transfers.Add(new SimplifiedDebtDto
                {
                    FromUserId = debtor.UserId,
                    ToUserId = creditor.UserId,
                    Amount = Math.Round(amount, 2), // Round to 2 decimal places for display
                    Currency = currency
                });

                // Adjust balances
                debtor.Amount += amount;
                creditor.Amount -= amount;

                // Move pointers if settled
                if (Math.Abs(debtor.Amount) < Tolerance) i++;
                if (creditor.Amount < Tolerance) j++;
            }

            return transfers;
        }

        private class BalanceNode
        {
            public string UserId { get; set; } = string.Empty;
            public decimal Amount { get; set; }
        }
    }
}
