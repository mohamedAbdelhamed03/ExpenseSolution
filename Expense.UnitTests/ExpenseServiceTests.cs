using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Expense.Core.Domain.Entities;
using Expense.Core.Domain.Enums;
using Expense.Core.DTOs.Expenses;
using Expense.Core.Common.Exceptions;
using Expense.Infrastructure.Data;
using Expense.Infrastructure.Expenses;
using Expense.Infrastructure.Repositories;
using Expense.Core.Abstractions.ActivityLogs;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Moq;
using FluentValidation;
using ExpenseEntity = Expense.Core.Domain.Entities.Expense;

namespace Expense.UnitTests
{
    public class ExpenseServiceTests
    {
        private readonly Mock<IValidator<CreateExpenseDto>> _mockCreateValidator;
        private readonly Mock<IValidator<UpdateExpenseDto>> _mockUpdateValidator;
        private readonly Mock<IValidator<UpdateExpensePatchDto>> _mockPatchValidator;

        public ExpenseServiceTests()
        {
            _mockCreateValidator = new Mock<IValidator<CreateExpenseDto>>();
            _mockCreateValidator.Setup(v => v.ValidateAsync(It.IsAny<CreateExpenseDto>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new FluentValidation.Results.ValidationResult());

            _mockUpdateValidator = new Mock<IValidator<UpdateExpenseDto>>();
            _mockUpdateValidator.Setup(v => v.ValidateAsync(It.IsAny<UpdateExpenseDto>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new FluentValidation.Results.ValidationResult());

            _mockPatchValidator = new Mock<IValidator<UpdateExpensePatchDto>>();
            _mockPatchValidator.Setup(v => v.ValidateAsync(It.IsAny<UpdateExpensePatchDto>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new FluentValidation.Results.ValidationResult());
        }

        private ApplicationDbContext CreateContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;
            return new ApplicationDbContext(options);
        }

        [Fact]
        public async Task CreateExpenseAsync_ShouldCreateExpense_WhenValid()
        {
            // Arrange
            var dbName = Guid.NewGuid().ToString();
            using var context = CreateContext(dbName);
            var userId = "user1";
            var group = new Group { Id = Guid.NewGuid(), Name = "Test Group", CreatedByUserId = userId, InviteCode = "123" };
            var member = new GroupMember { GroupId = group.Id, UserId = userId, Role = GroupRole.Admin };
            context.Groups.Add(group);
            context.GroupMembers.Add(member);
            await context.SaveChangesAsync();

            var mockActivityLogService = new Mock<IActivityLogService>();
            var service = new ExpenseService(
                new UnitOfWork(context), 
                mockActivityLogService.Object,
                _mockCreateValidator.Object,
                _mockUpdateValidator.Object,
                _mockPatchValidator.Object);

            var dto = new CreateExpenseDto
            {
                Amount = 100,
                Description = "Lunch",
                ExpenseDate = DateTime.Now,
                Splits = new List<ExpenseSplitDto>
                {
                    new ExpenseSplitDto { UserId = userId, Amount = 100 }
                }
            };

            // Act
            var result = await service.CreateExpenseAsync(group.Id, userId, dto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(100, result.Amount);
            Assert.Single(result.Splits);
            
            using var verifyContext = CreateContext(dbName);
            var savedExpense = await verifyContext.Expenses.Include(e => e.Splits).FirstOrDefaultAsync(e => e.Id == result.Id);
            Assert.NotNull(savedExpense);
            Assert.Equal(100, savedExpense.Amount);
            Assert.Single(savedExpense.Splits);
        }

        [Fact]
        public async Task CreateExpenseAsync_ShouldThrow_WhenUserNotMember()
        {
            // Arrange
            var dbName = Guid.NewGuid().ToString();
            using var context = CreateContext(dbName);
            var mockActivityLogService = new Mock<IActivityLogService>();
            var service = new ExpenseService(
                new UnitOfWork(context), 
                mockActivityLogService.Object,
                _mockCreateValidator.Object,
                _mockUpdateValidator.Object,
                _mockPatchValidator.Object);
            var dto = new CreateExpenseDto { Amount = 100 };

            // Act & Assert
            await Assert.ThrowsAsync<GroupAccessDeniedException>(() => service.CreateExpenseAsync(Guid.NewGuid(), "non-member", dto));
        }

        [Fact]
        public async Task CreateExpenseAsync_ShouldThrow_WhenSplitTotalMismatch()
        {
            // Arrange
            var dbName = Guid.NewGuid().ToString();
            using var context = CreateContext(dbName);
            var userId = "user1";
            var group = new Group { Id = Guid.NewGuid(), Name = "Test Group", CreatedByUserId = userId, InviteCode = "123" };
            var member = new GroupMember { GroupId = group.Id, UserId = userId, Role = GroupRole.Admin };
            context.Groups.Add(group);
            context.GroupMembers.Add(member);
            await context.SaveChangesAsync();

            var mockActivityLogService = new Mock<IActivityLogService>();
            var service = new ExpenseService(
                new UnitOfWork(context), 
                mockActivityLogService.Object,
                _mockCreateValidator.Object,
                _mockUpdateValidator.Object,
                _mockPatchValidator.Object);
            var dto = new CreateExpenseDto
            {
                Amount = 100,
                Splits = new List<ExpenseSplitDto>
                {
                    new ExpenseSplitDto { UserId = userId, Amount = 90 } // Mismatch
                }
            };

            // Act & Assert
            var ex = await Assert.ThrowsAsync<BusinessException>(() => service.CreateExpenseAsync(group.Id, userId, dto));
            Assert.Equal("Expense_SplitTotalMismatch", ex.ErrorCode);
        }

        [Fact]
        public async Task CreateExpenseAsync_ShouldDistributeRemainder_WhenSplittingEqually()
        {
            // Arrange
            var dbName = Guid.NewGuid().ToString();
            using var context = CreateContext(dbName);
            var userId1 = "user1";
            var userId2 = "user2";
            var userId3 = "user3";
            var group = new Group { Id = Guid.NewGuid(), Name = "Test Group", CreatedByUserId = userId1, InviteCode = "123" };
            var member1 = new GroupMember { GroupId = group.Id, UserId = userId1, Role = GroupRole.Admin };
            var member2 = new GroupMember { GroupId = group.Id, UserId = userId2, Role = GroupRole.Member };
            var member3 = new GroupMember { GroupId = group.Id, UserId = userId3, Role = GroupRole.Member };
            context.Groups.Add(group);
            context.GroupMembers.AddRange(member1, member2, member3);
            await context.SaveChangesAsync();

            var mockActivityLogService = new Mock<IActivityLogService>();
            var service = new ExpenseService(
                new UnitOfWork(context), 
                mockActivityLogService.Object,
                _mockCreateValidator.Object,
                _mockUpdateValidator.Object,
                _mockPatchValidator.Object);
            
            var dto = new CreateExpenseDto
            {
                Amount = 100, // 100 / 3 = 33.33 remainder 0.01
                Description = "Lunch",
                ExpenseDate = DateTime.Now,
                Splits = null // Auto split
            };

            // Act
            var result = await service.CreateExpenseAsync(group.Id, userId1, dto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.Splits.Count());
            
            // Payer should get remainder
            var payerSplit = result.Splits.First(s => s.UserId == userId1);
            Assert.Equal(33.34m, payerSplit.Amount);
            
            var otherSplit = result.Splits.First(s => s.UserId == userId2);
            Assert.Equal(33.33m, otherSplit.Amount);
            
            // Log verification
            mockActivityLogService.Verify(x => x.LogActivityAsync(
                It.Is<Guid>(g => g == group.Id),
                It.Is<string>(u => u == userId1),
                It.Is<ActivityType>(t => t == ActivityType.Created),
                It.IsAny<EntityType>(),
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
