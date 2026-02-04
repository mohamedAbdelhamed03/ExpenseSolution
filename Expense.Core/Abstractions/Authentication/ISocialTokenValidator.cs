using Expense.Core.Domain.IdentityEntities;
using Expense.Core.DTOs.Auth;

namespace Expense.Core.Abstractions.Authentication
{
    public interface ISocialTokenValidator
    {
        AuthProvider Provider { get; }
        Task<SocialUserDto?> ValidateTokenAsync(string token);
    }
}
