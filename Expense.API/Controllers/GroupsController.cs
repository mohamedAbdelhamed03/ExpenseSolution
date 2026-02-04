using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Expense.Core.Abstractions.Groups;
using Expense.Core.DTOs.Groups;
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
        public async Task<IActionResult> CreateGroup([FromBody] CreateGroupDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
            var result = await _groupService.CreateGroupAsync(userId, dto);
            return Ok(result);
        }

        [HttpGet("{groupId:guid}")]
        public async Task<IActionResult> GetGroup(Guid groupId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
            var result = await _groupService.GetGroupAsync(groupId, userId);
            if (result == null) return Forbid();
            return Ok(result);
        }

        [HttpPost("join/{inviteCode}")]
        public async Task<IActionResult> JoinGroup(string inviteCode)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
            var ok = await _groupService.JoinGroupAsync(userId, inviteCode);
            if (!ok) return NotFound();
            return Ok();
        }
    }
}
