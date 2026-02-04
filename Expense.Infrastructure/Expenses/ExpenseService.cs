using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Expense.Core.Abstractions.Expenses;
using Expense.Core.Abstractions.Persistence;
using Expense.Core.Domain.Entities;
using Expense.Core.DTOs.Expenses;
using Expense.Core.Exceptions;
using Microsoft.EntityFrameworkCore;
using ExpenseEntity = Expense.Core.Domain.Entities.Expense;

namespace Expense.Infrastructure.Expenses
{
    public class ExpenseService : IExpenseService
    {
        private readonly IApplicationDbContext _context;
        public ExpenseService(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<ExpenseDto> CreateExpenseAsync(Guid groupId, string paidByUserId, CreateExpenseDto dto)
        {
            var isMember = await _context.GroupMembers.AnyAsync(m => m.GroupId == groupId && m.UserId == paidByUserId);
            if (!isMember) throw new BusinessException("Not a group member");
            if (dto.Amount <= 0) throw new BusinessException("Invalid amount");
            var splitUsers = dto.Splits.Select(s => s.UserId).Distinct().ToList();
            var allMembers = await _context.GroupMembers.Where(m => m.GroupId == groupId && splitUsers.Contains(m.UserId)).Select(m => m.UserId).ToListAsync();
            if (allMembers.Count != splitUsers.Count) throw new BusinessException("Split contains non-member");
            var totalSplit = dto.Splits.Sum(s => s.Amount);
            if (totalSplit != dto.Amount) throw new BusinessException("Split total mismatch");
            var expense = new ExpenseEntity
            {
                GroupId = groupId,
                PaidByUserId = paidByUserId,
                Amount = dto.Amount,
                Description = dto.Description,
                ExpenseDate = dto.ExpenseDate
            };
            _context.Expenses.Add(expense);
            foreach (var s in dto.Splits)
            {
                _context.ExpenseSplits.Add(new ExpenseSplit
                {
                    Expense = expense,
                    UserId = s.UserId,
                    Amount = s.Amount
                });
            }
            await _context.SaveChangesAsync();
            var result = new ExpenseDto
            {
                Id = expense.Id,
                GroupId = expense.GroupId,
                PaidByUserId = expense.PaidByUserId,
                Amount = expense.Amount,
                Description = expense.Description,
                ExpenseDate = expense.ExpenseDate,
                Splits = dto.Splits.ToList()
            };
            return result;
        }

        public async Task<IEnumerable<ExpenseDto>> GetGroupExpensesAsync(Guid groupId, string requesterUserId)
        {
            var isMember = await _context.GroupMembers.AnyAsync(m => m.GroupId == groupId && m.UserId == requesterUserId);
            if (!isMember) throw new BusinessException("Not a group member");
            var expenses = await _context.Expenses
                .Where(e => e.GroupId == groupId)
                .Include(e => e.Splits)
                .OrderByDescending(e => e.ExpenseDate)
                .Select(e => new ExpenseDto
                {
                    Id = e.Id,
                    GroupId = e.GroupId,
                    PaidByUserId = e.PaidByUserId,
                    Amount = e.Amount,
                    Description = e.Description,
                    ExpenseDate = e.ExpenseDate,
                    Splits = e.Splits.Select(s => new ExpenseSplitDto
                    {
                        UserId = s.UserId,
                        Amount = s.Amount
                    })
                })
                .ToListAsync();
            return expenses;
        }
    }
}
