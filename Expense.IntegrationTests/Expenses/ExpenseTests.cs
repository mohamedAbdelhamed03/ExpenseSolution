using Expense.Core.DTOs.ActivityLogs;
using Expense.Core.DTOs.Balances;
using Expense.Core.DTOs.Categories;
using Expense.Core.DTOs.Expenses;
using Expense.Core.DTOs.Groups;
using Expense.Core.DTOs.Shared;
using Expense.IntegrationTests.Helpers;
using FluentAssertions;
using System.Net.Http.Json;
using Xunit;

namespace Expense.IntegrationTests.Expenses
{
    public class ExpenseTests : IntegrationTestBase
    {
        public ExpenseTests(CustomWebApplicationFactory<Expense.API.Program> factory) : base(factory)
        {
        }

        [Fact]
        public async Task ExpenseFlow_ShouldUpdateBalancesCorrectly()
        {
            // 1. Authenticate User A and User B
            var tokenA = await AuthenticateAsync("userA@expense.com", "Password123!");
            var tokenB = await AuthenticateAsync("userB@expense.com", "Password123!");

            // 2. User A creates a group
            _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokenA);
            var createGroupResponse = await _client.PostAsJsonAsync("/api/groups", new CreateGroupDto { Name = "Expense Test Group" });
            createGroupResponse.EnsureSuccessStatusCode();
            var group = (await createGroupResponse.Content.ReadFromJsonAsync<APIResponse<GroupDto>>())!.Data!;

            // 3. User B joins the group
            _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokenB);
            await _client.PostAsync($"/api/groups/join/{group.InviteCode}", null);

            // 4. User A creates a category
            _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokenA);
            var createCategoryResponse = await _client.PostAsJsonAsync($"/api/groups/{group.Id}/categories", new CreateExpenseCategoryDto { Name = "Food" });
            createCategoryResponse.EnsureSuccessStatusCode();
            var category = (await createCategoryResponse.Content.ReadFromJsonAsync<APIResponse<ExpenseCategoryDto>>())!.Data!;

            // 5. User A creates an expense ($100, Equal Split between A and B -> $50 each)
            var createExpenseDto = new CreateExpenseDto
            {
                Amount = 100m,
                Description = "Lunch",
                CategoryId = category.Id,
                ExpenseDate = DateTime.UtcNow,
                Splits = null // Trigger equal split
            };
            var createExpenseResponse = await _client.PostAsJsonAsync($"/api/groups/{group.Id}/expenses", createExpenseDto);
            createExpenseResponse.EnsureSuccessStatusCode();
            var expense = (await createExpenseResponse.Content.ReadFromJsonAsync<APIResponse<ExpenseDto>>())!.Data!;

            expense.Amount.Should().Be(100m);
            expense.Splits.Should().HaveCount(2);
            expense.Splits.All(s => s.Amount == 50m).Should().BeTrue();

            // 6. Verify Balances
            // User A Paid 100, Share 50. Balance = +50 (Owed 50)
            // User B Paid 0, Share 50. Balance = -50 (Owes 50)
            var balanceResponse = await _client.GetAsync($"/api/groups/{group.Id}/balances");
            balanceResponse.EnsureSuccessStatusCode();
            var balances = (await balanceResponse.Content.ReadFromJsonAsync<APIResponse<IEnumerable<BalanceDto>>>())!.Data!;

            var balanceA = balances.First(b => b.TotalPaid > 0); // User A
            var balanceB = balances.First(b => b.TotalPaid == 0); // User B

            balanceA.TotalPaid.Should().Be(100m);
            balanceA.TotalShared.Should().Be(50m);
            balanceA.Balance.Should().Be(50m);

            balanceB.TotalPaid.Should().Be(0m);
            balanceB.TotalShared.Should().Be(50m);
            balanceB.Balance.Should().Be(-50m);

            // 7. User A updates the expense (Amount 200)
            // Re-split equally: $100 each.
            var updateExpenseDto = new UpdateExpenseDto
            {
                Amount = 200m,
                Description = "Big Lunch",
                CategoryId = category.Id,
                ExpenseDate = DateTime.UtcNow,
                Splits = null // Trigger equal split
            };
            var updateResponse = await _client.PutAsJsonAsync($"/api/groups/{group.Id}/expenses/{expense.Id}", updateExpenseDto);
            updateResponse.EnsureSuccessStatusCode();
            var updatedExpense = (await updateResponse.Content.ReadFromJsonAsync<APIResponse<ExpenseDto>>())!.Data!;

            updatedExpense.Amount.Should().Be(200m);
            updatedExpense.Splits.All(s => s.Amount == 100m).Should().BeTrue();

            // 8. Verify Balances (Updated)
            // User A Paid 200, Share 100. Balance = +100
            // User B Paid 0, Share 100. Balance = -100
            balanceResponse = await _client.GetAsync($"/api/groups/{group.Id}/balances");
            balances = (await balanceResponse.Content.ReadFromJsonAsync<APIResponse<IEnumerable<BalanceDto>>>())!.Data!;

            balanceA = balances.First(b => b.TotalPaid > 0);
            balanceB = balances.First(b => b.TotalPaid == 0);

            balanceA.Balance.Should().Be(100m);
            balanceB.Balance.Should().Be(-100m);

            // 9. User A deletes the expense
            var deleteResponse = await _client.DeleteAsync($"/api/groups/{group.Id}/expenses/{expense.Id}");
            deleteResponse.EnsureSuccessStatusCode();

            // 10. Verify Balances (Reverted to 0)
            balanceResponse = await _client.GetAsync($"/api/groups/{group.Id}/balances");
            balances = (await balanceResponse.Content.ReadFromJsonAsync<APIResponse<IEnumerable<BalanceDto>>>())!.Data!;

            balances.All(b => b.Balance == 0).Should().BeTrue();
            balances.All(b => b.TotalPaid == 0).Should().BeTrue();

            // 11. Verify Activity Logs
            var logsResponse = await _client.GetAsync($"/api/groups/{group.Id}/activities");
            logsResponse.EnsureSuccessStatusCode();
            var logs = (await logsResponse.Content.ReadFromJsonAsync<APIResponse<IEnumerable<ActivityLogDto>>>())!.Data!;

            Assert.True(logs.Count() >= 3);
            logs.Should().Contain(l => l.Action == "Created" && l.EntityType == "Expense");
            logs.Should().Contain(l => l.Action == "Updated" && l.EntityType == "Expense");
            logs.Should().Contain(l => l.Action == "Deleted" && l.EntityType == "Expense");
        }
    }
}
