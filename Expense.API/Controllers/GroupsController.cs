using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Expense.Core.Application.Groups;
using Expense.Core.DTOs.Groups;
using Expense.Core.DTOs.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Expense.API.Controllers
{
    [ApiController]
    [Route("api/groups")]
    [Authorize]
    public class GroupsController : ControllerBase
    {
        private readonly IGroupService _groupService;
        public GroupsController(IGroupService groupService)
        {
            _groupService = groupService;
        }

        [HttpPost]
        public async Task<ActionResult<APIResponse<GroupDto>>> CreateGroup([FromBody] CreateGroupDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
            var result = await _groupService.CreateGroupAsync(userId, dto, HttpContext.RequestAborted);
            return Ok(APIResponse<GroupDto>.SuccessResponse(result, "Group created successfully"));
        }

        [HttpGet]
        public async Task<ActionResult<APIResponse<IEnumerable<GroupDto>>>> GetUserGroups()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
            var groups = await _groupService.GetUserGroupsAsync(userId, HttpContext.RequestAborted);
            return Ok(APIResponse<IEnumerable<GroupDto>>.SuccessResponse(groups));
        }

        [HttpGet("{groupId:guid}")]
        public async Task<ActionResult<APIResponse<GroupDto>>> GetGroup(Guid groupId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
            var result = await _groupService.GetGroupAsync(groupId, userId, HttpContext.RequestAborted);
            if (result == null) 
                return Forbid(); // Or NotFound depending on business logic, keeping Forbid as per original
            
            return Ok(APIResponse<GroupDto>.SuccessResponse(result));
        }

        [HttpPost("{groupId:guid}/members")]
        public async Task<ActionResult<APIResponse<bool>>> AddMember(Guid groupId, [FromBody] AddGroupMemberDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
            await _groupService.AddMemberByEmailAsync(groupId, userId, dto, HttpContext.RequestAborted);
            return Ok(APIResponse<bool>.SuccessResponse(true, "Member added successfully"));
        }

        [HttpPut("{groupId:guid}/members/{userId}")]
        public async Task<ActionResult<APIResponse<object>>> UpdateMemberRole(Guid groupId, string userId, [FromBody] UpdateGroupMemberRoleDto dto)
        {
            var requesterId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
            var success = await _groupService.UpdateMemberRoleAsync(groupId, requesterId, userId, dto.Role, HttpContext.RequestAborted);
            
            if (!success) return Forbid();

            return Ok(APIResponse<object>.SuccessResponse(null, "Member role updated"));
        }

        [HttpPatch("{groupId:guid}/members/{userId}/role")]
        public async Task<ActionResult<APIResponse<object>>> UpdateMemberRolePartial(Guid groupId, string userId, [FromBody] UpdateGroupMemberRolePatchDto dto)
        {
            var requesterId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
            var success = await _groupService.UpdateMemberRolePartialAsync(groupId, requesterId, userId, dto, HttpContext.RequestAborted);
            
            if (!success) return Forbid();

            return Ok(APIResponse<object>.SuccessResponse(null, "Member role updated"));
        }

        [HttpDelete("{groupId:guid}/members/{userId}")]
        public async Task<ActionResult<APIResponse<object>>> RemoveMember(Guid groupId, string userId)
        {
            var requesterId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
            var success = await _groupService.RemoveMemberAsync(groupId, requesterId, userId, HttpContext.RequestAborted);

            if (!success) return Forbid();

            return Ok(APIResponse<object>.SuccessResponse(null, "Member removed"));
        }

        [HttpPost("join/{inviteCode}")]
        public async Task<ActionResult<APIResponse<object>>> JoinGroup(string inviteCode)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
            var ok = await _groupService.JoinGroupAsync(userId, inviteCode, HttpContext.RequestAborted);
            if (!ok) 
                return NotFound(APIResponse<object>.ErrorResponse("Invalid invite code or group not found", statusCode: 404));
            
            return Ok(APIResponse<object>.SuccessResponse(null, "Joined group successfully"));
        }
    }
}
