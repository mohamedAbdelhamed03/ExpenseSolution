using Expense.Core.DTOs.Auth;

namespace Expense.Core.Application.Authentication
{
    public interface ISocialAuthService
    {
        Task<LoginResponseDto> SocialLoginAsync(SocialLoginDto socialLoginDto);
    }
}
