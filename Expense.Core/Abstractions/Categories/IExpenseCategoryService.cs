using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Expense.Core.DTOs.Categories;

namespace Expense.Core.Abstractions.Categories
{
    public interface IExpenseCategoryService
    {
        Task<ExpenseCategoryDto> CreateCategoryAsync(Guid groupId, string userId, CreateExpenseCategoryDto dto, CancellationToken cancellationToken);
        Task<IEnumerable<ExpenseCategoryDto>> GetCategoriesAsync(Guid groupId, string userId, CancellationToken cancellationToken);
        Task<ExpenseCategoryDto?> UpdateCategoryAsync(Guid categoryId, string userId, UpdateExpenseCategoryDto dto, CancellationToken cancellationToken);
        Task<bool> DeleteCategoryAsync(Guid categoryId, string userId, CancellationToken cancellationToken);
    }
}