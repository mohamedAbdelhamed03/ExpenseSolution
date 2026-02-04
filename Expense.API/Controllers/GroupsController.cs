using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Expense.Core.Abstractions.Groups;
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
            var result = await _groupService.CreateGroupAsync(userId, dto);
            return Ok(APIResponse<GroupDto>.SuccessResponse(result, "Group created successfully"));
        }

        [HttpGet("{groupId:guid}")]
        public async Task<ActionResult<APIResponse<GroupDto>>> GetGroup(Guid groupId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
            var result = await _groupService.GetGroupAsync(groupId, userId);
            if (result == null) 
                return Forbid(); // Or NotFound depending on business logic, keeping Forbid as per original
            
            return Ok(APIResponse<GroupDto>.SuccessResponse(result));
        }

        [HttpPost("join/{inviteCode}")]
        public async Task<ActionResult<APIResponse<object>>> JoinGroup(string inviteCode)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
            var ok = await _groupService.JoinGroupAsync(userId, inviteCode);
            if (!ok) 
                return NotFound(APIResponse<object>.ErrorResponse("Invalid invite code or group not found", statusCode: 404));
            
            return Ok(APIResponse<object>.SuccessResponse(null, "Joined group successfully"));
        }
    }
}
