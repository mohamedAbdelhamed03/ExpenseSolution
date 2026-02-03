using System.Security.Claims;
using Expense.Core.DTOs.Auth;
using Expense.Core.Interfaces;
using Expense.Core.DTO.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Expense.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
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
                return Ok(new APIResponse<RegisterResponseDto>
                {
                    Success = true,
                    Message = result.Message,
                    Data = result
                });
            }

            return BadRequest(new APIResponse<RegisterResponseDto>
            {
                Success = false,
                Message = result.Message,
                Data = result
            });
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
                return Ok(new APIResponse<LoginResponseDto>
                {
                    Success = true,
                    Message = result.Message,
                    Data = result
                });
            }

            return Unauthorized(new APIResponse<LoginResponseDto>
            {
                Success = false,
                Message = result.Message,
                Data = result
            });
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
                return Ok(new APIResponse<RefreshTokenResponseDto>
                {
                    Success = true,
                    Message = result.Message,
                    Data = result
                });
            }

            return Unauthorized(new APIResponse<RefreshTokenResponseDto>
            {
                Success = false,
                Message = result.Message,
                Data = result
            });
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
                return Unauthorized(new APIResponse<object>
                {
                    Success = false,
                    Message = "Invalid user"
                });
            }

            var result = await _authService.RevokeTokenAsync(userId);
            
            if (result)
            {
                return Ok(new APIResponse<object>
                {
                    Success = true,
                    Message = "Logout successful"
                });
            }

            return BadRequest(new APIResponse<object>
            {
                Success = false,
                Message = "Logout failed"
            });
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
                return Unauthorized(new APIResponse<object>
                {
                    Success = false,
                    Message = "Invalid user"
                });
            }

            var result = await _authService.RevokeRefreshTokenAsync(userId, refreshToken);
            
            if (result)
            {
                return Ok(new APIResponse<object>
                {
                    Success = true,
                    Message = "Refresh token revoked successfully"
                });
            }

            return BadRequest(new APIResponse<object>
            {
                Success = false,
                Message = "Failed to revoke refresh token"
            });
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
                return Unauthorized(new APIResponse<object>
                {
                    Success = false,
                    Message = "Invalid user"
                });
            }

            var result = await _authService.ChangePasswordAsync(userId, changePasswordDto);
            
            if (result)
            {
                return Ok(new APIResponse<object>
                {
                    Success = true,
                    Message = "Password changed successfully"
                });
            }

            return BadRequest(new APIResponse<object>
                {
                    Success = false,
                    Message = "Password change failed"
                });
        }

        [HttpPost("forgot-password")]
        [AllowAnonymous]
        [SwaggerOperation(Summary = "Request password reset")]
        [SwaggerResponse(200, "Password reset email sent")]
        public async Task<ActionResult<APIResponse<object>>> ForgotPassword([FromBody] ForgotPasswordDto forgotPasswordDto)
        {
            var result = await _authService.ForgotPasswordAsync(forgotPasswordDto);
            
            return Ok(new APIResponse<object>
            {
                Success = true,
                Message = "If the email exists, a password reset link has been sent"
            });
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
                return Ok(new APIResponse<object>
                {
                    Success = true,
                    Message = "Password reset successful"
                });
            }

            return BadRequest(new APIResponse<object>
            {
                Success = false,
                Message = "Password reset failed"
            });
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