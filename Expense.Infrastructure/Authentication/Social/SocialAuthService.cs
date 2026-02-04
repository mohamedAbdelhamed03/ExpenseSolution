using System.Security.Claims;
using Expense.Core.Abstractions.Authentication;
using Expense.Core.Abstractions.Persistence;
using Expense.Core.Domain.Entities;
using Expense.Core.Domain.IdentityEntities;
using Expense.Core.DTOs.Auth;
using Expense.Core.Common.Exceptions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Http;
using System.Security.Cryptography;
using System.Text;

namespace Expense.Infrastructure.Authentication.Social
{
    public class SocialAuthService : ISocialAuthService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IJwtService _jwtService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IEnumerable<ISocialTokenValidator> _validators;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public SocialAuthService(
            UserManager<ApplicationUser> userManager,
            IJwtService jwtService,
            IUnitOfWork unitOfWork,
            IEnumerable<ISocialTokenValidator> validators,
            IHttpContextAccessor httpContextAccessor)
        {
            _userManager = userManager;
            _jwtService = jwtService;
            _unitOfWork = unitOfWork;
            _validators = validators;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<LoginResponseDto> SocialLoginAsync(SocialLoginDto socialLoginDto)
        {
            var validator = _validators.FirstOrDefault(v => v.Provider == socialLoginDto.Provider);
            if (validator == null)
            {
                return new LoginResponseDto { Success = false, Message = "Unsupported provider" };
            }

            var socialUser = await validator.ValidateTokenAsync(socialLoginDto.Token);
            if (socialUser == null)
            {
                return new LoginResponseDto { Success = false, Message = "Invalid token" };
            }

            if (string.IsNullOrEmpty(socialUser.Email))
            {
                // Requirement: Handle Facebook accounts without email. Reject with clear error.
                return new LoginResponseDto { Success = false, Message = "Email is required for authentication" };
            }

            // Check if user exists by Email
            var user = await _userManager.FindByEmailAsync(socialUser.Email);
            if (user == null)
            {
                // Auto-register
                user = new ApplicationUser
                {
                    Email = socialUser.Email,
                    UserName = socialUser.Email, // Use email as username
                    FirstName = socialUser.FirstName,
                    LastName = socialUser.LastName,
                    ProfilePicture = socialUser.PictureUrl,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    Provider = socialLoginDto.Provider,
                    EmailConfirmed = true // Trusted provider
                };

                if (socialLoginDto.Provider == AuthProvider.Google) user.GoogleId = socialUser.Id;
                else if (socialLoginDto.Provider == AuthProvider.Facebook) user.FacebookId = socialUser.Id;

                var result = await _userManager.CreateAsync(user);
                if (!result.Succeeded)
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    return new LoginResponseDto { Success = false, Message = $"Registration failed: {errors}" };
                }

                await _userManager.AddToRoleAsync(user, "User");
            }
            else
            {
                // User exists, link account if needed
                bool changed = false;
                if (socialLoginDto.Provider == AuthProvider.Google && user.GoogleId != socialUser.Id)
                {
                    user.GoogleId = socialUser.Id;
                    changed = true;
                }
                else if (socialLoginDto.Provider == AuthProvider.Facebook && user.FacebookId != socialUser.Id)
                {
                    user.FacebookId = socialUser.Id;
                    changed = true;
                }
                
                // Update profile picture if missing
                if (string.IsNullOrEmpty(user.ProfilePicture) && !string.IsNullOrEmpty(socialUser.PictureUrl))
                {
                    user.ProfilePicture = socialUser.PictureUrl;
                    changed = true;
                }

                if (changed)
                {
                    await _userManager.UpdateAsync(user);
                }
            }

            if (!user.IsActive)
            {
                return new LoginResponseDto { Success = false, Message = "Account is inactive" };
            }

            // Generate Tokens
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

            var ipAddress = _httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString() ?? "Unknown";
            var refreshTokenEntity = new RefreshToken
            {
                Token = hashedRefreshToken,
                UserId = user.Id.ToString(),
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                CreatedByIp = ipAddress
            };

            await _unitOfWork.Repository<RefreshToken>().Add(refreshTokenEntity);
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

        private string HashToken(string token)
        {
            using var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(token));
            return Convert.ToBase64String(bytes);
        }
    }
}
