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
        Task<IEnumerable<PersonalExpenseDto>> GetUserExpensesAsync(Guid userId, int page, int pageSize, CancellationToken cancellationToken = default);
    }
}
