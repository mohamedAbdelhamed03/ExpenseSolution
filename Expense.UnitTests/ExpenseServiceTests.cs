using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Expense.Core.Domain.Entities;
using Expense.Core.DTOs.Expenses;
using Expense.Core.Exceptions;
using Expense.Infrastructure.Data;
using Expense.Infrastructure.Expenses;
using Microsoft.EntityFrameworkCore;
using Xunit;
using ExpenseEntity = Expense.Core.Domain.Entities.Expense;

namespace Expense.UnitTests
{
    public class ExpenseServiceTests
    {
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

            var service = new ExpenseService(context);
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
            var service = new ExpenseService(context);
            var dto = new CreateExpenseDto { Amount = 100 };

            // Act & Assert
            await Assert.ThrowsAsync<BusinessException>(() => service.CreateExpenseAsync(Guid.NewGuid(), "non-member", dto));
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

            var service = new ExpenseService(context);
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
            Assert.Equal("Split total mismatch", ex.Message);
        }
    }
}
