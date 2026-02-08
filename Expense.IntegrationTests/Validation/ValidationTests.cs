using System;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Expense.Core.DTOs.Groups;
using Expense.Core.DTOs.Expenses;
using Expense.Core.DTOs.Settlements;
using Expense.IntegrationTests.Helpers;
using FluentAssertions;
using Xunit;
using Expense.Core.DTOs.Shared;

namespace Expense.IntegrationTests.Validation
{
    public class ValidationTests : IntegrationTestBase
    {
        public ValidationTests(CustomWebApplicationFactory<Expense.API.Program> factory) : base(factory)
        {
        }

        [Fact]
        public async Task CreateExpense_WithInvalidData_ShouldReturnBadRequest()
        {
            // 1. Setup
            var token = await AuthenticateAsync("user_val@test.com", "Password123!");
            _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            var groupResponse = await _client.PostAsJsonAsync("/api/groups", new CreateGroupDto { Name = "Val Group" });
            var group = (await groupResponse.Content.ReadFromJsonAsync<APIResponse<GroupDto>>())!.Data!;

            // 2. Test Invalid Currency
            var invalidCurrencyDto = new CreateExpenseDto
            {
                Amount = 100,
                Description = "Test",
                ExpenseDate = DateTime.UtcNow,
                Currency = "XX" // Too short
            };
            var response = await _client.PostAsJsonAsync($"/api/groups/{group.Id}/expenses", invalidCurrencyDto);
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            var error = await response.Content.ReadFromJsonAsync<APIResponse<object>>();
            error!.Success.Should().BeFalse();
            // Verify specific error message if possible, but status code is sufficient for now

            // 3. Test Negative Amount
            var negativeAmountDto = new CreateExpenseDto
            {
                Amount = -100,
                Description = "Test",
                ExpenseDate = DateTime.UtcNow,
                Currency = "USD"
            };
            response = await _client.PostAsJsonAsync($"/api/groups/{group.Id}/expenses", negativeAmountDto);
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

            // 4. Test Future Date
            var futureDateDto = new CreateExpenseDto
            {
                Amount = 100,
                Description = "Test",
                ExpenseDate = DateTime.UtcNow.AddDays(10), // Future + 10
                Currency = "USD"
            };
            response = await _client.PostAsJsonAsync($"/api/groups/{group.Id}/expenses", futureDateDto);
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        private async Task<string> GetUserIdAsync(string token)
        {
             // We can decode the token or just fetch profile if there is an endpoint
             // Or we can rely on the fact that AuthenticateAsync sets the auth header
             // and call a "Get Me" endpoint if it exists.
             // Or assume we don't need user ID if we can get it from another way.
             // But for Settlement we need PayeeUserId.
             
             // Let's implement a simple helper here or use the one from SettlementTests if it had one.
             // SettlementTests likely got user ID differently.
             // Let's check how SettlementTests did it.
             // But for now, I'll just decode the token or call /api/auth/me if it existed (it doesn't).
             
             // Actually, I can just register and get the ID if the register response returns it.
             // But AuthenticateAsync returns token.
             
             // Let's just assume we can get it from a protected endpoint that returns user info.
             // Or, better, let's just parse the JWT if possible, but that requires a library.
             
             // A workaround: Register returns success. Login returns token.
             // Maybe we can add a helper to IntegrationTestBase later.
             // For now, I'll use a known trick: Create a group, verify I am a member, and get my ID from the member list.
             
            _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            var groupResponse = await _client.PostAsJsonAsync("/api/groups", new CreateGroupDto { Name = "Temp Group" });
            var group = (await groupResponse.Content.ReadFromJsonAsync<APIResponse<GroupDto>>())!.Data!;
            return group.Members.First(m => m.Role == "Admin").UserId;
        }

        [Fact]
        public async Task CreateSettlement_WithInvalidData_ShouldReturnBadRequest()
        {
            // 1. Setup
            var token1 = await AuthenticateAsync("user1_val@test.com", "Password123!");
            var user1Id = await GetUserIdAsync(token1);
            
            var token2 = await AuthenticateAsync("user2_val@test.com", "Password123!");
            var user2Id = await GetUserIdAsync(token2);

            _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token1);
            var groupResponse = await _client.PostAsJsonAsync("/api/groups", new CreateGroupDto { Name = "Val Group Settle" });
            var group = (await groupResponse.Content.ReadFromJsonAsync<APIResponse<GroupDto>>())!.Data!;

            _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token2);
            await _client.PostAsJsonAsync($"/api/groups/join/{group.InviteCode}", new { });

            // 2. Test Invalid Currency
            var invalidCurrencyDto = new CreateSettlementDto
            {
                PayeeUserId = user1Id,
                Amount = 50,
                SettlementDate = DateTime.UtcNow,
                Currency = "INVALID"
            };
            var response = await _client.PostAsJsonAsync($"/api/groups/{group.Id}/settlements", invalidCurrencyDto);
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

            // 3. Test Negative Amount
            var negativeAmountDto = new CreateSettlementDto
            {
                PayeeUserId = user1Id,
                Amount = -50,
                SettlementDate = DateTime.UtcNow,
                Currency = "USD"
            };
            response = await _client.PostAsJsonAsync($"/api/groups/{group.Id}/settlements", negativeAmountDto);
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

            // 4. Test Missing Payee
            var missingPayeeDto = new CreateSettlementDto
            {
                PayeeUserId = "",
                Amount = 50,
                SettlementDate = DateTime.UtcNow,
                Currency = "USD"
            };
            response = await _client.PostAsJsonAsync($"/api/groups/{group.Id}/settlements", missingPayeeDto);
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

            // 5. Test Future Date
            var futureDateDto = new CreateSettlementDto
            {
                PayeeUserId = user1Id,
                Amount = 50,
                SettlementDate = DateTime.UtcNow.AddDays(10),
                Currency = "USD"
            };
            response = await _client.PostAsJsonAsync($"/api/groups/{group.Id}/settlements", futureDateDto);
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }
    }
}
