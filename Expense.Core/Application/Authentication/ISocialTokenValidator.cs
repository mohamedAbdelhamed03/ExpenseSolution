using Expense.Core.Domain.Enums;
using Expense.Core.DTOs.Auth;

namespace Expense.Core.Application.Authentication
{
    public interface ISocialTokenValidator
    {
        AuthProvider Provider { get; }
        Task<SocialUserDto?> ValidateTokenAsync(string token);
    }
}
