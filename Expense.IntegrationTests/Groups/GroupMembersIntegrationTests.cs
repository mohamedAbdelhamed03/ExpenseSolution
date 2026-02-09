using Expense.Core.DTOs.Auth;
using Expense.Core.DTOs.Groups;
using Expense.Core.DTOs.Shared;
using Expense.IntegrationTests.Helpers;
using FluentAssertions;
using System;
using System.Linq;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Xunit;

namespace Expense.IntegrationTests.Groups
{
    public class GroupMembersIntegrationTests : IntegrationTestBase
    {
        public GroupMembersIntegrationTests(CustomWebApplicationFactory<Expense.API.Program> factory) : base(factory)
        {
        }

        [Fact]
        public async Task AddMemberByEmail_ShouldAddExistingUserToGroup()
        {
            // 1. Ensure User B exists
            var userBEmail = "member@test.com";
            await _client.PostAsJsonAsync("/api/auth/register", new RegisterDto 
            { 
                Email = userBEmail, 
                Password = "Password123!",
                ConfirmPassword = "Password123!",
                FirstName = "Member",
                LastName = "User"
            });

            // 2. Authenticate as User A
            await AuthenticateAsync("admin@test.com", "Password123!");

            // 3. Create Group
            var groupResponse = await _client.PostAsJsonAsync("/api/groups", new CreateGroupDto 
            { 
                Name = "Test Group"
            });
            groupResponse.EnsureSuccessStatusCode();
            var groupResult = await groupResponse.Content.ReadFromJsonAsync<APIResponse<GroupDto>>();
            var groupId = groupResult.Data.Id;

            // 4. Add User B by Email
            var addResponse = await _client.PostAsJsonAsync($"/api/groups/{groupId}/members", new AddGroupMemberDto 
            { 
                Email = userBEmail 
            });
            
            addResponse.EnsureSuccessStatusCode();

            // 5. Verify User B is in the group
            var getGroupResponse = await _client.GetAsync($"/api/groups/{groupId}");
            getGroupResponse.EnsureSuccessStatusCode();
            var getGroupResult = await getGroupResponse.Content.ReadFromJsonAsync<APIResponse<GroupDto>>();
            
            getGroupResult.Data.Members.Should().Contain(m => m.UserId != null); // We don't know User B's ID easily without logging in as them or checking DB, but we can check count or assume 2 members
            getGroupResult.Data.Members.Should().HaveCount(2);
        }

        [Fact]
        public async Task AddMemberByEmail_ShouldFail_IfUserDoesNotExist()
        {
             // 1. Authenticate as User A
            await AuthenticateAsync("admin2@test.com", "Password123!");

            // 2. Create Group
            var groupResponse = await _client.PostAsJsonAsync("/api/groups", new CreateGroupDto 
            { 
                Name = "Test Group 2"
            });
            groupResponse.EnsureSuccessStatusCode();
            var groupResult = await groupResponse.Content.ReadFromJsonAsync<APIResponse<GroupDto>>();
            var groupId = groupResult.Data.Id;

            // 3. Add Non-existent User
            var addResponse = await _client.PostAsJsonAsync($"/api/groups/{groupId}/members", new AddGroupMemberDto 
            { 
                Email = "nonexistent@test.com" 
            });
            
            addResponse.IsSuccessStatusCode.Should().BeFalse();
            addResponse.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
        }
    }
}
