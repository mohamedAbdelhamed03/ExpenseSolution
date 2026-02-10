using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Expense.Core.Domain.Entities;
using Expense.Infrastructure.Identity;
using Expense.Core.DTOs.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Http;
using Expense.Core.Application.Authentication;
using Expense.Core.Application.Persistence;

namespace Expense.Infrastructure.Authentication
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IJwtService _jwtService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AuthService(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IJwtService jwtService,
            IUnitOfWork unitOfWork,
            IHttpContextAccessor httpContextAccessor)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _jwtService = jwtService;
            _unitOfWork = unitOfWork;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<RegisterResponseDto> RegisterAsync(RegisterDto registerDto)
        {
            var existingUser = await _userManager.FindByEmailAsync(registerDto.Email);
            if (existingUser != null)
            {
                return new RegisterResponseDto { Success = false, Message = "Email already exists" };
            }

            var user = new ApplicationUser
            {
                Email = registerDto.Email,
                UserName = registerDto.Email,
                FirstName = registerDto.FirstName,
                LastName = registerDto.LastName,
                PhoneNumber = registerDto.PhoneNumber,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                TokenVersion = 1
            };

            var result = await _userManager.CreateAsync(user, registerDto.Password);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                return new RegisterResponseDto { Success = false, Message = errors };
            }

            // Assign default role
            await _userManager.AddToRoleAsync(user, "User");

            return new RegisterResponseDto 
            { 
                Success = true, 
                Message = "Registration successful", 
                UserId = user.Id.ToString() 
            };
        }

        public async Task<LoginResponseDto> LoginAsync(LoginDto loginDto)
        {
            var user = await _userManager.FindByEmailAsync(loginDto.Email);
            if (user == null || !user.IsActive)
            {
                return new LoginResponseDto { Success = false, Message = "Invalid credentials" };
            }

            var result = await _signInManager.CheckPasswordSignInAsync(user, loginDto.Password, false);
            if (!result.Succeeded)
            {
                return new LoginResponseDto { Success = false, Message = "Invalid credentials" };
            }

            var roles = await _userManager.GetRolesAsync(user);
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email!),
                new Claim(ClaimTypes.Name, user.UserName!),
                new Claim("TokenVersion", user.TokenVersion.ToString())
            };

            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var accessToken = _jwtService.GenerateAccessToken(claims);
            var refreshToken = _jwtService.GenerateRefreshToken();
            var hashedRefreshToken = HashToken(refreshToken);

            var ipAddress = GetIpAddress();
            var refreshTokenEntity = new RefreshToken
            {
                Token = hashedRefreshToken,
                UserId = user.Id.ToString(),
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                CreatedByIp = ipAddress
            };

            _unitOfWork.Repository<RefreshToken>().Add(refreshTokenEntity);
            await _unitOfWork.SaveAsync();

            return new LoginResponseDto
            {
                Success = true,
                Message = "Login successful",
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresAt = DateTime.UtcNow.AddHours(1),
                UserId = user.Id.ToString(),
                Email = user.Email,
                Roles = roles
            };
        }

        public async Task<RefreshTokenResponseDto> RefreshTokenAsync(RefreshTokenDto refreshTokenDto)
        {
            var principal = _jwtService.GetPrincipalFromExpiredToken(refreshTokenDto.AccessToken);
            var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var tokenVersion = int.Parse(principal.FindFirst("TokenVersion")?.Value ?? "1");

            if (string.IsNullOrEmpty(userId))
            {
                return new RefreshTokenResponseDto { Success = false, Message = "Invalid token" };
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null || !user.IsActive)
            {
                return new RefreshTokenResponseDto { Success = false, Message = "User not found" };
            }

            if (user.TokenVersion != tokenVersion)
            {
                return new RefreshTokenResponseDto { Success = false, Message = "Token revoked" };
            }

            var hashedIncomingToken = HashToken(refreshTokenDto.RefreshToken);
            var storedRefreshToken = await _unitOfWork.Repository<RefreshToken>()
                .Get(rt => rt.Token == hashedIncomingToken && rt.UserId == userId);

            if (storedRefreshToken == null || storedRefreshToken.IsRevoked || storedRefreshToken.ExpiresAt < DateTime.UtcNow)
            {
                return new RefreshTokenResponseDto { Success = false, Message = "Invalid refresh token" };
            }

            // Revoke old refresh token
            storedRefreshToken.IsRevoked = true;
            storedRefreshToken.RevokedAt = DateTime.UtcNow;
            storedRefreshToken.RevokedByIp = GetIpAddress();

            var roles = await _userManager.GetRolesAsync(user);
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email!),
                new Claim(ClaimTypes.Name, user.UserName!),
                new Claim("TokenVersion", user.TokenVersion.ToString())
            };

            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var newAccessToken = _jwtService.GenerateAccessToken(claims);
            var newRefreshToken = _jwtService.GenerateRefreshToken();
            var newHashedRefreshToken = HashToken(newRefreshToken);

            var newRefreshTokenEntity = new RefreshToken
            {
                Token = newHashedRefreshToken,
                UserId = user.Id.ToString(),
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                CreatedByIp = GetIpAddress(),
                ReplacedByToken = storedRefreshToken.Token
            };

            _unitOfWork.Repository<RefreshToken>().Add(newRefreshTokenEntity);
            await _unitOfWork.SaveAsync();

            return new RefreshTokenResponseDto
            {
                Success = true,
                Message = "Token refreshed successfully",
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken,
                ExpiresAt = DateTime.UtcNow.AddHours(1)
            };
        }

        public async Task<bool> RevokeTokenAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return false;
            }

            user.TokenVersion++;
            var result = await _userManager.UpdateAsync(user);
            
            if (result.Succeeded)
            {
                // Revoke all refresh tokens for this user
                var refreshTokens = await _unitOfWork.Repository<RefreshToken>()
                    .GetAll(rt => rt.UserId == userId && !rt.IsRevoked);

                foreach (var token in refreshTokens)
                {
                    token.IsRevoked = true;
                    token.RevokedAt = DateTime.UtcNow;
                    token.RevokedByIp = GetIpAddress();
                }

                await _unitOfWork.SaveAsync();
                return true;
            }

            return false;
        }

        public async Task<bool> RevokeRefreshTokenAsync(string userId, string refreshToken)
        {
            var hashedIncomingToken = HashToken(refreshToken);
            var storedToken = await _unitOfWork.Repository<RefreshToken>()
                .Get(rt => rt.Token == hashedIncomingToken && rt.UserId == userId);

            if (storedToken == null || storedToken.IsRevoked)
            {
                return false;
            }

            storedToken.IsRevoked = true;
            storedToken.RevokedAt = DateTime.UtcNow;
            storedToken.RevokedByIp = GetIpAddress();

            await _unitOfWork.SaveAsync();
            return true;
        }

        public async Task<bool> ChangePasswordAsync(string userId, ChangePasswordDto changePasswordDto)
        {
            if (changePasswordDto.NewPassword != changePasswordDto.ConfirmPassword)
            {
                return false;
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return false;
            }

            var result = await _userManager.ChangePasswordAsync(user, changePasswordDto.CurrentPassword, changePasswordDto.NewPassword);
            if (result.Succeeded)
            {
                // Increment token version to invalidate existing tokens
                user.TokenVersion++;
                await _userManager.UpdateAsync(user);
                return true;
            }

            return false;
        }

        public async Task<bool> ResetPasswordAsync(ResetPasswordDto resetPasswordDto)
        {
            if (resetPasswordDto.NewPassword != resetPasswordDto.ConfirmPassword)
            {
                return false;
            }

            var user = await _userManager.FindByEmailAsync(resetPasswordDto.Email);
            if (user == null)
            {
                return false;
            }

            var result = await _userManager.ResetPasswordAsync(user, resetPasswordDto.Token, resetPasswordDto.NewPassword);
            if (result.Succeeded)
            {
                // Increment token version to invalidate existing tokens
                user.TokenVersion++;
                await _userManager.UpdateAsync(user);
                return true;
            }

            return false;
        }

        public async Task<bool> ForgotPasswordAsync(ForgotPasswordDto forgotPasswordDto)
        {
            var user = await _userManager.FindByEmailAsync(forgotPasswordDto.Email);
            if (user == null || !user.IsActive)
            {
                // Don't reveal that the user doesn't exist or is not active
                return true;
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            // Here you would typically send an email with the token
            // For now, we'll just return success
            
            return true;
        }

        private string? GetIpAddress()
        {
            return _httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString();
        }

        private static string HashToken(string token)
        {
            using var sha256 = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(token);
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }
    }
}
