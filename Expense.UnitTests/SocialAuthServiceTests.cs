using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Expense.Core.Abstractions.Authentication;
using Expense.Core.Abstractions.Persistence;
using Expense.Core.Domain.Entities;
using Expense.Core.Domain.Enums;
using Expense.Core.Domain.IdentityEntities;
using Expense.Core.DTOs.Auth;
using Expense.Infrastructure.Authentication.Social;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace Expense.UnitTests
{
    public class SocialAuthServiceTests
    {
        private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
        private readonly Mock<IJwtService> _jwtServiceMock;
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
        private readonly List<ISocialTokenValidator> _validators;
        private readonly Mock<ISocialTokenValidator> _googleValidatorMock;
        private readonly SocialAuthService _service;

        public SocialAuthServiceTests()
        {
            var store = new Mock<IUserStore<ApplicationUser>>();
            _userManagerMock = new Mock<UserManager<ApplicationUser>>(store.Object, null, null, null, null, null, null, null, null);
            _jwtServiceMock = new Mock<IJwtService>();
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _httpContextAccessorMock = new Mock<IHttpContextAccessor>();
            
            _googleValidatorMock = new Mock<ISocialTokenValidator>();
            _googleValidatorMock.Setup(v => v.Provider).Returns(AuthProvider.Google);
            
            _validators = new List<ISocialTokenValidator> { _googleValidatorMock.Object };

            var refreshTokenRepoMock = new Mock<IRepository<RefreshToken>>();
            _unitOfWorkMock.Setup(u => u.Repository<RefreshToken>()).Returns(refreshTokenRepoMock.Object);

            _service = new SocialAuthService(
                _userManagerMock.Object,
                _jwtServiceMock.Object,
                _unitOfWorkMock.Object,
                _validators,
                _httpContextAccessorMock.Object
            );
        }

        [Fact]
        public async Task SocialLoginAsync_ShouldReturnError_WhenProviderNotSupported()
        {
            var dto = new SocialLoginDto { Provider = AuthProvider.Facebook, Token = "token" };
            var result = await _service.SocialLoginAsync(dto);
            Assert.False(result.Success);
            Assert.Equal("Unsupported provider", result.Message);
        }

        [Fact]
        public async Task SocialLoginAsync_ShouldReturnError_WhenTokenInvalid()
        {
            _googleValidatorMock.Setup(v => v.ValidateTokenAsync("token")).ReturnsAsync((SocialUserDto?)null);
            var dto = new SocialLoginDto { Provider = AuthProvider.Google, Token = "token" };
            var result = await _service.SocialLoginAsync(dto);
            Assert.False(result.Success);
            Assert.Equal("Invalid token", result.Message);
        }

        [Fact]
        public async Task SocialLoginAsync_ShouldReturnError_WhenEmailMissing()
        {
            _googleValidatorMock.Setup(v => v.ValidateTokenAsync("token")).ReturnsAsync(new SocialUserDto { Id = "123" });
            var dto = new SocialLoginDto { Provider = AuthProvider.Google, Token = "token" };
            var result = await _service.SocialLoginAsync(dto);
            Assert.False(result.Success);
            Assert.Equal("Email is required for authentication", result.Message);
        }
        [Fact]
        public async Task SocialLoginAsync_ShouldReturnSuccess_WhenNewUserRegisters()
        {
            var socialUser = new SocialUserDto
            {
                Id = "google123",
                Email = "newuser@gmail.com",
                FirstName = "New",
                LastName = "User",
                PictureUrl = "http://pic.com"
            };

            _googleValidatorMock.Setup(v => v.ValidateTokenAsync("valid_token"))
                .ReturnsAsync(socialUser);

            _userManagerMock.Setup(u => u.FindByEmailAsync(socialUser.Email))
                .ReturnsAsync((ApplicationUser?)null);

            _userManagerMock.Setup(u => u.CreateAsync(It.IsAny<ApplicationUser>()))
                .ReturnsAsync(IdentityResult.Success);

            _userManagerMock.Setup(u => u.AddToRoleAsync(It.IsAny<ApplicationUser>(), "User"))
                .ReturnsAsync(IdentityResult.Success);

            _userManagerMock.Setup(u => u.GetRolesAsync(It.IsAny<ApplicationUser>()))
                .ReturnsAsync(new List<string> { "User" });

            _jwtServiceMock.Setup(j => j.GenerateAccessToken(It.IsAny<IEnumerable<Claim>>()))
                .Returns("access_token");

            _jwtServiceMock.Setup(j => j.GenerateRefreshToken())
                .Returns("refresh_token");

            var dto = new SocialLoginDto { Provider = AuthProvider.Google, Token = "valid_token" };

            var result = await _service.SocialLoginAsync(dto);

            Assert.True(result.Success);
            Assert.Equal("Login successful", result.Message);
            Assert.NotNull(result.AccessToken);
            Assert.Equal("newuser@gmail.com", result.Email);

            _userManagerMock.Verify(u => u.CreateAsync(It.Is<ApplicationUser>(user =>
                user.Email == socialUser.Email &&
                user.GoogleId == socialUser.Id &&
                user.Provider == AuthProvider.Google
            )), Times.Once);
        }
    }
}
