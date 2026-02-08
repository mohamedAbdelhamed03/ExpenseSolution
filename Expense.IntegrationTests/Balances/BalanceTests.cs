using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Expense.Core.DTOs.Auth;
using Expense.Core.DTOs.Balances;
using Expense.Core.DTOs.Expenses;
using Expense.Core.DTOs.Groups;
using Expense.Core.DTOs.Settlements;
using Expense.Core.DTOs.Shared;
using Expense.IntegrationTests.Helpers;
using FluentAssertions;
using Xunit;
using Expense.API;

namespace Expense.IntegrationTests.Balances
{
    public class BalanceTests : IntegrationTestBase
    {
        public BalanceTests(CustomWebApplicationFactory<Program> factory) : base(factory)
        {
        }

        [Fact]
        public async Task Complex_Scenario_Balances_Should_Be_Correct_With_Multiple_Expenses_And_Settlements()
        {
            // 1. Setup Users A, B, C
            var tokenA = await AuthenticateAsync("userA@test.com", "Password123!");
            var userAId = await GetUserIdAsync(tokenA);

            var tokenB = await AuthenticateAsync("userB@test.com", "Password123!");
            var userBId = await GetUserIdAsync(tokenB);

            var tokenC = await AuthenticateAsync("userC@test.com", "Password123!");
            var userCId = await GetUserIdAsync(tokenC);

            // 2. Create Group (User A)
            _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokenA);
            var groupResponse = await _client.PostAsJsonAsync("/api/groups", new CreateGroupDto { Name = "Complex Balance Group" });
            var group = (await groupResponse.Content.ReadFromJsonAsync<APIResponse<GroupDto>>())!.Data!;

            // 3. Users B and C join
            _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokenB);
            await _client.PostAsJsonAsync($"/api/groups/join/{group.InviteCode}", new { });

            _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokenC);
            await _client.PostAsJsonAsync($"/api/groups/join/{group.InviteCode}", new { });

            // 4. Expense 1: User A pays 120 for A, B, C (Equal split: 40 each)
            // A: +80, B: -40, C: -40
            _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokenA);
            var expense1 = new CreateExpenseDto
            {
                Amount = 120,
                Description = "Lunch",
                ExpenseDate = DateTime.UtcNow,
                Currency = "EGP"
            };
            var resp1 = await _client.PostAsJsonAsync($"/api/groups/{group.Id}/expenses", expense1);
            resp1.EnsureSuccessStatusCode();

            // 5. Expense 2: User B pays 60 for A, B (Equal split: 30 each)
            // B pays 60.
            // A owes 30. B owes 30 (to himself).
            // B's net change: Paid 60 - Consumed 30 = +30.
            // A's net change: Paid 0 - Consumed 30 = -30.
            // Cumulative:
            // A: +80 - 30 = +50
            // B: -40 + 30 = -10
            // C: -40 + 0 = -40
            _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokenB);
            var expense2 = new CreateExpenseDto
            {
                Amount = 60,
                Description = "Drinks",
                ExpenseDate = DateTime.UtcNow,
                Currency = "EGP",
                Splits = new List<ExpenseSplitDto>
                {
                    new ExpenseSplitDto { UserId = userAId, Amount = 30 },
                    new ExpenseSplitDto { UserId = userBId, Amount = 30 }
                }
            };
            var resp2 = await _client.PostAsJsonAsync($"/api/groups/{group.Id}/expenses", expense2);
            resp2.EnsureSuccessStatusCode();

            // Check Balances Intermediate
            var balResp = await _client.GetAsync($"/api/groups/{group.Id}/balances");
            var balances = (await balResp.Content.ReadFromJsonAsync<APIResponse<IEnumerable<BalanceDto>>>())!.Data!;
            
            balances.First(b => b.UserId == userAId).Balance.Should().Be(50);
            balances.First(b => b.UserId == userBId).Balance.Should().Be(-10);
            balances.First(b => b.UserId == userCId).Balance.Should().Be(-40);

            // 6. Settlement: User C pays User A 40
            // C: -40 + 40 = 0
            // A: 50 - 40 = 10
            // B: -10 (Unchanged)
            _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokenC);
            var settlement = new CreateSettlementDto
            {
                PayeeUserId = userAId,
                Amount = 40,
                SettlementDate = DateTime.UtcNow,
                Currency = "EGP"
            };
            var setResp = await _client.PostAsJsonAsync($"/api/groups/{group.Id}/settlements", settlement);
            setResp.EnsureSuccessStatusCode();

            // 7. Verify Final Balances
            balResp = await _client.GetAsync($"/api/groups/{group.Id}/balances");
            balances = (await balResp.Content.ReadFromJsonAsync<APIResponse<IEnumerable<BalanceDto>>>())!.Data!;

            balances.First(b => b.UserId == userAId).Balance.Should().Be(10);
            balances.First(b => b.UserId == userBId).Balance.Should().Be(-10);
            balances.First(b => b.UserId == userCId).Balance.Should().Be(0);
        }

        private async Task<string> GetUserIdAsync(string token)
        {
            _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            var response = await _client.GetAsync("/api/auth/me");
            response.EnsureSuccessStatusCode();
            
            // Deserialize anonymously or create a small DTO if strict typing needed.
            // Using dynamic or JsonNode is easiest if APIResponse is generic object.
            // But we know the structure from AuthController: Data = { UserId, Email, Roles }
            var json = await response.Content.ReadFromJsonAsync<System.Text.Json.Nodes.JsonNode>();
            return json!["data"]!["userId"]!.GetValue<string>();
        }
        
        // Wait, IntegrationTestBase usually has this? 
        // Let's check IntegrationTestBase.
    }
}
