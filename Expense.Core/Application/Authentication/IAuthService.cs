using System.Security.Claims;
using Expense.Core.DTOs.Auth;

namespace Expense.Core.Application.Authentication
{
    public interface IAuthService
    {
        Task<RegisterResponseDto> RegisterAsync(RegisterDto registerDto);
        Task<LoginResponseDto> LoginAsync(LoginDto loginDto);
        Task<RefreshTokenResponseDto> RefreshTokenAsync(RefreshTokenDto refreshTokenDto);
        Task<bool> RevokeTokenAsync(string userId);
        Task<bool> RevokeRefreshTokenAsync(string userId, string refreshToken);
        Task<bool> ChangePasswordAsync(string userId, ChangePasswordDto changePasswordDto);
        Task<bool> ResetPasswordAsync(ResetPasswordDto resetPasswordDto);
        Task<bool> ForgotPasswordAsync(ForgotPasswordDto forgotPasswordDto);
    }
}