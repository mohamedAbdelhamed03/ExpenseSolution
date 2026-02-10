using Expense.Core.Application.Persistence;
using Expense.Core.Domain.Entities;
using Expense.Core.DTOs.Personal;
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

        public PersonalExpenseService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<PersonalExpenseDto> CreateAsync(Guid userId, CreatePersonalExpenseDto dto, CancellationToken cancellationToken = default)
        {
            var entity = new PersonalExpense
            {
                Id = Guid.NewGuid(),
                UserId = userId.ToString(),
                Amount = dto.Amount,
                Currency = dto.Currency,
                Date = dto.Date,
                Description = dto.Description,
                CreatedAt = DateTime.UtcNow
            };

            _unitOfWork.PersonalExpenses.Add(entity);
            await _unitOfWork.SaveAsync(cancellationToken);

            return MapToDto(entity);
        }

        public async Task<IEnumerable<PersonalExpenseDto>> GetUserExpensesAsync(Guid userId, int page, int pageSize, CancellationToken cancellationToken = default)
        {
            var entities = await _unitOfWork.PersonalExpenses.GetUserPersonalExpensesAsync(userId.ToString(), page, pageSize, cancellationToken);
            return entities.Select(MapToDto);
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
                CreatedAt = entity.CreatedAt
            };
        }
    }
}
