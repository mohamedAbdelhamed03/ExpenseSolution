using Expense.Core.DTOs.Expenses;
using Expense.Core.DTOs.Groups;
using Expense.Core.DTOs.Shared;
using Expense.IntegrationTests.Helpers;
using FluentAssertions;
using System.Net.Http.Json;
using Xunit;

namespace Expense.IntegrationTests.Expenses
{
    public class ExpenseCurrencyTests : IntegrationTestBase
    {
        public ExpenseCurrencyTests(CustomWebApplicationFactory<Expense.API.Program> factory) : base(factory)
        {
        }

        [Fact]
        public async Task CreateExpense_WithoutCurrency_ShouldDefaultToEGP()
        {
            // Arrange
            var token = await AuthenticateAsync("user_curr@test.com", "Password123!");
            _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            var groupResponse = await _client.PostAsJsonAsync("/api/groups", new CreateGroupDto { Name = "Currency Group" });
            var group = (await groupResponse.Content.ReadFromJsonAsync<APIResponse<GroupDto>>())!.Data!;

            var createDto = new CreateExpenseDto
            {
                Amount = 100,
                Description = "Default Currency",
                ExpenseDate = DateTime.UtcNow,
                // Currency intentionally omitted (defaults to EGP in DTO ctor)
            };

            // Act
            var response = await _client.PostAsJsonAsync($"/api/groups/{group.Id}/expenses", createDto);
            
            // Assert
            response.EnsureSuccessStatusCode();
            var expense = (await response.Content.ReadFromJsonAsync<APIResponse<ExpenseDto>>())!.Data!;
            expense.Currency.Should().Be("EGP");
            expense.ExchangeRate.Should().BeNull();
        }

        [Fact]
        public async Task CreateExpense_WithCurrencyUSD_ShouldPersist()
        {
            // Arrange
            var token = await AuthenticateAsync("user_curr2@test.com", "Password123!");
            _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            var groupResponse = await _client.PostAsJsonAsync("/api/groups", new CreateGroupDto { Name = "Currency Group 2" });
            var group = (await groupResponse.Content.ReadFromJsonAsync<APIResponse<GroupDto>>())!.Data!;

            var createDto = new CreateExpenseDto
            {
                Amount = 100,
                Description = "USD Expense",
                ExpenseDate = DateTime.UtcNow,
                Currency = "USD",
                ExchangeRate = 50.5m
            };

            // Act
            var response = await _client.PostAsJsonAsync($"/api/groups/{group.Id}/expenses", createDto);

            // Assert
            response.EnsureSuccessStatusCode();
            var expense = (await response.Content.ReadFromJsonAsync<APIResponse<ExpenseDto>>())!.Data!;
            expense.Currency.Should().Be("USD");
            expense.ExchangeRate.Should().Be(50.5m);
        }
    }
}
