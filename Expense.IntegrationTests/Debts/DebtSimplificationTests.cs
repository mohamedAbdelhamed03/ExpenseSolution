using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Expense.Core.DTOs.Debts;
using Expense.Core.DTOs.Expenses;
using Expense.Core.DTOs.Groups;
using Expense.Core.DTOs.Categories;
using Expense.Core.DTOs.Shared;
using Expense.Core.Domain.IdentityEntities;
using Expense.IntegrationTests.Helpers;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Expense.IntegrationTests.Debts
{
    public class DebtSimplificationTests : IntegrationTestBase
    {
        public DebtSimplificationTests(CustomWebApplicationFactory<Expense.API.Program> factory) : base(factory)
        {
        }

        [Fact]
        public async Task GetSimplifiedDebts_ShouldReturnSimplifiedChain()
        {
            // Arrange
            // 1. Create Users
            var tokenA = await AuthenticateAsync("userA@test.com", "Password123!");
            var tokenB = await AuthenticateAsync("userB@test.com", "Password123!");
            var tokenC = await AuthenticateAsync("userC@test.com", "Password123!");

            var userAId = await GetUserIdFromTokenAsync(tokenA);
            var userBId = await GetUserIdFromTokenAsync(tokenB);
            var userCId = await GetUserIdFromTokenAsync(tokenC);
            
            // 2. Create Group (User A)
            var group = await CreateGroupAsync(tokenA, "Debt Test Group");
            
            // 3. Add Members
            await JoinGroupAsync(group.Id, tokenB, group.InviteCode);
            await JoinGroupAsync(group.Id, tokenC, group.InviteCode);

            // 4. Create Categories (Required for expenses)
            var category = await CreateCategoryAsync(tokenA, group.Id, "General");

            // 5. Create Expenses
            // B pays 100 for A (A owes B 100)
            await CreateExpenseAsync(tokenB, group.Id, "B pays for A", 100, "USD", category.Id, new[] { userAId });
            
            // C pays 100 for B (B owes C 100)
            await CreateExpenseAsync(tokenC, group.Id, "C pays for B", 100, "USD", category.Id, new[] { userBId });

            // 6. Get Simplified Debts (User A checks)
            _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokenA);
            var response = await _client.GetAsync($"/api/groups/{group.Id}/debts/simplified");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<APIResponse<IEnumerable<SimplifiedDebtGroupDto>>>();
            
            result.Should().NotBeNull();
            result!.Data.Should().NotBeNull();
            result.Data.Should().HaveCount(1);
            
            var groupDto = result.Data.First();
            groupDto.Currency.Should().Be("USD");
            groupDto.Transfers.Should().HaveCount(1);
            
            var transfer = groupDto.Transfers.First();
            // Expected: A owes C 100
            // Expected: A owes C 100
            transfer.FromUserId.Should().Be(userAId);
            transfer.ToUserId.Should().Be(userCId);
            transfer.Amount.Should().Be(100);
        }

        private async Task<string> GetUserIdFromTokenAsync(string token)
        {
            _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            var response = await _client.GetAsync("/api/auth/me");
            response.EnsureSuccessStatusCode();
            var payload = await response.Content.ReadFromJsonAsync<APIResponse<System.Text.Json.JsonElement>>();
            return payload!.Data.GetProperty("userId").GetString() ?? string.Empty;
        }

        private async Task<GroupDto> CreateGroupAsync(string token, string name)
        {
            _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            var response = await _client.PostAsJsonAsync("/api/groups", new CreateGroupDto { Name = name });
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<APIResponse<GroupDto>>();
            return result!.Data!;
        }

        private async Task JoinGroupAsync(Guid groupId, string token, string inviteCode)
        {
            _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            var response = await _client.PostAsync($"/api/groups/join/{inviteCode}", null);
            response.EnsureSuccessStatusCode();
        }

        private async Task<ExpenseCategoryDto> CreateCategoryAsync(string token, Guid groupId, string name)
        {
            _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            var response = await _client.PostAsJsonAsync($"/api/groups/{groupId}/categories", new CreateExpenseCategoryDto { Name = name });
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<APIResponse<ExpenseCategoryDto>>();
            return result!.Data!;
        }

        private async Task CreateExpenseAsync(string token, Guid groupId, string description, decimal amount, string currency, Guid categoryId, string[] splitUserIds)
        {
            _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            
            var splits = new List<ExpenseSplitDto>();
            // If multiple splits, assume equal split for simplicity in this test helper, OR full amount if single split
            // In the test case "B pays for A", A is the only split, so A takes full amount.
            var splitAmount = amount / splitUserIds.Length; 

            foreach (var userId in splitUserIds)
            {
                splits.Add(new ExpenseSplitDto { UserId = userId, Amount = splitAmount });
            }

            var dto = new CreateExpenseDto
            {
                Description = description,
                Amount = amount,
                Currency = currency,
                CategoryId = categoryId,
                ExpenseDate = DateTime.UtcNow,
                Splits = splits
            };

            var response = await _client.PostAsJsonAsync($"/api/groups/{groupId}/expenses", dto);
            response.EnsureSuccessStatusCode();
        }
    }
}
