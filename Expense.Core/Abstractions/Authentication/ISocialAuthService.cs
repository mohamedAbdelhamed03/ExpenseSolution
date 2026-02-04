using Expense.Core.DTOs.Auth;

namespace Expense.Core.Abstractions.Authentication
{
    public interface ISocialAuthService
    {
        Task<LoginResponseDto> SocialLoginAsync(SocialLoginDto socialLoginDto);
    }
}
