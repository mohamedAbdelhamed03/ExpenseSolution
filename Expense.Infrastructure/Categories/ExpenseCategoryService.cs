using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Expense.Core.Abstractions.ActivityLogs;
using Expense.Core.Abstractions.Categories;
using Expense.Core.Abstractions.Persistence;
using Expense.Core.Common.Exceptions;
using Expense.Core.Domain.Entities;
using Expense.Core.Domain.Enums;
using Expense.Core.DTOs.Categories;
using FluentValidation;

namespace Expense.Infrastructure.Categories
{
    public class ExpenseCategoryService : IExpenseCategoryService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IActivityLogService _activityLogService;
        private readonly IValidator<CreateExpenseCategoryDto> _createValidator;
        private readonly IValidator<UpdateExpenseCategoryDto> _updateValidator;

        public ExpenseCategoryService(
            IUnitOfWork unitOfWork,
            IActivityLogService activityLogService,
            IValidator<CreateExpenseCategoryDto> createValidator,
            IValidator<UpdateExpenseCategoryDto> updateValidator)
        {
            _unitOfWork = unitOfWork;
            _activityLogService = activityLogService;
            _createValidator = createValidator;
            _updateValidator = updateValidator;
        }

        public async Task<ExpenseCategoryDto> CreateCategoryAsync(Guid groupId, string userId, CreateExpenseCategoryDto dto, CancellationToken cancellationToken)
        {
            await _createValidator.ValidateAndThrowAsync(dto, cancellationToken);

            // 1. Check if user is member of group
            var isMember = await _unitOfWork.Groups.IsMemberAsync(groupId, userId, cancellationToken);
            if (!isMember)
            {
                throw new GroupAccessDeniedException("Group_NotMember");
            }

            // 2. Check if user is Admin (optional, enforcing for structure as per previous tasks)
            var member = await _unitOfWork.Groups.GetMemberAsync(groupId, userId, cancellationToken);
            if (member?.Role != GroupRole.Admin)
            {
                throw new GroupAccessDeniedException("Group_AdminRequired");
            }

            // 3. Check uniqueness
            var existing = await _unitOfWork.Categories.GetByNameAsync(groupId, dto.Name, cancellationToken);
            if (existing != null)
            {
                throw new BusinessException("Category_AlreadyExists");
            }

            // 4. Create
            var category = new ExpenseCategory
            {
                Id = Guid.NewGuid(),
                GroupId = groupId,
                Name = dto.Name,
                Description = dto.Description,
                IsSystem = false,
                CreatedAt = DateTime.UtcNow
            };

            _unitOfWork.Categories.Add(category);

            await _activityLogService.LogActivityAsync(groupId, userId, ActivityType.Created, EntityType.Category, category.Id.ToString(), $"Category '{category.Name}' created", cancellationToken);

            await _unitOfWork.SaveAsync(cancellationToken);

            return MapToDto(category);
        }

        public async Task<IEnumerable<ExpenseCategoryDto>> GetCategoriesAsync(Guid groupId, string userId, CancellationToken cancellationToken)
        {
            var isMember = await _unitOfWork.Groups.IsMemberAsync(groupId, userId, cancellationToken);
            if (!isMember)
            {
                throw new GroupAccessDeniedException("Group_NotMember");
            }

            var categories = await _unitOfWork.Categories.GetCategoriesForGroupAsync(groupId, cancellationToken);
            return categories.Select(MapToDto);
        }

        public async Task<ExpenseCategoryDto?> UpdateCategoryAsync(Guid categoryId, string userId, UpdateExpenseCategoryDto dto, CancellationToken cancellationToken)
        {
            await _updateValidator.ValidateAndThrowAsync(dto, cancellationToken);

            var category = await _unitOfWork.Categories.Get(c => c.Id == categoryId);
            if (category == null)
            {
                throw new NotFoundException("Category_NotFound");
            }

            var member = await _unitOfWork.Groups.GetMemberAsync(category.GroupId, userId, cancellationToken);
            if (member == null)
            {
                 throw new GroupAccessDeniedException("Group_NotMember");
            }
            
            if (member.Role != GroupRole.Admin)
            {
                throw new GroupAccessDeniedException("Group_AdminRequired");
            }

            if (category.Name != dto.Name)
            {
                var existing = await _unitOfWork.Categories.GetByNameAsync(category.GroupId, dto.Name, cancellationToken);
                if (existing != null)
                {
                    throw new BusinessException("Category_AlreadyExists");
                }
            }

            category.Name = dto.Name;
            category.Description = dto.Description;

            _unitOfWork.Categories.Update(category);

            await _activityLogService.LogActivityAsync(category.GroupId, userId, ActivityType.Updated, EntityType.Category, category.Id.ToString(), $"Category '{category.Name}' updated", cancellationToken);

            await _unitOfWork.SaveAsync(cancellationToken);

            return MapToDto(category);
        }

        public async Task<bool> DeleteCategoryAsync(Guid categoryId, string userId, CancellationToken cancellationToken)
        {
            var category = await _unitOfWork.Categories.Get(c => c.Id == categoryId);
            if (category == null)
            {
                throw new NotFoundException("Category_NotFound");
            }

            var member = await _unitOfWork.Groups.GetMemberAsync(category.GroupId, userId, cancellationToken);
            if (member == null)
            {
                 throw new GroupAccessDeniedException("Group_NotMember");
            }

            if (member.Role != GroupRole.Admin)
            {
                throw new GroupAccessDeniedException("Group_AdminRequired");
            }
            
            if (category.IsSystem)
            {
                throw new BusinessException("Category_SystemCannotBeDeleted");
            }

            _unitOfWork.Categories.Remove(category);

            await _activityLogService.LogActivityAsync(category.GroupId, userId, ActivityType.Deleted, EntityType.Category, category.Id.ToString(), $"Category '{category.Name}' deleted", cancellationToken);

            await _unitOfWork.SaveAsync(cancellationToken);

            return true;
        }

        private static ExpenseCategoryDto MapToDto(ExpenseCategory category)
        {
            return new ExpenseCategoryDto
            {
                Id = category.Id,
                GroupId = category.GroupId,
                Name = category.Name,
                Description = category.Description,
                IsSystem = category.IsSystem,
                CreatedAt = category.CreatedAt
            };
        }
    }
}