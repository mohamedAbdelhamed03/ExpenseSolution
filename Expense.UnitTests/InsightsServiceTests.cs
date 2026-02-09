using Expense.Core.Application.Persistence;
using Expense.Core.Common.Exceptions;
using Expense.Core.Domain.Entities;
using Expense.Core.DTOs.Insights;
using Expense.Infrastructure.Insights;
using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Expense.UnitTests
{
    public class InsightsServiceTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IExpenseRepository> _mockExpenseRepo;
        private readonly Mock<IGroupRepository> _mockGroupRepo;
        private readonly Mock<IExpenseCategoryRepository> _mockCategoryRepo;
        private readonly InsightsService _service;

        public InsightsServiceTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockExpenseRepo = new Mock<IExpenseRepository>();
            _mockGroupRepo = new Mock<IGroupRepository>();
            _mockCategoryRepo = new Mock<IExpenseCategoryRepository>();

            _mockUnitOfWork.Setup(u => u.Expenses).Returns(_mockExpenseRepo.Object);
            _mockUnitOfWork.Setup(u => u.Groups).Returns(_mockGroupRepo.Object);
            _mockUnitOfWork.Setup(u => u.Categories).Returns(_mockCategoryRepo.Object);

            _service = new InsightsService(_mockUnitOfWork.Object);
        }

        [Fact]
        public async Task GetInsightsAsync_ShouldReturnCorrectData_ForMonth()
        {
            // Arrange
            var groupId = Guid.NewGuid();
            var userId = "user1";
            var categoryId = Guid.NewGuid();

            _mockGroupRepo.Setup(r => r.IsMemberAsync(groupId, userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            var stats = new List<CategoryStatistics>
            {
                new CategoryStatistics { CategoryId = categoryId, TotalAmount = 100, Currency = "USD" },
                new CategoryStatistics { CategoryId = null, TotalAmount = 50, Currency = "USD" } // Uncategorized
            };

            _mockExpenseRepo.Setup(r => r.GetInsightsByCategoryAsync(groupId, It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .ReturnsAsync(stats);

            var categories = new List<ExpenseCategory>
            {
                new ExpenseCategory { Id = categoryId, Name = "Food", GroupId = groupId }
            };

            _mockCategoryRepo.Setup(r => r.GetAll(It.IsAny<Expression<Func<ExpenseCategory, bool>>>(), It.IsAny<Expression<Func<ExpenseCategory, object>>[]>()))
                .ReturnsAsync(categories);

            // Act
            var result = await _service.GetInsightsAsync(groupId, "month", "2023-10", userId);

            // Assert
            result.Should().HaveCount(1); // One currency
            var summary = result.First();
            summary.Currency.Should().Be("USD");
            summary.TotalAmount.Should().Be(150);
            summary.Categories.Should().HaveCount(2);
            
            var food = summary.Categories.FirstOrDefault(c => c.CategoryId == categoryId);
            food.Should().NotBeNull();
            food.CategoryName.Should().Be("Food");
            food.Amount.Should().Be(100);
            food.Percentage.Should().Be(66.67m);

            var uncategorized = summary.Categories.FirstOrDefault(c => c.CategoryId == null);
            uncategorized.Should().NotBeNull();
            uncategorized.CategoryName.Should().Be("Uncategorized");
            uncategorized.Amount.Should().Be(50);
            uncategorized.Percentage.Should().Be(33.33m);
        }

        [Fact]
        public async Task GetInsightsAsync_ShouldThrow_WhenUserNotMember()
        {
            // Arrange
            var groupId = Guid.NewGuid();
            var userId = "user1";

            _mockGroupRepo.Setup(r => r.IsMemberAsync(groupId, userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            // Act
            Func<Task> act = async () => await _service.GetInsightsAsync(groupId, "month", "2023-10", userId);

            // Assert
            await act.Should().ThrowAsync<GroupAccessDeniedException>();
        }
    }
}
