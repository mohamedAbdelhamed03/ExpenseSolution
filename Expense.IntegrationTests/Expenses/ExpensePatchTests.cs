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
    public class ExpensePatchTests : IntegrationTestBase
    {
        public ExpensePatchTests(CustomWebApplicationFactory<Expense.API.Program> factory) : base(factory)
        {
        }

        [Fact]
        public async Task PatchExpense_ShouldUpdatePartialFields_WhenValid()
        {
            // 1. Authenticate
            var token = await AuthenticateAsync("userA@expense.com", "Password123!");
            _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            // 2. Create Group
            var createGroupResponse = await _client.PostAsJsonAsync("/api/groups", new CreateGroupDto { Name = "Patch Test Group" });
            createGroupResponse.EnsureSuccessStatusCode();
            var group = (await createGroupResponse.Content.ReadFromJsonAsync<APIResponse<GroupDto>>())!.Data!;

            // 3. Create Categories
            var cat1Resp = await _client.PostAsJsonAsync($"/api/groups/{group.Id}/categories", new CreateExpenseCategoryDto { Name = "Cat1" });
            var cat1 = (await cat1Resp.Content.ReadFromJsonAsync<APIResponse<ExpenseCategoryDto>>())!.Data!;

            var cat2Resp = await _client.PostAsJsonAsync($"/api/groups/{group.Id}/categories", new CreateExpenseCategoryDto { Name = "Cat2" });
            var cat2 = (await cat2Resp.Content.ReadFromJsonAsync<APIResponse<ExpenseCategoryDto>>())!.Data!;

            // 4. Create Expense
            var createExpenseDto = new CreateExpenseDto
            {
                Amount = 100m,
                Description = "Original Description",
                CategoryId = cat1.Id,
                ExpenseDate = DateTime.UtcNow.Date,
                Splits = null
            };
            var createExpenseResponse = await _client.PostAsJsonAsync($"/api/groups/{group.Id}/expenses", createExpenseDto);
            createExpenseResponse.EnsureSuccessStatusCode();
            var expense = (await createExpenseResponse.Content.ReadFromJsonAsync<APIResponse<ExpenseDto>>())!.Data!;

            // 5. PATCH Description only
            var patchDesc = new UpdateExpensePatchDto { Description = "Updated Description" };
            // Note: Using SendAsync for PATCH because PostAsJsonAsync/PutAsJsonAsync are standard helpers, PatchAsJsonAsync is available in .NET 7+ or via extension
            // Since we are using standard HttpClient, we might need to construct HttpRequestMessage
            // Or use JsonContent.Create
            
            var patchRequest1 = new HttpRequestMessage(HttpMethod.Patch, $"/api/groups/{group.Id}/expenses/{expense.Id}")
            {
                Content = JsonContent.Create(patchDesc)
            };
            var patchResp1 = await _client.SendAsync(patchRequest1);
            patchResp1.EnsureSuccessStatusCode();
            var updatedExpense1 = (await patchResp1.Content.ReadFromJsonAsync<APIResponse<ExpenseDto>>())!.Data!;
            
            updatedExpense1.Description.Should().Be("Updated Description");
            updatedExpense1.CategoryId.Should().Be(cat1.Id); // Should remain unchanged
            updatedExpense1.Amount.Should().Be(100m);

            // 6. PATCH Category only
            var patchCat = new UpdateExpensePatchDto { CategoryId = cat2.Id };
            var patchRequest2 = new HttpRequestMessage(HttpMethod.Patch, $"/api/groups/{group.Id}/expenses/{expense.Id}")
            {
                Content = JsonContent.Create(patchCat)
            };
            var patchResp2 = await _client.SendAsync(patchRequest2);
            patchResp2.EnsureSuccessStatusCode();
            var updatedExpense2 = (await patchResp2.Content.ReadFromJsonAsync<APIResponse<ExpenseDto>>())!.Data!;

            updatedExpense2.CategoryId.Should().Be(cat2.Id);
            updatedExpense2.Description.Should().Be("Updated Description"); // Should remain from previous update

            // 7. PATCH Multiple fields
            var newDate = DateTime.UtcNow.AddDays(-1).Date;
            var patchMulti = new UpdateExpensePatchDto 
            { 
                Description = "Final Description",
                ExpenseDate = newDate
            };
            var patchRequest3 = new HttpRequestMessage(HttpMethod.Patch, $"/api/groups/{group.Id}/expenses/{expense.Id}")
            {
                Content = JsonContent.Create(patchMulti)
            };
            var patchResp3 = await _client.SendAsync(patchRequest3);
            patchResp3.EnsureSuccessStatusCode();
            var updatedExpense3 = (await patchResp3.Content.ReadFromJsonAsync<APIResponse<ExpenseDto>>())!.Data!;

            updatedExpense3.Description.Should().Be("Final Description");
            updatedExpense3.ExpenseDate.Should().Be(newDate);
            updatedExpense3.CategoryId.Should().Be(cat2.Id);
        }
    }
}