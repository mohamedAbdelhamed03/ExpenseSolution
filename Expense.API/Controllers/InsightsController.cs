using Expense.Core.Application.Insights;
using Expense.Core.DTOs.Insights;
using Expense.Core.DTOs.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Expense.API.Controllers
{
    [ApiController]
    [Route("api/groups/{groupId}/insights")]
    [Authorize]
    public class InsightsController : ControllerBase
    {
        private readonly IInsightsService _insightsService;

        public InsightsController(IInsightsService insightsService)
        {
            _insightsService = insightsService;
        }

        [HttpGet]
        public async Task<ActionResult<APIResponse<IEnumerable<InsightsSummaryDto>>>> GetInsights(
            [FromRoute] Guid groupId,
            [FromQuery] string period = "month",
            [FromQuery] string date = "",
            [FromQuery] string scope = "group")
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var result = await _insightsService.GetInsightsAsync(groupId, period, date, scope, userId);
            return Ok(APIResponse<IEnumerable<InsightsSummaryDto>>.SuccessResponse(result));
        }

        [HttpGet("/api/insights/home")]
        public async Task<ActionResult<APIResponse<IEnumerable<InsightsSummaryDto>>>> GetHomeInsights(
            [FromQuery] string period = "month",
            [FromQuery] string date = "")
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var result = await _insightsService.GetHomeInsightsAsync(period, date, userId);
            return Ok(APIResponse<IEnumerable<InsightsSummaryDto>>.SuccessResponse(result));
        }
    }
}
