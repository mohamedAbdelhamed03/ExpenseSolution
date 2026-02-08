using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Expense.Core.Abstractions.ActivityLogs;
using Expense.Core.Abstractions.Expenses;
using Expense.Core.Abstractions.Persistence;
using Expense.Core.Common.Exceptions;
using Expense.Core.Domain.Entities;
using Expense.Core.Domain.Enums;
using Expense.Core.DTOs.Expenses;
using FluentValidation;
using ExpenseEntity = Expense.Core.Domain.Entities.Expense;

namespace Expense.Infrastructure.Expenses
{
    public class ExpenseService : IExpenseService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IActivityLogService _activityLogService;
        private readonly IValidator<CreateExpenseDto> _createValidator;
        private readonly IValidator<UpdateExpenseDto> _updateValidator;
        private readonly IValidator<UpdateExpensePatchDto> _patchValidator;
        
        public ExpenseService(
            IUnitOfWork unitOfWork, 
            IActivityLogService activityLogService,
            IValidator<CreateExpenseDto> createValidator,
            IValidator<UpdateExpenseDto> updateValidator,
            IValidator<UpdateExpensePatchDto> patchValidator)
        {
            _unitOfWork = unitOfWork;
            _activityLogService = activityLogService;
            _createValidator = createValidator;
            _updateValidator = updateValidator;
            _patchValidator = patchValidator;
        }

        public async Task<ExpenseDto> CreateExpenseAsync(Guid groupId, string paidByUserId, CreateExpenseDto dto, CancellationToken cancellationToken = default)
        {
            await _createValidator.ValidateAndThrowAsync(dto, cancellationToken);

            // 1. Check if user is member
            var isMember = await _unitOfWork.Groups.IsMemberAsync(groupId, paidByUserId, cancellationToken);
            if (!isMember) throw new GroupAccessDeniedException("Group_NotMember");
            
            if (dto.Amount <= 0) throw new BusinessException("Expense_InvalidAmount");
            
            // 2. Validate Category
            if (dto.CategoryId.HasValue)
            {
                var category = await _unitOfWork.Categories.Get(c => c.Id == dto.CategoryId.Value);
                if (category == null || category.GroupId != groupId)
                {
                     throw new BusinessException("Category_Invalid");
                }
            }
            
            // 3. Handle Splits
            if (dto.Splits == null || !dto.Splits.Any())
            {
                // Equal split logic
                var groupMembers = await _unitOfWork.Repository<GroupMember>().GetAll(m => m.GroupId == groupId);
                var memberList = groupMembers.ToList();
                if (!memberList.Any()) throw new BusinessException("Group_NoMembers");
                
                var count = memberList.Count;
                var share = Math.Floor(dto.Amount / count * 100) / 100;
                var remainder = dto.Amount - (share * count);
                
                var splits = new List<ExpenseSplitDto>();
                
                // First pass: create splits with base share
                foreach (var member in memberList)
                {
                    splits.Add(new ExpenseSplitDto { UserId = member.UserId, Amount = share });
                }

                // Distribute remainder
                if (remainder > 0)
                {
                    var payerSplit = splits.FirstOrDefault(s => s.UserId == paidByUserId);
                    if (payerSplit != null)
                    {
                        payerSplit.Amount += remainder;
                    }
                    else
                    {
                        // Fallback to first member
                        splits[0].Amount += remainder;
                    }
                }
                
                dto.Splits = splits;
            }
            else
            {
                // Validate provided splits
                var splitUsers = dto.Splits.Select(s => s.UserId).Distinct().ToList();
                
                // Efficient member check
                var groupMembers = await _unitOfWork.Repository<GroupMember>()
                    .GetAll(m => m.GroupId == groupId && splitUsers.Contains(m.UserId));
                    
                if (groupMembers.Count() != splitUsers.Count) throw new BusinessException("Expense_SplitUserNotMember");
                
                var totalSplit = dto.Splits.Sum(s => s.Amount);
                if (totalSplit != dto.Amount) throw new BusinessException("Expense_SplitTotalMismatch");
            }
            
            // 4. Create Expense
            var expense = new ExpenseEntity
            {
                Id = Guid.NewGuid(),
                GroupId = groupId,
                PaidByUserId = paidByUserId,
                Amount = dto.Amount,
                Description = dto.Description,
                ExpenseDate = dto.ExpenseDate,
                CategoryId = dto.CategoryId,
                Currency = dto.Currency,
                ExchangeRate = dto.ExchangeRate,
                CreatedAt = DateTime.UtcNow
            };
            
            _unitOfWork.Expenses.Add(expense);
            
            // 5. Create Splits
            foreach (var s in dto.Splits)
            {
                _unitOfWork.Repository<ExpenseSplit>().Add(new ExpenseSplit
                {
                    Id = Guid.NewGuid(),
                    ExpenseId = expense.Id,
                    Expense = expense,
                    UserId = s.UserId,
                    Amount = s.Amount
                });
            }
            
            // 6. Log Activity
            await _activityLogService.LogActivityAsync(groupId, paidByUserId, ActivityType.Created, EntityType.Expense, expense.Id.ToString(), $"Amount: {dto.Amount}", cancellationToken);
            
            await _unitOfWork.SaveAsync(cancellationToken);
            
            return new ExpenseDto
            {
                Id = expense.Id,
                GroupId = expense.GroupId,
                PaidByUserId = expense.PaidByUserId,
                Amount = expense.Amount,
                Currency = expense.Currency,
                ExchangeRate = expense.ExchangeRate,
                Description = expense.Description,
                ExpenseDate = expense.ExpenseDate,
                CategoryId = expense.CategoryId,
                Splits = dto.Splits.ToList()
            };
        }

