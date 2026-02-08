using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Expense.API;
using Expense.Core.DTOs.Expenses;
using Expense.Core.DTOs.Groups;
using Expense.Core.DTOs.Notifications;
using Expense.Core.DTOs.Shared;
using Expense.IntegrationTests.Helpers;
using FluentAssertions;
using Xunit;

namespace Expense.IntegrationTests.Notifications
{
    public class NotificationIntegrationTests : IntegrationTestBase
    {
        public NotificationIntegrationTests(CustomWebApplicationFactory<Program> factory) : base(factory)
        {
        }

        [Fact]
        public async Task CreateExpense_ShouldGenerateNotification_And_PersistIt()
        {
            // Arrange
            // 1. Create User 1 (Payer)
            var token1 = await AuthenticateAsync("user1@example.com", "Password123!");
            
            // 2. Create User 2 (Member)
            var token2 = await AuthenticateAsync("user2@example.com", "Password123!");
            
            // 3. Create Group as User 1
            _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token1);
            var createGroupDto = new CreateGroupDto { Name = "Notification Test Group" };
            var groupResponse = await _client.PostAsJsonAsync("/api/groups", createGroupDto);
            groupResponse.EnsureSuccessStatusCode();
            var groupResult = await groupResponse.Content.ReadFromJsonAsync<APIResponse<GroupDto>>();
            groupResult.Data.Should().NotBeNull();
            var groupId = groupResult.Data.Id;
            var inviteCode = groupResult.Data.InviteCode;

            // 4. Join Group as User 2
            _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token2);
            var joinResponse = await _client.PostAsync($"/api/groups/join/{inviteCode}", null);
            joinResponse.EnsureSuccessStatusCode();

            // 5. Create Expense as User 1
            _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token1);
            
            // Get User 2 ID
            // We can get it from group members
            var groupGetResponse = await _client.GetFromJsonAsync<APIResponse<GroupDto>>($"/api/groups/{groupId}");
            groupGetResponse.Data.Should().NotBeNull();
            var user2Id = groupGetResponse.Data.Members.First(m => m.Role == "Member").UserId;
            var user1Id = groupGetResponse.Data.Members.First(m => m.Role == "Admin").UserId;

            var expenseDto = new CreateExpenseDto
            {
                Amount = 100,
                Description = "Test Notification",
                ExpenseDate = DateTime.UtcNow,
                Currency = "USD",
                Splits = new List<ExpenseSplitDto>
                {
                    new ExpenseSplitDto { UserId = user1Id, Amount = 50 }, // User 1 pays 50 (share)
                    new ExpenseSplitDto { UserId = user2Id, Amount = 50 }  // User 2 pays 50 (share)
                }
            };

            var expenseResponse = await _client.PostAsJsonAsync($"/api/groups/{groupId}/expenses", expenseDto);
            expenseResponse.EnsureSuccessStatusCode();

            // Act: Check Notifications for User 2
            _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token2);
            var notificationResponse = await _client.GetFromJsonAsync<APIResponse<IEnumerable<NotificationDto>>>("/api/notifications/unread");

            // Assert
            notificationResponse.Should().NotBeNull();
            notificationResponse.Data.Should().NotBeEmpty();
            var notification = notificationResponse.Data.First();
            notification.Type.Should().Be("Expense_Created");
            notification.Payload.Should().Contain("Test Notification");
            notification.IsRead.Should().BeFalse();

            // Act 2: Mark as Read
            var markReadDto = new MarkReadDto { NotificationIds = new List<Guid> { notification.Id } };
            var markReadResponse = await _client.PostAsJsonAsync("/api/notifications/mark-read", markReadDto);
            markReadResponse.EnsureSuccessStatusCode();

            // Assert 2: Should be empty now
            var notificationResponseAfter = await _client.GetFromJsonAsync<APIResponse<IEnumerable<NotificationDto>>>("/api/notifications/unread");
            notificationResponseAfter.Data.Should().BeEmpty();
        }
    }
}
