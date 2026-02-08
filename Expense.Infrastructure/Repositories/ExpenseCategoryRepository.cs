using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Expense.Core.Abstractions.Persistence;
using Expense.Core.Domain.Entities;
using Expense.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Expense.Infrastructure.Repositories
{
    public class ExpenseCategoryRepository : Repository<ExpenseCategory>, IExpenseCategoryRepository
    {
        private readonly ApplicationDbContext _db;

        public ExpenseCategoryRepository(ApplicationDbContext db) : base(db)
        {
            _db = db;
        }

        public async Task<IEnumerable<ExpenseCategory>> GetCategoriesForGroupAsync(Guid groupId, CancellationToken cancellationToken)
        {
            return await _db.ExpenseCategories
                .AsNoTracking()
                .Where(c => c.GroupId == groupId)
                .OrderBy(c => c.Name)
                .ToListAsync(cancellationToken);
        }

        public async Task<ExpenseCategory?> GetByNameAsync(Guid groupId, string name, CancellationToken cancellationToken)
        {
            return await _db.ExpenseCategories
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.GroupId == groupId && c.Name == name, cancellationToken);
        }
    }
}