        public async Task<IEnumerable<ExpenseDto>> GetGroupExpensesAsync(Guid groupId, string requesterUserId, CancellationToken cancellationToken = default)
        {
             var isMember = await _unitOfWork.Groups.IsMemberAsync(groupId, requesterUserId, cancellationToken);
             if (!isMember) throw new GroupAccessDeniedException("Group_NotMember");
            
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
                    Currency = e.Currency,
                    ExchangeRate = e.ExchangeRate,
                    Description = e.Description,
                    ExpenseDate = e.ExpenseDate,
                    CategoryId = e.CategoryId,
                    Splits = e.Splits.Select(s => new ExpenseSplitDto
                    {
                        UserId = s.UserId,
                        Amount = s.Amount
                    }).ToList()
                });
        }
        public async Task<ExpenseDto?> GetExpenseAsync(Guid groupId, Guid expenseId, string requesterUserId, CancellationToken cancellationToken = default)
        {
            var isMember = await _unitOfWork.Groups.IsMemberAsync(groupId, requesterUserId, cancellationToken);
            if (!isMember) throw new GroupAccessDeniedException("Group_NotMember");

            var expense = await _unitOfWork.Expenses.Get(e => e.Id == expenseId, false, e => e.Splits);
            if (expense == null) return null;
            if (expense.GroupId != groupId) throw new BusinessException("Expense_GroupMismatch");

            return new ExpenseDto
            {
                Id = expense.Id,
                GroupId = expense.GroupId,
                PaidByUserId = expense.PaidByUserId,
                Amount = expense.Amount,
                Currency = expense.Currency,
                ExchangeRate = expense.ExchangeRate,
                Description = expense.Description,
                ExpenseDate = expense.ExpenseDate,
                CategoryId = expense.CategoryId,
                Splits = expense.Splits.Select(s => new ExpenseSplitDto
                {
                    UserId = s.UserId,
                    Amount = s.Amount
                }).ToList()
            };
        }

        public async Task<ExpenseDto> UpdateExpenseAsync(Guid groupId, Guid expenseId, string userId, UpdateExpenseDto dto, CancellationToken cancellationToken = default)
        {
            await _updateValidator.ValidateAndThrowAsync(dto, cancellationToken);

            var isMember = await _unitOfWork.Groups.IsMemberAsync(groupId, userId, cancellationToken);
            if (!isMember) throw new GroupAccessDeniedException("Group_NotMember");

            var expense = await _unitOfWork.Expenses.Get(e => e.Id == expenseId, false, e => e.Splits);
            if (expense == null) throw new NotFoundException("Expense.NotFound");
            if (expense.GroupId != groupId) throw new BusinessException("Expense_GroupMismatch");

            // ... (rest of logic)

            // Permission check: Member + (Payer or Admin)
            var member = await _unitOfWork.Groups.GetMemberAsync(expense.GroupId, userId, cancellationToken);
            if (member == null) throw new GroupAccessDeniedException("Group_NotMember");

            if (expense.PaidByUserId != userId && member.Role != GroupRole.Admin)
            {
                 throw new GroupAccessDeniedException("Expense_Modify_Denied");
            }

            if (dto.Amount <= 0) throw new BusinessException("Expense_InvalidAmount");

            // Validate Category
            if (dto.CategoryId.HasValue)
            {
                if (expense.CategoryId != dto.CategoryId)
                {
                    var category = await _unitOfWork.Categories.Get(c => c.Id == dto.CategoryId.Value);
                    if (category == null || category.GroupId != expense.GroupId)
                    {
                         throw new BusinessException("Category_Invalid");
                    }
                }
            }

            // Handle Splits Logic (Re-calculate)
            List<ExpenseSplitDto> newSplits;
            if (dto.Splits == null || !dto.Splits.Any())
            {
                // Equal split logic
                var groupMembers = await _unitOfWork.Repository<GroupMember>().GetAll(m => m.GroupId == expense.GroupId);
                var memberList = groupMembers.ToList();
                
                var count = memberList.Count;
                var share = Math.Floor(dto.Amount / count * 100) / 100;
                var remainder = dto.Amount - (share * count);
                
                newSplits = new List<ExpenseSplitDto>();
                foreach (var m in memberList)
                {
                    newSplits.Add(new ExpenseSplitDto { UserId = m.UserId, Amount = share });
                }

                if (remainder > 0)
                {
                    // Add remainder to payer if possible, else first member
                    // Note: Payer might not be in splits? (Unlikely in equal split of all members)
                    // If paidByUserId is not changing, use expense.PaidByUserId
                    var payerId = expense.PaidByUserId; 
                    var payerSplit = newSplits.FirstOrDefault(s => s.UserId == payerId);
                    if (payerSplit != null)
                    {
                        payerSplit.Amount += remainder;
                    }
                    else
                    {
                        newSplits[0].Amount += remainder;
                    }
                }
            }
            else
            {
                var splitUsers = dto.Splits.Select(s => s.UserId).Distinct().ToList();
                var groupMembers = await _unitOfWork.Repository<GroupMember>()
                    .GetAll(m => m.GroupId == expense.GroupId && splitUsers.Contains(m.UserId));
                    
                if (groupMembers.Count() != splitUsers.Count) throw new BusinessException("Expense_SplitUserNotMember");
                
                var totalSplit = dto.Splits.Sum(s => s.Amount);
                if (totalSplit != dto.Amount) throw new BusinessException("Expense_SplitTotalMismatch");
                
                newSplits = dto.Splits.ToList();
            }

            // Update Expense
            expense.Amount = dto.Amount;
            expense.Description = dto.Description;
            expense.ExpenseDate = dto.ExpenseDate;
            expense.CategoryId = dto.CategoryId;
            expense.Currency = dto.Currency;
            expense.ExchangeRate = dto.ExchangeRate;

            _unitOfWork.Expenses.Update(expense);

            // Replace Splits
            _unitOfWork.Repository<ExpenseSplit>().RemoveRange(expense.Splits);
            foreach (var s in newSplits)
            {
                _unitOfWork.Repository<ExpenseSplit>().Add(new ExpenseSplit
                {
                    Id = Guid.NewGuid(),
                    ExpenseId = expense.Id,
                    Expense = expense,
                    UserId = s.UserId,
                    Amount = s.Amount
                });
            }

            await _activityLogService.LogActivityAsync(expense.GroupId, userId, ActivityType.Updated, EntityType.Expense, expense.Id.ToString(), $"Amount: {dto.Amount}", cancellationToken);
            await _unitOfWork.SaveAsync(cancellationToken);

            return new ExpenseDto
            {
                Id = expense.Id,
                GroupId = expense.GroupId,
                PaidByUserId = expense.PaidByUserId,
                Amount = expense.Amount,
                Currency = expense.Currency,
                ExchangeRate = expense.ExchangeRate,
                Description = expense.Description,
                ExpenseDate = expense.ExpenseDate,
                CategoryId = expense.CategoryId,
                Splits = newSplits
            };
        }

        public async Task<ExpenseDto> UpdateExpensePartialAsync(Guid groupId, Guid expenseId, string userId, UpdateExpensePatchDto dto, CancellationToken cancellationToken = default)
        {
            await _patchValidator.ValidateAndThrowAsync(dto, cancellationToken);

            var isMember = await _unitOfWork.Groups.IsMemberAsync(groupId, userId, cancellationToken);
            if (!isMember) throw new GroupAccessDeniedException("Group_NotMember");

            var expense = await _unitOfWork.Expenses.Get(e => e.Id == expenseId, false, e => e.Splits);
            if (expense == null) throw new NotFoundException("Expense.NotFound");
            if (expense.GroupId != groupId) throw new BusinessException("Expense_GroupMismatch");

            // Permission check: Member + (Payer or Admin)
            var member = await _unitOfWork.Groups.GetMemberAsync(expense.GroupId, userId, cancellationToken);
            if (member == null) throw new GroupAccessDeniedException("Group_NotMember");

            if (expense.PaidByUserId != userId && member.Role != GroupRole.Admin)
            {
                 throw new GroupAccessDeniedException("Expense_Modify_Denied");
            }

            // Apply updates
            bool changed = false;
            var changes = new List<string>();

            if (dto.Description != null && dto.Description != expense.Description)
            {
                expense.Description = dto.Description;
                changes.Add($"Description changed");
                changed = true;
            }

            if (dto.ExpenseDate.HasValue && dto.ExpenseDate != expense.ExpenseDate)
            {
                expense.ExpenseDate = dto.ExpenseDate.Value;
                changes.Add($"Date changed to {dto.ExpenseDate}");
                changed = true;
            }

            if (dto.CategoryId.HasValue && dto.CategoryId != expense.CategoryId)
            {
                 var category = await _unitOfWork.Categories.Get(c => c.Id == dto.CategoryId.Value);
                 if (category == null || category.GroupId != expense.GroupId)
                 {
                      throw new BusinessException("Category_Invalid");
                 }
                 expense.CategoryId = dto.CategoryId;
                 changes.Add($"Category changed");
                 changed = true;
            }

            if (changed)
            {
                _unitOfWork.Expenses.Update(expense);
                
                await _activityLogService.LogActivityAsync(groupId, userId, ActivityType.Updated, EntityType.Expense, expense.Id.ToString(), $"Partial update: {string.Join(", ", changes)}", cancellationToken);
                
                await _unitOfWork.SaveAsync(cancellationToken);
            }

            return new ExpenseDto
            {
                Id = expense.Id,
                GroupId = expense.GroupId,
                PaidByUserId = expense.PaidByUserId,
                Amount = expense.Amount,
                Currency = expense.Currency,
                ExchangeRate = expense.ExchangeRate,
                Description = expense.Description,
                ExpenseDate = expense.ExpenseDate,
                CategoryId = expense.CategoryId,
                Splits = expense.Splits.Select(s => new ExpenseSplitDto
                {
                    UserId = s.UserId,
                    Amount = s.Amount
                }).ToList()
            };
        }

        public async Task<bool> DeleteExpenseAsync(Guid groupId, Guid expenseId, string userId, CancellationToken cancellationToken = default)
        {
            var isMember = await _unitOfWork.Groups.IsMemberAsync(groupId, userId, cancellationToken);
            if (!isMember) throw new GroupAccessDeniedException("Group_NotMember");

            var expense = await _unitOfWork.Expenses.Get(e => e.Id == expenseId, false, e => e.Splits);
            if (expense == null) throw new NotFoundException("Expense.NotFound");
            if (expense.GroupId != groupId) throw new BusinessException("Expense_GroupMismatch");

            if (expense.PaidByUserId != userId)
            {
                // Also allow admin? For now, only payer.
                // Or check if user is Admin of the group
                var member = await _unitOfWork.Repository<GroupMember>().Get(m => m.GroupId == groupId && m.UserId == userId);
                if (member?.Role != GroupRole.Admin)
                {
                    throw new BusinessException("Expense_Delete_NotAuthorized");
                }
            }

            _unitOfWork.Expenses.Remove(expense);
            await _activityLogService.LogActivityAsync(groupId, userId, ActivityType.Deleted, EntityType.Expense, expense.Id.ToString(), null, cancellationToken);
            
            await _unitOfWork.SaveAsync(cancellationToken);
            return true;
        }
    }
}
