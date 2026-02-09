using Expense.Core.Abstractions.Persistence;
using Expense.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Expense.Core.DTOs.Insights;

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

        public async Task<IEnumerable<CategoryStatistics>> GetInsightsByCategoryAsync(Guid groupId, DateTime startDate, DateTime endDate)
        {
            return await dbSet
                .AsNoTracking()
                .Where(e => e.GroupId == groupId && e.ExpenseDate >= startDate && e.ExpenseDate <= endDate)
                .GroupBy(e => new { e.CategoryId, e.Currency })
                .Select(g => new CategoryStatistics
                {
                    CategoryId = g.Key.CategoryId,
                    Currency = g.Key.Currency,
                    TotalAmount = g.Sum(e => e.Amount * e.ExchangeRate) ?? 0m
                })
                .ToListAsync();
        }
    }
}
