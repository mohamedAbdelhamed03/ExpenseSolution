using Expense.Core.DTOs.Categories;
using Expense.Core.DTOs.Expenses;
using Expense.Core.DTOs.Groups;
using Expense.Core.DTOs.Insights;
using Expense.Core.DTOs.Shared;
using Expense.IntegrationTests.Helpers;
using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Xunit;

namespace Expense.IntegrationTests.Insights
{
    public class InsightsIntegrationTests : IntegrationTestBase
    {
        public InsightsIntegrationTests(CustomWebApplicationFactory<Expense.API.Program> factory) : base(factory)
        {
        }

        [Fact]
        public async Task GetInsights_ShouldReturnCorrectAggregations()
        {
            // 1. Authenticate
            await AuthenticateAsync("insightuser1@test.com", "Password123!");

            // 2. Create Group
            var groupResponse = await _client.PostAsJsonAsync("/api/groups", new CreateGroupDto 
            { 
                Name = "Insights Group"
            });
            groupResponse.EnsureSuccessStatusCode();
            var groupResult = await groupResponse.Content.ReadFromJsonAsync<APIResponse<GroupDto>>();
            var groupId = groupResult.Data.Id;

            // 3. Create Categories
            var cat1Id = await CreateCategory(groupId, "Food");
            var cat2Id = await CreateCategory(groupId, "Transport");

            // 4. Create Expenses
            // Food: 100 USD
            await CreateExpense(groupId, 100, "USD", "Lunch", cat1Id);
            // Food: 50 USD
            await CreateExpense(groupId, 50, "USD", "Dinner", cat1Id);
            // Transport: 30 USD
            await CreateExpense(groupId, 30, "USD", "Taxi", cat2Id);
            // Uncategorized: 20 USD
            await CreateExpense(groupId, 20, "USD", "Misc", null);

            // 5. Get Insights (Month)
            var now = DateTime.UtcNow;
            var period = $"{now.Year}-{now.Month:D2}";
            var response = await _client.GetAsync($"/api/groups/{groupId}/insights?period=month&date={period}");
            
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<IEnumerable<InsightsSummaryDto>>();

            // 6. Assert
            result.Should().HaveCount(1);
            var summary = result.First();
            summary.Currency.Should().Be("USD");
            summary.TotalAmount.Should().Be(200); 

            summary.Categories.Should().HaveCount(3); 

            var food = summary.Categories.First(c => c.CategoryName == "Food");
            food.Amount.Should().Be(150);
            food.Percentage.Should().Be(75);

            var transport = summary.Categories.First(c => c.CategoryName == "Transport");
            transport.Amount.Should().Be(30);
            transport.Percentage.Should().Be(15);

            var uncategorized = summary.Categories.First(c => c.CategoryName == "Uncategorized");
            uncategorized.Amount.Should().Be(20);
            uncategorized.Percentage.Should().Be(10);
        }

        private async Task<Guid> CreateCategory(Guid groupId, string name)
        {
            var response = await _client.PostAsJsonAsync($"/api/groups/{groupId}/categories", new CreateExpenseCategoryDto 
            { 
                Name = name, 
                Description = name 
            });
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<APIResponse<ExpenseCategoryDto>>();
            return result.Data.Id;
        }

        private async Task CreateExpense(Guid groupId, decimal amount, string currency, string description, Guid? categoryId)
        {
            var dto = new CreateExpenseDto
            {
                Amount = amount,
                Currency = currency,
                ExchangeRate = 1m,
                Description = description,
                ExpenseDate = DateTime.UtcNow,
                CategoryId = categoryId,
                Splits = null
            };
            
            var response = await _client.PostAsJsonAsync($"/api/groups/{groupId}/expenses", dto);
            
            if (!response.IsSuccessStatusCode)
            {
                 var content = await response.Content.ReadAsStringAsync();
                 throw new Exception($"Failed to create expense: {content}");
            }
        }
    }
}
