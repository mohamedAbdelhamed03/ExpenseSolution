using Expense.Core.DTOs.Categories;
using Expense.Core.DTOs.Expenses;
using Expense.Core.DTOs.Groups;
using Expense.Core.DTOs.Settlements;
using Expense.Core.DTOs.Shared;
using Expense.IntegrationTests.Helpers;
using FluentAssertions;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace Expense.IntegrationTests.Settlements
{
    public class SettlementOverValidationTests : IntegrationTestBase
    {
        public SettlementOverValidationTests(CustomWebApplicationFactory<Expense.API.Program> factory) : base(factory)
        {
        }

        private async Task<(Guid GroupId, string TokenA, string TokenB, Guid CategoryId, string UserAId, string UserBId)> SetupGroupWithTwoUsersAsync()
        {
            var tokenA = await AuthenticateAsync("userA_over@test.com", "Password123!");
            var tokenB = await AuthenticateAsync("userB_over@test.com", "Password123!");

            _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokenA);
            var group = (await (await _client.PostAsJsonAsync("/api/groups", new CreateGroupDto { Name = "OverSettle Group" })).Content.ReadFromJsonAsync<APIResponse<GroupDto>>())!.Data!;

            _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokenB);
            await _client.PostAsync($"/api/groups/join/{group.InviteCode}", null);

            // Get Members to find IDs
            _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokenA);
            var groupDetails = (await (await _client.GetAsync($"/api/groups/{group.Id}")).Content.ReadFromJsonAsync<APIResponse<GroupDto>>())!.Data!;
            
            // User A is the Creator -> Admin
            var userAId = groupDetails.Members.First(m => m.Role == "Admin").UserId;
            var userBId = groupDetails.Members.First(m => m.UserId != userAId).UserId;

            var category = (await (await _client.PostAsJsonAsync($"/api/groups/{group.Id}/categories", new CreateExpenseCategoryDto { Name = "Food" })).Content.ReadFromJsonAsync<APIResponse<ExpenseCategoryDto>>())!.Data!;

            return (group.Id, tokenA, tokenB, category.Id, userAId, userBId);
        }

        [Fact]
        public async Task CreateSettlement_WhenPayerHasNoDebt_ShouldFail()
        {
            // Arrange
            var (groupId, tokenA, tokenB, _, userAId, userBId) = await SetupGroupWithTwoUsersAsync();
            // No expenses yet. Balance A: 0, B: 0.

            var settlementDto = new CreateSettlementDto
            {
                PayeeUserId = userBId,
                Amount = 50,
                SettlementDate = DateTime.UtcNow,
                Currency = "EGP"
            };

            // Act
            _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokenA);
            var response = await _client.PostAsJsonAsync($"/api/groups/{groupId}/settlements", settlementDto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest); 
        }

        [Fact]
        public async Task CreateSettlement_AmountExceedsDebt_ShouldFail()
        {
            // Arrange
            var (groupId, tokenA, tokenB, categoryId, userAId, userBId) = await SetupGroupWithTwoUsersAsync();
            
            // A pays 100, Split 50/50. A: +50, B: -50.
            await CreateExpenseAsync(groupId, tokenA, categoryId, 100, "Lunch");

            var settlementDto = new CreateSettlementDto
            {
                PayeeUserId = userAId, // Paying A
                Amount = 100, // Debt is only 50
                SettlementDate = DateTime.UtcNow,
                Currency = "EGP"
            };

            // Act
            _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokenB); // B pays
            var response = await _client.PostAsJsonAsync($"/api/groups/{groupId}/settlements", settlementDto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task CreateSettlement_ValidAmount_ShouldSucceed()
        {
            // Arrange
            var (groupId, tokenA, tokenB, categoryId, userAId, userBId) = await SetupGroupWithTwoUsersAsync();
            
            // A pays 100, Split 50/50. A: +50, B: -50.
            await CreateExpenseAsync(groupId, tokenA, categoryId, 100, "Lunch");

            var settlementDto = new CreateSettlementDto
            {
                PayeeUserId = userAId,
                Amount = 50,
                SettlementDate = DateTime.UtcNow,
                Currency = "EGP"
            };

            // Act
            _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokenB);
            var response = await _client.PostAsJsonAsync($"/api/groups/{groupId}/settlements", settlementDto);

            // Assert
            response.EnsureSuccessStatusCode();
        }

        private async Task CreateExpenseAsync(Guid groupId, string token, Guid categoryId, decimal amount, string desc)
        {
            _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            await _client.PostAsJsonAsync($"/api/groups/{groupId}/expenses", new CreateExpenseDto
            {
                Amount = amount,
                Description = desc,
                CategoryId = categoryId,
                ExpenseDate = DateTime.UtcNow
            });
        }
    }
}

