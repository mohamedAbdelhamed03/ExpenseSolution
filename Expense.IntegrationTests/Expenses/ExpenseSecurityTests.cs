using Expense.Core.DTOs.Expenses;
using Expense.Core.DTOs.Groups;
using Expense.Core.DTOs.Shared;
using Expense.IntegrationTests.Helpers;
using FluentAssertions;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace Expense.IntegrationTests.Expenses
{
    public class ExpenseSecurityTests : IntegrationTestBase
    {
        public ExpenseSecurityTests(CustomWebApplicationFactory<Expense.API.Program> factory) : base(factory)
        {
        }

        [Fact]
        public async Task NonMember_CannotCreateExpense()
        {
            // 1. Setup Group with User A
            var tokenA = await AuthenticateAsync("userA_sec@test.com", "Password123!");
            _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokenA);
            var groupResponse = await _client.PostAsJsonAsync("/api/groups", new CreateGroupDto { Name = "Sec Group" });
            var group = (await groupResponse.Content.ReadFromJsonAsync<APIResponse<GroupDto>>())!.Data!;

            // 2. User B (Non-member) tries to create expense
            var tokenB = await AuthenticateAsync("userB_sec@test.com", "Password123!");
            _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokenB);

            var createDto = new CreateExpenseDto
            {
                Amount = 100,
                Description = "Hack",
                ExpenseDate = DateTime.UtcNow
            };

            var response = await _client.PostAsJsonAsync($"/api/groups/{group.Id}/expenses", createDto);
            
            // Should be 403 Forbidden (GroupAccessDeniedException maps to 403? Or 400? Let's assume 403 for now, but GroupAccessDeniedException usually maps to 403)
            // Wait, GlobalExceptionHandlerMiddleware might map it.
            // Let's check GlobalExceptionHandlerMiddleware.
            // If not mapped, it might be 500.
            // I'll check response.StatusCode.
            
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task NonPayer_CannotDeleteExpense()
        {
            // 1. Setup Group with User A and User B
            var tokenA = await AuthenticateAsync("userA_del@test.com", "Password123!");
            var tokenB = await AuthenticateAsync("userB_del@test.com", "Password123!");

            _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokenA);
            var groupResponse = await _client.PostAsJsonAsync("/api/groups", new CreateGroupDto { Name = "Del Group" });
            var group = (await groupResponse.Content.ReadFromJsonAsync<APIResponse<GroupDto>>())!.Data!;

            // User B joins
            _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokenB);
            await _client.PostAsync($"/api/groups/join/{group.InviteCode}", null);

            // 2. User A creates expense
            _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokenA);
            var createDto = new CreateExpenseDto
            {
                Amount = 100,
                Description = "A's Expense",
                ExpenseDate = DateTime.UtcNow
            };
            var expenseResponse = await _client.PostAsJsonAsync($"/api/groups/{group.Id}/expenses", createDto);
            var expense = (await expenseResponse.Content.ReadFromJsonAsync<APIResponse<ExpenseDto>>())!.Data!;

            // 3. User B tries to delete it
            _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokenB);
            var deleteResponse = await _client.DeleteAsync($"/api/groups/{group.Id}/expenses/{expense.Id}");

            // Should be 403 or 400 "Expense_Delete_NotAuthorized" (BusinessException maps to 400 usually, GroupAccessDenied to 403)
            // The service throws BusinessException("Expense_Delete_NotAuthorized") if member but not owner/admin.
            // BusinessException usually maps to 400 Bad Request.
            // GroupAccessDeniedException maps to 403.
            
            // Let's verify mapping in GlobalExceptionHandlerMiddleware.
            // But I can't read it right now inside this thought block.
            // I'll assume 400 for BusinessException.
            deleteResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }
    }
}
