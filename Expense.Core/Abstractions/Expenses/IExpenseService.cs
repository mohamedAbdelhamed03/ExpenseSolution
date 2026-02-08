using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Expense.Core.DTOs.Expenses;

namespace Expense.Core.Abstractions.Expenses
{
    public interface IExpenseService
    {
        Task<ExpenseDto> CreateExpenseAsync(Guid groupId, string paidByUserId, CreateExpenseDto dto, CancellationToken cancellationToken = default);
        Task<IEnumerable<ExpenseDto>> GetGroupExpensesAsync(Guid groupId, string requesterUserId, CancellationToken cancellationToken = default);
        Task<ExpenseDto?> GetExpenseAsync(Guid groupId, Guid expenseId, string requesterUserId, CancellationToken cancellationToken = default);
        Task<ExpenseDto> UpdateExpenseAsync(Guid groupId, Guid expenseId, string userId, UpdateExpenseDto dto, CancellationToken cancellationToken = default);
        Task<ExpenseDto> UpdateExpensePartialAsync(Guid groupId, Guid expenseId, string userId, UpdateExpensePatchDto dto, CancellationToken cancellationToken = default);
        Task<bool> DeleteExpenseAsync(Guid groupId, Guid expenseId, string userId, CancellationToken cancellationToken = default);
    }
}
