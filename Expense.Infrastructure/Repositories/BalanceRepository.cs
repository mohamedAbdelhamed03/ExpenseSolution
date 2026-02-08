using Expense.Core.Abstractions.Persistence;
using Expense.Core.Domain.Entities;
using Expense.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Expense.Infrastructure.Repositories
{
    public class BalanceRepository : IBalanceRepository
    {
        private readonly ApplicationDbContext _db;

        public BalanceRepository(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<IEnumerable<GroupMember>> GetMembersAsync(Guid groupId, CancellationToken cancellationToken)
        {
            return await _db.GroupMembers
                .AsNoTracking()
                .Where(m => m.GroupId == groupId)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<Expense.Core.Domain.Entities.Expense>> GetExpensesWithSplitsAsync(Guid groupId, CancellationToken cancellationToken)
        {
            return await _db.Expenses
                .AsNoTracking()
                .Include(e => e.Splits)
                .Where(e => e.GroupId == groupId)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<Settlement>> GetSettlementsAsync(Guid groupId, CancellationToken cancellationToken)
        {
            return await _db.Settlements
                .AsNoTracking()
                .Where(s => s.GroupId == groupId)
                .ToListAsync(cancellationToken);
        }
    }
}
