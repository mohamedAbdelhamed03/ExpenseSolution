using Expense.Core.Application.Persistence;
using Expense.Core.Domain.Entities;
using Expense.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Expense.Infrastructure.Repositories
{
    public class PersonalExpenseRepository : Repository<PersonalExpense>, IPersonalExpenseRepository
    {
        public PersonalExpenseRepository(ApplicationDbContext db) : base(db)
        {
        }

        public async Task<IEnumerable<PersonalExpense>> GetUserPersonalExpensesAsync(string userId, int page, int pageSize, CancellationToken cancellationToken = default)
        {
            return await _db.PersonalExpenses
                .AsNoTracking()
                .Where(e => e.UserId == userId)
                .OrderByDescending(e => e.Date)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);
        }
    }
}
