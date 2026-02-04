using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Expense.Core.Abstractions.Balances;
using Expense.Core.Abstractions.Persistence;
using Expense.Core.DTOs.Balances;
using Expense.Core.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Expense.Infrastructure.Balances
{
    public class BalanceService : IBalanceService
    {
        private readonly IApplicationDbContext _context;
        public BalanceService(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<BalanceDto>> GetGroupBalancesAsync(Guid groupId, string requesterUserId)
        {
            var isMember = await _context.GroupMembers.AnyAsync(m => m.GroupId == groupId && m.UserId == requesterUserId);
            if (!isMember) throw new BusinessException("Not a group member");
            var members = await _context.GroupMembers.Where(m => m.GroupId == groupId).Select(m => m.UserId).ToListAsync();
            var totalPaid = await _context.Expenses
                .Where(e => e.GroupId == groupId)
                .GroupBy(e => e.PaidByUserId)
                .Select(g => new { UserId = g.Key, Total = g.Sum(x => x.Amount) })
                .ToListAsync();
            var totalShared = await _context.ExpenseSplits
                .Where(s => _context.Expenses.Where(e => e.GroupId == groupId).Select(e => e.Id).Contains(s.ExpenseId))
                .GroupBy(s => s.UserId)
                .Select(g => new { UserId = g.Key, Total = g.Sum(x => x.Amount) })
                .ToListAsync();
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
