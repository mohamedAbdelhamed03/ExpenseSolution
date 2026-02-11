using Expense.Core.Application.ActivityLogs;
using Expense.Core.Application.Notifications;
using Expense.Core.Application.Persistence;
using Expense.Core.Common.Exceptions;
using Expense.Core.Domain.Entities;
using Expense.Core.Domain.Enums;
using Expense.Core.DTOs.Categories;
using Expense.Infrastructure.Categories;
using FluentValidation;
using Moq;
using System;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Expense.UnitTests
{
    public class ExpenseCategoryServiceTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IActivityLogService> _mockActivityLogService;
        private readonly Mock<IValidator<CreateExpenseCategoryDto>> _mockCreateValidator;
        private readonly Mock<IValidator<UpdateExpenseCategoryDto>> _mockUpdateValidator;
        private readonly Mock<IValidator<UpdateCategoryPatchDto>> _mockPatchValidator;
        private readonly Mock<IExpenseCategoryRepository> _mockCategoryRepo;
        private readonly Mock<IGroupRepository> _mockGroupRepo;
        private readonly ExpenseCategoryService _service;

        public ExpenseCategoryServiceTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockActivityLogService = new Mock<IActivityLogService>();
            _mockCreateValidator = new Mock<IValidator<CreateExpenseCategoryDto>>();
            _mockUpdateValidator = new Mock<IValidator<UpdateExpenseCategoryDto>>();
            _mockPatchValidator = new Mock<IValidator<UpdateCategoryPatchDto>>();
            _mockCategoryRepo = new Mock<IExpenseCategoryRepository>();
            _mockGroupRepo = new Mock<IGroupRepository>();

            _mockUnitOfWork.Setup(u => u.Categories).Returns(_mockCategoryRepo.Object);
            _mockUnitOfWork.Setup(u => u.Groups).Returns(_mockGroupRepo.Object);

            _service = new ExpenseCategoryService(
                _mockUnitOfWork.Object,
                _mockActivityLogService.Object,
                _mockCreateValidator.Object,
                _mockUpdateValidator.Object,
                _mockPatchValidator.Object);
        }

        [Fact]
        public async Task UpdateCategoryAsync_ShouldUpdateIcon_WhenValid()
        {
            // Arrange
            var userId = Guid.NewGuid().ToString();
            var groupId = Guid.NewGuid();
            var categoryId = Guid.NewGuid();

            var category = new ExpenseCategory
            {
                Id = categoryId,
                GroupId = groupId,
                Name = "Old Name",
                Icon = "🍔"
            };

            var member = new GroupMember { UserId = userId, Role = GroupRole.Admin };

            _mockCategoryRepo.Setup(r => r.Get(It.IsAny<Expression<Func<ExpenseCategory, bool>>>(), false, It.IsAny<Expression<Func<ExpenseCategory, object>>[]>()))
                .ReturnsAsync(category);

            _mockGroupRepo.Setup(r => r.GetMemberAsync(groupId, userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(member);

            var dto = new UpdateExpenseCategoryDto
            {
                Name = "New Name",
                Icon = "🍕"
            };

            // Act
            var result = await _service.UpdateCategoryAsync(categoryId, userId, dto, CancellationToken.None);

            // Assert
            Assert.Equal("New Name", result.Name);
            Assert.Equal("🍕", result.Icon);
            _mockCategoryRepo.Verify(r => r.Update(category), Times.Once);
            _mockUnitOfWork.Verify(u => u.SaveAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task UpdateCategoryPartialAsync_ShouldUpdateIcon_WhenValid()
        {
            // Arrange
            var userId = Guid.NewGuid().ToString();
            var groupId = Guid.NewGuid();
            var categoryId = Guid.NewGuid();

            var category = new ExpenseCategory
            {
                Id = categoryId,
                GroupId = groupId,
                Name = "Old Name",
                Icon = "🍔"
            };

            var member = new GroupMember { UserId = userId, Role = GroupRole.Admin };

            _mockCategoryRepo.Setup(r => r.Get(It.IsAny<Expression<Func<ExpenseCategory, bool>>>(), false, It.IsAny<Expression<Func<ExpenseCategory, object>>[]>()))
                .ReturnsAsync(category);

            _mockGroupRepo.Setup(r => r.GetMemberAsync(groupId, userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(member);

            var dto = new UpdateCategoryPatchDto
            {
                Icon = "🍕"
            };

            // Act
            var result = await _service.UpdateCategoryPartialAsync(categoryId, userId, dto, CancellationToken.None);

            // Assert
            Assert.Equal("Old Name", result.Name); // Should be unchanged
            Assert.Equal("🍕", result.Icon);
            _mockCategoryRepo.Verify(r => r.Update(category), Times.Once);
            _mockUnitOfWork.Verify(u => u.SaveAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
