using Expense.Core.Application.ActivityLogs;
using Expense.Core.Application.Notifications;
using Expense.Core.Application.Persistence;
using Expense.Core.Domain.Entities;
using Expense.Core.Domain.Enums;
using Expense.Core.DTOs.Groups;
using Expense.Infrastructure.Groups;
using Expense.Infrastructure.Identity;
using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Expense.UnitTests
{
    public class GroupServiceTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IActivityLogService> _mockActivityLogService;
        private readonly Mock<IRealtimeNotifier> _mockNotifier;
        private readonly Mock<IValidator<UpdateGroupMemberRolePatchDto>> _mockPatchValidator;
        private readonly Mock<UserManager<ApplicationUser>> _mockUserManager;
        private readonly Mock<IGroupRepository> _mockGroupRepo;
        private readonly Mock<ILogger<GroupService>> _mockLogger;
        private readonly GroupService _service;

        public GroupServiceTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockActivityLogService = new Mock<IActivityLogService>();
            _mockNotifier = new Mock<IRealtimeNotifier>();
            _mockPatchValidator = new Mock<IValidator<UpdateGroupMemberRolePatchDto>>();
            _mockGroupRepo = new Mock<IGroupRepository>();
            _mockLogger = new Mock<ILogger<GroupService>>();

            var userStore = new Mock<IUserStore<ApplicationUser>>();
            _mockUserManager = new Mock<UserManager<ApplicationUser>>(userStore.Object, null, null, null, null, null, null, null, null);

            _mockUnitOfWork.Setup(u => u.Groups).Returns(_mockGroupRepo.Object);

            _service = new GroupService(
                _mockUnitOfWork.Object,
                _mockActivityLogService.Object,
                _mockNotifier.Object,
                _mockPatchValidator.Object,
                _mockUserManager.Object,
                _mockLogger.Object);
        }

        [Fact]
        public async Task CreateGroupAsync_ShouldMapLogoUrl_WhenValid()
        {
            // Arrange
            var userId = Guid.NewGuid().ToString();
            var dto = new CreateGroupDto
            {
                Name = "New Group",
                LogoUrl = "http://example.com/logo.png"
            };

            // Act
            var result = await _service.CreateGroupAsync(userId, dto, CancellationToken.None);

            // Assert
            Assert.Equal("New Group", result.Name);
            Assert.Equal("http://example.com/logo.png", result.LogoUrl);
            _mockGroupRepo.Verify(r => r.Add(It.Is<Group>(g => g.LogoUrl == dto.LogoUrl)), Times.Once);
            _mockUnitOfWork.Verify(u => u.SaveAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
