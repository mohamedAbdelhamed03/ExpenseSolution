using Expense.Core.DTOs.Personal;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Expense.Core.Application.Services
{
    public interface IPersonalExpenseService
    {
        Task<PersonalExpenseDto> CreateAsync(Guid userId, CreatePersonalExpenseDto dto, CancellationToken cancellationToken = default);
        Task<PersonalExpenseDto> GetByIdAsync(Guid userId, Guid expenseId, CancellationToken cancellationToken = default);
        Task<IEnumerable<PersonalExpenseDto>> GetUserExpensesAsync(Guid userId, int page, int pageSize, CancellationToken cancellationToken = default);
        Task<PersonalExpenseDto> UpdateAsync(Guid userId, Guid expenseId, UpdatePersonalExpenseDto dto, CancellationToken cancellationToken = default);
        Task<PersonalExpenseDto> UpdatePatchAsync(Guid userId, Guid expenseId, UpdatePersonalExpensePatchDto dto, CancellationToken cancellationToken = default);
        Task DeleteAsync(Guid userId, Guid expenseId, CancellationToken cancellationToken = default);
    }
}
