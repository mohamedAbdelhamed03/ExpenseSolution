using Expense.Core.Abstractions.Persistence;
using Expense.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Expense.Infrastructure.Repositories
{
    public class ExpenseRepository : Repository<Expense.Core.Domain.Entities.Expense>, IExpenseRepository
    {
        public ExpenseRepository(ApplicationDbContext db) : base(db)
        {
        }

        public async Task<IEnumerable<Expense.Core.Domain.Entities.Expense>> GetExpensesByGroupAsync(Guid groupId)
        {
            return await dbSet
                .Include(e => e.Splits)
                .Where(e => e.GroupId == groupId)
                .ToListAsync();
        }
    }
}
