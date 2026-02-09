using System;
using System.Threading.Tasks;
using Expense.Infrastructure.Data;
using Expense.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;
using ExpenseEntity = Expense.Core.Domain.Entities.Expense;

namespace Expense.UnitTests
{
    public class InsightsAggregationTests
    {
        private static ApplicationDbContext CreateContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;
            return new ApplicationDbContext(options);
        }

        [Fact]
        public async Task GetInsightsByCategoryAsync_UsesExchangeRateInAggregation()
        {
            // Arrange
            var dbName = Guid.NewGuid().ToString();
            var groupId = Guid.NewGuid();
            var expenseDate = DateTime.UtcNow;

            await using (var context = CreateContext(dbName))
            {
                context.Expenses.Add(new ExpenseEntity
                {
                    Id = Guid.NewGuid(),
                    GroupId = groupId,
                    PaidByUserId = "user1",
                    Amount = 100m,
                    Currency = "USD",
                    ExchangeRate = 30m,
                    ExpenseDate = expenseDate,
                    CreatedAt = expenseDate
                });
                await context.SaveChangesAsync();
            }

            await using (var context = CreateContext(dbName))
            {
                var repo = new ExpenseRepository(context);

                // Act
                var results = await repo.GetInsightsByCategoryAsync(
                    groupId,
                    expenseDate.AddDays(-1),
                    expenseDate.AddDays(1));

                // Assert
                var stat = Assert.Single(results);
                Assert.Equal(3000m, stat.TotalAmount);
                Assert.Equal("USD", stat.Currency);
            }
        }
    }
}
