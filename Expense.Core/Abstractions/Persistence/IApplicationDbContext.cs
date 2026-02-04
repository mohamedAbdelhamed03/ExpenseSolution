using Expense.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Expense.Core.Abstractions.Persistence
{
    public interface IApplicationDbContext
    {
        DbSet<RefreshToken> RefreshTokens { get; set; }
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}