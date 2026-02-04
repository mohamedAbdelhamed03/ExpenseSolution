using Expense.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using ExpenseEntity = Expense.Core.Domain.Entities.Expense;

namespace Expense.Core.Abstractions.Persistence
{
    public interface IApplicationDbContext
    {
        DbSet<RefreshToken> RefreshTokens { get; set; }
        DbSet<Group> Groups { get; set; }
        DbSet<GroupMember> GroupMembers { get; set; }
        DbSet<ExpenseEntity> Expenses { get; set; }
        DbSet<ExpenseSplit> ExpenseSplits { get; set; }
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
