using System.Security.Claims;
using Expense.Core.DTOs.Auth;
using Expense.Core.Application.Authentication;
using Expense.Core.DTOs.Shared;
using Expense.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Expense.Core.Domain.Enums;

namespace Expense.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ISocialAuthService _socialAuthService;

        public AuthController(IAuthService authService, ISocialAuthService socialAuthService)
        {
            _authService = authService;
            _socialAuthService = socialAuthService;
        }

        [HttpPost("google")]
        [AllowAnonymous]
        [SwaggerOperation(Summary = "Login with Google")]
        [SwaggerResponse(200, "Login successful", typeof(APIResponse<LoginResponseDto>))]
        [SwaggerResponse(400, "Login failed")]
        public async Task<ActionResult<APIResponse<LoginResponseDto>>> GoogleLogin([FromBody] SocialLoginDto loginDto)
        {
            if (loginDto.Provider != AuthProvider.Google)
                return BadRequest(APIResponse<LoginResponseDto>.ErrorResponse("Invalid provider"));

            var result = await _socialAuthService.SocialLoginAsync(loginDto);
            
            if (result.Success)
            {
                return Ok(APIResponse<LoginResponseDto>.SuccessResponse(result, result.Message));
            }

            return BadRequest(APIResponse<LoginResponseDto>.ErrorResponse(result.Message, result));
        }

        [HttpPost("facebook")]
        [AllowAnonymous]
        [SwaggerOperation(Summary = "Login with Facebook")]
        [SwaggerResponse(200, "Login successful", typeof(APIResponse<LoginResponseDto>))]
        [SwaggerResponse(400, "Login failed")]
        public async Task<ActionResult<APIResponse<LoginResponseDto>>> FacebookLogin([FromBody] SocialLoginDto loginDto)
        {
            if (loginDto.Provider != AuthProvider.Facebook)
                return BadRequest(APIResponse<LoginResponseDto>.ErrorResponse("Invalid provider"));

            var result = await _socialAuthService.SocialLoginAsync(loginDto);
            
            if (result.Success)
            {
                return Ok(APIResponse<LoginResponseDto>.SuccessResponse(result, result.Message));
            }

            return BadRequest(APIResponse<LoginResponseDto>.ErrorResponse(result.Message, result));
        }

        [HttpPost("register")]
        [AllowAnonymous]
        [SwaggerOperation(Summary = "Register a new user")]
        [SwaggerResponse(200, "Registration successful", typeof(APIResponse<RegisterResponseDto>))]
        [SwaggerResponse(400, "Registration failed")]
        public async Task<ActionResult<APIResponse<RegisterResponseDto>>> Register([FromBody] RegisterDto registerDto)
        {
            var result = await _authService.RegisterAsync(registerDto);
            
            if (result.Success)
            {
                return Ok(APIResponse<RegisterResponseDto>.SuccessResponse(result, result.Message));
            }

            return BadRequest(APIResponse<RegisterResponseDto>.ErrorResponse(result.Message, result));
        }

        [HttpPost("login")]
        [AllowAnonymous]
        [SwaggerOperation(Summary = "Login user")]
        [SwaggerResponse(200, "Login successful", typeof(APIResponse<LoginResponseDto>))]
        [SwaggerResponse(401, "Invalid credentials")]
        public async Task<ActionResult<APIResponse<LoginResponseDto>>> Login([FromBody] LoginDto loginDto)
        {
            var result = await _authService.LoginAsync(loginDto);
            
            if (result.Success)
            {
                return Ok(APIResponse<LoginResponseDto>.SuccessResponse(result, result.Message));
            }

            return Unauthorized(APIResponse<LoginResponseDto>.ErrorResponse(result.Message, result, statusCode: 401));
        }

        [HttpPost("refresh")]
        [AllowAnonymous]
        [SwaggerOperation(Summary = "Refresh access token")]
        [SwaggerResponse(200, "Token refreshed successfully", typeof(APIResponse<RefreshTokenResponseDto>))]
        [SwaggerResponse(401, "Invalid refresh token")]
        public async Task<ActionResult<APIResponse<RefreshTokenResponseDto>>> RefreshToken([FromBody] RefreshTokenDto refreshTokenDto)
        {
            var result = await _authService.RefreshTokenAsync(refreshTokenDto);
            
            if (result.Success)
            {
                return Ok(APIResponse<RefreshTokenResponseDto>.SuccessResponse(result, result.Message));
            }

            return Unauthorized(APIResponse<RefreshTokenResponseDto>.ErrorResponse(result.Message, result, statusCode: 401));
        }

        [HttpPost("logout")]
        [Authorize]
        [SwaggerOperation(Summary = "Logout user (revoke access token)")]
        [SwaggerResponse(200, "Logout successful")]
        [SwaggerResponse(401, "Unauthorized")]
        public async Task<ActionResult<APIResponse<object>>> Logout()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(APIResponse<object>.ErrorResponse("Invalid user", statusCode: 401));
            }

            var result = await _authService.RevokeTokenAsync(userId);
            
            if (result)
            {
                return Ok(APIResponse<object>.SuccessResponse(null, "Logout successful"));
            }

            return BadRequest(APIResponse<object>.ErrorResponse("Logout failed"));
        }

        [HttpPost("revoke-refresh-token")]
        [Authorize]
        [SwaggerOperation(Summary = "Revoke refresh token")]
        [SwaggerResponse(200, "Refresh token revoked successfully")]
        [SwaggerResponse(401, "Unauthorized")]
        public async Task<ActionResult<APIResponse<object>>> RevokeRefreshToken([FromBody] string refreshToken)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(APIResponse<object>.ErrorResponse("Invalid user", statusCode: 401));
            }

            var result = await _authService.RevokeRefreshTokenAsync(userId, refreshToken);
            
            if (result)
            {
                return Ok(APIResponse<object>.SuccessResponse(null, "Refresh token revoked successfully"));
            }

            return BadRequest(APIResponse<object>.ErrorResponse("Failed to revoke refresh token"));
        }

        [HttpPost("change-password")]
        [Authorize]
        [SwaggerOperation(Summary = "Change user password")]
        [SwaggerResponse(200, "Password changed successfully")]
        [SwaggerResponse(400, "Password change failed")]
        [SwaggerResponse(401, "Unauthorized")]
        public async Task<ActionResult<APIResponse<object>>> ChangePassword([FromBody] ChangePasswordDto changePasswordDto)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(APIResponse<object>.ErrorResponse("Invalid user", statusCode: 401));
            }

            var result = await _authService.ChangePasswordAsync(userId, changePasswordDto);
            
            if (result)
            {
                return Ok(APIResponse<object>.SuccessResponse(null, "Password changed successfully"));
            }

            return BadRequest(APIResponse<object>.ErrorResponse("Password change failed"));
        }

        [HttpPost("forgot-password")]
        [AllowAnonymous]
        [SwaggerOperation(Summary = "Request password reset")]
        [SwaggerResponse(200, "Password reset email sent")]
        public async Task<ActionResult<APIResponse<object>>> ForgotPassword([FromBody] ForgotPasswordDto forgotPasswordDto)
        {
            var result = await _authService.ForgotPasswordAsync(forgotPasswordDto);
            
            return Ok(APIResponse<object>.SuccessResponse(null, "If the email exists, a password reset link has been sent"));
        }

        [HttpPost("reset-password")]
        [AllowAnonymous]
        [SwaggerOperation(Summary = "Reset user password")]
        [SwaggerResponse(200, "Password reset successful")]
        [SwaggerResponse(400, "Password reset failed")]
        public async Task<ActionResult<APIResponse<object>>> ResetPassword([FromBody] ResetPasswordDto resetPasswordDto)
        {
            var result = await _authService.ResetPasswordAsync(resetPasswordDto);
            
            if (result)
            {
                return Ok(APIResponse<object>.SuccessResponse(null, "Password reset successful"));
            }

            return BadRequest(APIResponse<object>.ErrorResponse("Password reset failed"));
        }

        [HttpGet("me")]
        [Authorize]
        [SwaggerOperation(Summary = "Get current user info")]
        [SwaggerResponse(200, "User info retrieved successfully")]
        [SwaggerResponse(401, "Unauthorized")]
        public ActionResult<APIResponse<object>> GetCurrentUser()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var email = User.FindFirst(ClaimTypes.Email)?.Value;
            var roles = User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new APIResponse<object>
                {
                    Success = false,
                    Message = "Invalid user"
                });
            }

            return Ok(new APIResponse<object>
            {
                Success = true,
                Message = "User info retrieved successfully",
                Data = new
                {
                    UserId = userId,
                    Email = email,
                    Roles = roles
                }
            });
        }
    }
}