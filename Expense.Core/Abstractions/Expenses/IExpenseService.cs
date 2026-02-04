using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Expense.Core.DTOs.Expenses;

namespace Expense.Core.Abstractions.Expenses
{
    public interface IExpenseService
    {
        Task<ExpenseDto> CreateExpenseAsync(Guid groupId, string paidByUserId, CreateExpenseDto dto);
        Task<IEnumerable<ExpenseDto>> GetGroupExpensesAsync(Guid groupId, string requesterUserId);
    }
}
