using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Expense.Core.Abstractions.Expenses;
using Expense.Core.Abstractions.Persistence;
using Expense.Core.Domain.Entities;
using Expense.Core.DTOs.Expenses;
using Expense.Core.Common.Exceptions;
using ExpenseEntity = Expense.Core.Domain.Entities.Expense;

namespace Expense.Infrastructure.Expenses
{
    public class ExpenseService : IExpenseService
    {
        private readonly IUnitOfWork _unitOfWork;
        
        public ExpenseService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<ExpenseDto> CreateExpenseAsync(Guid groupId, string paidByUserId, CreateExpenseDto dto)
        {
            var isMember = await _unitOfWork.Repository<GroupMember>().Exists(m => m.GroupId == groupId && m.UserId == paidByUserId);
            if (!isMember) throw new BusinessException("Not a group member");
            
            if (dto.Amount <= 0) throw new BusinessException("Invalid amount");
            
            var splitUsers = dto.Splits.Select(s => s.UserId).Distinct().ToList();
            
            // Note: Efficiently checking if all split users are members.
            // Using GetAll with filter. 
            var groupMembers = await _unitOfWork.Repository<GroupMember>()
                .GetAll(m => m.GroupId == groupId && splitUsers.Contains(m.UserId));
                
            var memberIds = groupMembers.Select(m => m.UserId).ToList();
            if (memberIds.Count != splitUsers.Count) throw new BusinessException("Split contains non-member");
            
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
            
            _unitOfWork.Repository<ExpenseEntity>().Add(expense);
            
            foreach (var s in dto.Splits)
            {
                _unitOfWork.Repository<ExpenseSplit>().Add(new ExpenseSplit
                {
                    Expense = expense,
                    UserId = s.UserId,
                    Amount = s.Amount
                });
            }
            
            await _unitOfWork.SaveAsync();
            
            return new ExpenseDto
            {
                Id = expense.Id,
                GroupId = expense.GroupId,
                PaidByUserId = expense.PaidByUserId,
                Amount = expense.Amount,
                Description = expense.Description,
                ExpenseDate = expense.ExpenseDate,
                Splits = dto.Splits.ToList()
            };
        }

        public async Task<IEnumerable<ExpenseDto>> GetGroupExpensesAsync(Guid groupId, string requesterUserId)
        {
            var isMember = await _unitOfWork.Repository<GroupMember>().Exists(m => m.GroupId == groupId && m.UserId == requesterUserId);
            if (!isMember) throw new BusinessException("Not a group member");
            
            // Fetch all expenses with splits for the group
            var expenses = await _unitOfWork.Expenses.GetExpensesByGroupAsync(groupId);
            
            // Perform ordering and projection in memory
            return expenses
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
                });
        }
    }
}