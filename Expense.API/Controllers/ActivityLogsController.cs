using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Expense.Core.Abstractions.ActivityLogs;
using Expense.Core.DTOs.ActivityLogs;
using Expense.Core.DTOs.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Expense.API.Controllers
{
    [ApiController]
    [Authorize]
    public class ActivityLogsController : ControllerBase
    {
        private readonly IActivityLogService _activityLogService;

        public ActivityLogsController(IActivityLogService activityLogService)
        {
            _activityLogService = activityLogService;
        }

        [HttpGet("api/groups/{groupId:guid}/activities")]
        public async Task<ActionResult<APIResponse<IEnumerable<ActivityLogDto>>>> GetActivityLogs(
            Guid groupId, 
            [FromQuery] int page = 1, 
            [FromQuery] int pageSize = 20)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
            var logs = await _activityLogService.GetActivityLogsAsync(groupId, userId, page, pageSize, HttpContext.RequestAborted);
            return Ok(APIResponse<IEnumerable<ActivityLogDto>>.SuccessResponse(logs));
        }
    }
}