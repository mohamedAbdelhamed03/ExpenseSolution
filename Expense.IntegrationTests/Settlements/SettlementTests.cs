using System;
using System.Linq;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Expense.Core.DTOs.Auth;
using Expense.Core.DTOs.Groups;
using Expense.Core.DTOs.Expenses;
using Expense.Core.DTOs.Balances;
using Expense.Core.DTOs.Settlements;
// using Expense.Core.Common.Responses; // Removing invalid reference
using Expense.IntegrationTests.Helpers;
using FluentAssertions;
using Xunit;
using System.Collections.Generic;
using Expense.Core.DTOs.Shared; // APIResponse is likely here
using Expense.API; // Program is here

namespace Expense.IntegrationTests.Settlements
{
    public class SettlementTests : IntegrationTestBase
    {
        public SettlementTests(CustomWebApplicationFactory<Program> factory) : base(factory)
        {
        }

        [Fact]
        public async Task Settlement_Lifecycle_ShouldUpdateBalances()
        {
            // 1. Setup Users
            var token1 = await AuthenticateAsync("user1_settle@test.com", "Password123!");
            var user1Id = await GetUserIdAsync(token1);
            
            var token2 = await AuthenticateAsync("user2_settle@test.com", "Password123!");
            var user2Id = await GetUserIdAsync(token2);

            // 2. Create Group (User 1)
            _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token1);
            var groupResponse = await _client.PostAsJsonAsync("/api/groups", new CreateGroupDto 
            { 
                Name = "Settlement Group" 
            });
            var group = (await groupResponse.Content.ReadFromJsonAsync<APIResponse<GroupDto>>())!.Data!;

            // 3. User 2 joins
            _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token2);
            await _client.PostAsJsonAsync($"/api/groups/join/{group.InviteCode}", new { });

            // 4. User 1 pays for an expense (100) split equally
            // User 1 pays 100.
            // Share: User 1: 50, User 2: 50.
            // User 1 Balance: +50 (Paid 100 - Share 50)
            // User 2 Balance: -50 (Paid 0 - Share 50)
            _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token1);
            var expenseDto = new CreateExpenseDto
            {
                Amount = 100,
                Description = "Dinner",
                ExpenseDate = DateTime.UtcNow,
                Currency = "EGP"
            };
            var expenseResponse = await _client.PostAsJsonAsync($"/api/groups/{group.Id}/expenses", expenseDto);
            expenseResponse.EnsureSuccessStatusCode();

            // 5. Verify Balances before settlement
            var balanceResponse = await _client.GetAsync($"/api/groups/{group.Id}/balances");
            var balances = (await balanceResponse.Content.ReadFromJsonAsync<APIResponse<IEnumerable<BalanceDto>>>())!.Data!;
            
            var b1 = balances.First(b => b.UserId == user1Id);
            var b2 = balances.First(b => b.UserId == user2Id);

            b1.Balance.Should().Be(50);
            b2.Balance.Should().Be(-50);

            // 6. User 2 settles the debt (pays User 1 50)
            _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token2);
            var settlementDto = new CreateSettlementDto
            {
                PayeeUserId = user1Id,
                Amount = 50,
                SettlementDate = DateTime.UtcNow,
                Currency = "EGP"
            };
            
            var settlementResponse = await _client.PostAsJsonAsync($"/api/groups/{group.Id}/settlements", settlementDto);
            settlementResponse.EnsureSuccessStatusCode();

            // 7. Verify Balances after settlement
            // User 2 (Payer) sends 50. Balance increases by 50. (-50 + 50 = 0)
            // User 1 (Payee) receives 50. Balance decreases by 50. (50 - 50 = 0)
            balanceResponse = await _client.GetAsync($"/api/groups/{group.Id}/balances");
            balances = (await balanceResponse.Content.ReadFromJsonAsync<APIResponse<IEnumerable<BalanceDto>>>())!.Data!;
            
            b1 = balances.First(b => b.UserId == user1Id);
            b2 = balances.First(b => b.UserId == user2Id);

            b1.Balance.Should().Be(0);
            b2.Balance.Should().Be(0);
        }

        private async Task<string> GetUserIdAsync(string token)
        {
            // Helper to get user ID from profile endpoint
            _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            var response = await _client.GetAsync("/api/auth/me");
            // Assuming /api/auth/me returns LoginResponseDto or similar with Id/UserId
            var profile = (await response.Content.ReadFromJsonAsync<APIResponse<LoginResponseDto>>())!.Data!;
            return profile.UserId!;
        }
    }
}
