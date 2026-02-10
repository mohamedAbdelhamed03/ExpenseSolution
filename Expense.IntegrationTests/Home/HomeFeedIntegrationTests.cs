using Expense.API;
using Expense.Core.DTOs.Auth;
using Expense.Core.DTOs.Expenses;
using Expense.Core.DTOs.Groups;
using Expense.Core.DTOs.Home;
using Expense.Core.DTOs.Settlements;
using Expense.Core.DTOs.Shared;
using Expense.Core.DTOs.Personal;
using Expense.IntegrationTests.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Xunit;

namespace Expense.IntegrationTests.Home
{
    public class HomeFeedIntegrationTests : IntegrationTestBase
    {
        public HomeFeedIntegrationTests(CustomWebApplicationFactory<Program> factory) : base(factory)
        {
        }

        [Fact]
        public async Task GetFeed_ShouldReturnCorrectItems_ForExpensesAndSettlements()
        {
            // 1. Setup Users
            var (userAId, tokenA, emailA) = await RegisterAndLoginAsync("usera@example.com");
            var (userBId, tokenB, emailB) = await RegisterAndLoginAsync("userb@example.com");

            // 2. Setup Group (User A creates)
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenA);
            var createGroupResponse = await _client.PostAsJsonAsync("/api/groups", new CreateGroupDto 
            { 
                Name = "Test Group"
            });
            createGroupResponse.EnsureSuccessStatusCode();
            var group = (await createGroupResponse.Content.ReadFromJsonAsync<APIResponse<GroupDto>>())!.Data!;
            var groupId = group.Id;

            // 3. Add User B to Group (via Add Member by Email)
            var addMemberResponse = await _client.PostAsJsonAsync($"/api/groups/{groupId}/members", new AddGroupMemberDto 
            { 
                Email = emailB 
            });
            addMemberResponse.EnsureSuccessStatusCode();

            // 4. User A creates Expense ($100, split 50/50)
            var createExpenseResponse = await _client.PostAsJsonAsync($"/api/groups/{groupId}/expenses", new CreateExpenseDto
            {
                Amount = 100,
                Currency = "USD",
                Description = "Dinner",
                ExpenseDate = DateTime.UtcNow,
                Splits = new List<ExpenseSplitDto>
                {
                    new ExpenseSplitDto { UserId = userAId, Amount = 50 },
                    new ExpenseSplitDto { UserId = userBId, Amount = 50 }
                }
            });
            createExpenseResponse.EnsureSuccessStatusCode();

            // 5. User B pays Settlement to User A ($50)
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenB);
            var createSettlementResponse = await _client.PostAsJsonAsync($"/api/groups/{groupId}/settlements", new CreateSettlementDto
            {
                Amount = 50,
                Currency = "USD",
                PayeeUserId = userAId,
                SettlementDate = DateTime.UtcNow
            });
            createSettlementResponse.EnsureSuccessStatusCode();

            // 6. Verify Feed A
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenA);
            var feedAResponse = await _client.GetAsync("/api/home");
            feedAResponse.EnsureSuccessStatusCode();
            var feedA = (await feedAResponse.Content.ReadFromJsonAsync<APIResponse<IEnumerable<HomeFeedItemDto>>>())!.Data!;

            Assert.Contains(feedA, i => i.Type == HomeFeedType.Expense && i.Amount == 100 && i.Direction == HomeFeedDirection.Out);
            Assert.Contains(feedA, i => i.Type == HomeFeedType.Settlement && i.Amount == 50 && i.Direction == HomeFeedDirection.In);

            // 7. Verify Feed B
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenB);
            var feedBResponse = await _client.GetAsync("/api/home");
            feedBResponse.EnsureSuccessStatusCode();
            var feedB = (await feedBResponse.Content.ReadFromJsonAsync<APIResponse<IEnumerable<HomeFeedItemDto>>>())!.Data!;

            // User B owes 50 for the expense (since A paid 100, B's share is 50)
            Assert.Contains(feedB, i => i.Type == HomeFeedType.Expense && i.Amount == 50 && i.Direction == HomeFeedDirection.Neutral);
            Assert.Contains(feedB, i => i.Type == HomeFeedType.Settlement && i.Amount == 50 && i.Direction == HomeFeedDirection.Out);
        }

        [Fact]
        public async Task GetFeed_ShouldIncludePersonalExpenses()
        {
            // 1. Setup User
            var (userId, token, email) = await RegisterAndLoginAsync("userc@example.com");
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            // 2. Create Personal Expense
            var createResponse = await _client.PostAsJsonAsync("/api/personal-expenses", new CreatePersonalExpenseDto
            {
                Amount = 75.5m,
                Currency = "USD",
                Date = DateTime.UtcNow,
                Description = "Personal Lunch"
            });
            createResponse.EnsureSuccessStatusCode();

            // 3. Verify Feed
            var feedResponse = await _client.GetAsync("/api/home");
            feedResponse.EnsureSuccessStatusCode();
            var feed = (await feedResponse.Content.ReadFromJsonAsync<APIResponse<IEnumerable<HomeFeedItemDto>>>())!.Data!;

            Assert.Contains(feed, i => 
                i.Type == HomeFeedType.PersonalExpense && 
                i.Amount == 75.5m && 
                i.Description == "Personal Lunch" &&
                i.Direction == HomeFeedDirection.Out);
        }

        private async Task<(string UserId, string Token, string Email)> RegisterAndLoginAsync(string email)
        {
            var password = "Password123!";
            
            // Ensure unique email
            if (email.Contains("@") && !email.Contains("+"))
            {
                var parts = email.Split('@');
                email = $"{parts[0]}+{Guid.NewGuid():N}@{parts[1]}";
            }

            await _client.PostAsJsonAsync("/api/auth/register", new RegisterDto 
            { 
                Email = email, 
                Password = password,
                ConfirmPassword = password,
                FirstName = "Test",
                LastName = "User"
            });

            var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new LoginDto 
            { 
                Email = email, 
                Password = password 
            });
            
            loginResponse.EnsureSuccessStatusCode();
            var result = await loginResponse.Content.ReadFromJsonAsync<APIResponse<LoginResponseDto>>();
            
            return (result!.Data!.UserId!, result.Data.AccessToken!, email);
        }
    }
}
