using Expense.Core.DTOs.Groups;
using Expense.Core.DTOs.Shared;
using Expense.IntegrationTests.Helpers;
using FluentAssertions;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace Expense.IntegrationTests.Groups
{
    public class GroupTests : IntegrationTestBase
    {
        public GroupTests(CustomWebApplicationFactory<Expense.API.Program> factory) : base(factory)
        {
        }

        [Fact]
        public async Task GroupFlow_ShouldWorkCorrectly()
        {
            // 1. Authenticate User A
            var tokenA = await AuthenticateAsync("userA@test.com", "Password123!");

            // 2. User A creates a group
            var createGroupDto = new CreateGroupDto { Name = "Test Group" };
            var createResponse = await _client.PostAsJsonAsync("/api/groups", createGroupDto);
            createResponse.EnsureSuccessStatusCode();
            var groupData = await createResponse.Content.ReadFromJsonAsync<APIResponse<GroupDto>>();
            var group = groupData!.Data!;

            group.Should().NotBeNull();
            group.Name.Should().Be("Test Group");
            group.InviteCode.Should().NotBeNullOrEmpty();
            group.Members.Should().Contain(m => m.UserId != null && m.Role == "Admin");

            // 3. Authenticate User B
            _client.DefaultRequestHeaders.Authorization = null; // Clear auth
            var tokenB = await AuthenticateAsync("userB@test.com", "Password123!");

            // 4. User B joins the group
            var joinResponse = await _client.PostAsync($"/api/groups/join/{group.InviteCode}", null);
            joinResponse.EnsureSuccessStatusCode();

            // 5. User A checks members
            _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokenA);
            var getGroupResponse = await _client.GetAsync($"/api/groups/{group.Id}");
            getGroupResponse.EnsureSuccessStatusCode();
            var getGroupData = await getGroupResponse.Content.ReadFromJsonAsync<APIResponse<GroupDto>>();
            
            getGroupData!.Data!.Members.Should().HaveCount(2);
            var memberB = getGroupData.Data.Members.FirstOrDefault(m => m.Role == "Member");
            memberB.Should().NotBeNull();

            // 6. User A promotes User B to Admin
            var updateRoleDto = new UpdateGroupMemberRoleDto { Role = "Admin" };
            var updateResponse = await _client.PutAsJsonAsync($"/api/groups/{group.Id}/members/{memberB!.UserId}", updateRoleDto);
            updateResponse.EnsureSuccessStatusCode();

            // 7. Verify promotion
            getGroupResponse = await _client.GetAsync($"/api/groups/{group.Id}");
            getGroupData = await getGroupResponse.Content.ReadFromJsonAsync<APIResponse<GroupDto>>();
            getGroupData!.Data!.Members.First(m => m.UserId == memberB.UserId).Role.Should().Be("Admin");

            // 8. User B (now Admin) removes User A
            _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokenB);
            var userAId = getGroupData.Data.Members.First(m => m.UserId != memberB.UserId).UserId;
            var removeResponse = await _client.DeleteAsync($"/api/groups/{group.Id}/members/{userAId}");
            removeResponse.EnsureSuccessStatusCode();

            // 9. Verify removal
            getGroupResponse = await _client.GetAsync($"/api/groups/{group.Id}");
            getGroupData = await getGroupResponse.Content.ReadFromJsonAsync<APIResponse<GroupDto>>();
            getGroupData!.Data!.Members.Should().HaveCount(1);
        }

        [Fact]
        public async Task GetUserGroups_ShouldReturnOnlyUserGroups()
        {
             // 1. Authenticate User C
            var tokenC = await AuthenticateAsync("userC@test.com", "Password123!");

            // 2. Create Group 1
            await _client.PostAsJsonAsync("/api/groups", new CreateGroupDto { Name = "Group 1" });
            // 3. Create Group 2
            await _client.PostAsJsonAsync("/api/groups", new CreateGroupDto { Name = "Group 2" });

            // 4. Get User Groups
            var response = await _client.GetAsync("/api/groups");
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<APIResponse<IEnumerable<GroupDto>>>();

            result!.Data.Should().HaveCount(2);
            result.Data.Should().Contain(g => g.Name == "Group 1");
            result.Data.Should().Contain(g => g.Name == "Group 2");
        }
    }
}