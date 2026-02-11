using Expense.Core.Application.Persistence;
using Expense.Core.Common.Exceptions;
using Expense.Core.Domain.Entities;
using Expense.Core.DTOs.Personal;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Expense.Core.Application.Services
{
    public class PersonalExpenseService : IPersonalExpenseService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IValidator<CreatePersonalExpenseDto> _createValidator;
        private readonly IValidator<UpdatePersonalExpenseDto> _updateValidator;
        private readonly IValidator<UpdatePersonalExpensePatchDto> _patchValidator;

        public PersonalExpenseService(
            IUnitOfWork unitOfWork,
            IValidator<CreatePersonalExpenseDto> createValidator,
            IValidator<UpdatePersonalExpenseDto> updateValidator,
            IValidator<UpdatePersonalExpensePatchDto> patchValidator)
        {
            _unitOfWork = unitOfWork;
            _createValidator = createValidator;
            _updateValidator = updateValidator;
            _patchValidator = patchValidator;
        }

        public async Task<PersonalExpenseDto> CreateAsync(Guid userId, CreatePersonalExpenseDto dto, CancellationToken cancellationToken = default)
        {
            await _createValidator.ValidateAndThrowAsync(dto, cancellationToken);

            var entity = new PersonalExpense
            {
                Id = Guid.NewGuid(),
                UserId = userId.ToString(),
                Amount = dto.Amount,
                Currency = dto.Currency,
                Date = dto.Date,
                Description = dto.Description,
                CategoryId = dto.CategoryId,
                CreatedAt = DateTime.UtcNow
            };

            _unitOfWork.PersonalExpenses.Add(entity);
            await _unitOfWork.SaveAsync(cancellationToken);

            // We need to reload to get the category info if it was set
            if (dto.CategoryId.HasValue)
            {
                 entity = await _unitOfWork.PersonalExpenses.Get(x => x.Id == entity.Id, includes: e => e.Category);
            }

            return MapToDto(entity);
        }

        public async Task<PersonalExpenseDto> GetByIdAsync(Guid userId, Guid expenseId, CancellationToken cancellationToken = default)
        {
            var entity = await _unitOfWork.PersonalExpenses.Get(x => x.Id == expenseId, noTracking: true, includes: e => e.Category);
            if (entity == null)
            {
                throw new NotFoundException("PersonalExpense.NotFound");
            }

            if (entity.UserId != userId.ToString())
            {
                throw new AccessDeniedException("You do not have permission to access this expense.");
            }

            return MapToDto(entity);
        }

        public async Task<IEnumerable<PersonalExpenseDto>> GetUserExpensesAsync(Guid userId, int page, int pageSize, CancellationToken cancellationToken = default)
        {
            var entities = await _unitOfWork.PersonalExpenses.GetUserPersonalExpensesAsync(userId.ToString(), page, pageSize, cancellationToken);
            return entities.Select(MapToDto);
        }

        public async Task<PersonalExpenseDto> UpdateAsync(Guid userId, Guid expenseId, UpdatePersonalExpenseDto dto, CancellationToken cancellationToken = default)
        {
            await _updateValidator.ValidateAndThrowAsync(dto, cancellationToken);

            var entity = await _unitOfWork.PersonalExpenses.Get(x => x.Id == expenseId, includes: e => e.Category);
            if (entity == null)
            {
                throw new NotFoundException("PersonalExpense.NotFound");
            }

            if (entity.UserId != userId.ToString())
            {
                throw new AccessDeniedException("You do not have permission to modify this expense.");
            }

            entity.Amount = dto.Amount;
            entity.Currency = dto.Currency;
            entity.Date = dto.Date;
            entity.Description = dto.Description;
            entity.CategoryId = dto.CategoryId;

            _unitOfWork.PersonalExpenses.Update(entity);
            await _unitOfWork.SaveAsync(cancellationToken);

            // Reload if CategoryId changed to get new category details
            if (entity.CategoryId.HasValue)
            {
                 entity = await _unitOfWork.PersonalExpenses.Get(x => x.Id == entity.Id, includes: e => e.Category);
            }

            return MapToDto(entity);
        }

        public async Task<PersonalExpenseDto> UpdatePatchAsync(Guid userId, Guid expenseId, UpdatePersonalExpensePatchDto dto, CancellationToken cancellationToken = default)
        {
            await _patchValidator.ValidateAndThrowAsync(dto, cancellationToken);

            var entity = await _unitOfWork.PersonalExpenses.Get(x => x.Id == expenseId, includes: e => e.Category);
            if (entity == null)
            {
                throw new NotFoundException("PersonalExpense.NotFound");
            }

            if (entity.UserId != userId.ToString())
            {
                throw new AccessDeniedException("You do not have permission to modify this expense.");
            }

            if (dto.Amount.HasValue) entity.Amount = dto.Amount.Value;
            if (dto.Currency != null) entity.Currency = dto.Currency;
            if (dto.Date.HasValue) entity.Date = dto.Date.Value;
            if (dto.Description != null) entity.Description = dto.Description;
            if (dto.CategoryId.HasValue) entity.CategoryId = dto.CategoryId.Value;

            _unitOfWork.PersonalExpenses.Update(entity);
            await _unitOfWork.SaveAsync(cancellationToken);
            
            // Reload if CategoryId changed
            if (dto.CategoryId.HasValue)
            {
                 entity = await _unitOfWork.PersonalExpenses.Get(x => x.Id == entity.Id, includes: e => e.Category);
            }

            return MapToDto(entity);
        }

        public async Task DeleteAsync(Guid userId, Guid expenseId, CancellationToken cancellationToken = default)
        {
            var entity = await _unitOfWork.PersonalExpenses.Get(x => x.Id == expenseId);
            if (entity == null)
            {
                throw new NotFoundException("PersonalExpense.NotFound");
            }

            if (entity.UserId != userId.ToString())
            {
                throw new AccessDeniedException("You do not have permission to delete this expense.");
            }

            _unitOfWork.PersonalExpenses.Remove(entity);
            await _unitOfWork.SaveAsync(cancellationToken);
        }

        private static PersonalExpenseDto MapToDto(PersonalExpense entity)
        {
            return new PersonalExpenseDto
            {
                Id = entity.Id,
                UserId = entity.UserId,
                Amount = entity.Amount,
                Currency = entity.Currency,
                Date = entity.Date,
                Description = entity.Description,
                CategoryId = entity.CategoryId,
                CategoryName = entity.Category?.Name,
                CategoryIcon = entity.Category?.Icon,
                CreatedAt = entity.CreatedAt
            };
        }
    }
}
