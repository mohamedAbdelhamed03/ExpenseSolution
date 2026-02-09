using Expense.Core.Application.Insights;
using Expense.Core.DTOs.Insights;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
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
        public async Task<ActionResult<IEnumerable<InsightsSummaryDto>>> GetInsights(
            Guid groupId,
            [FromQuery] string period = "month",
            [FromQuery] string? date = null,
            CancellationToken cancellationToken = default)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var result = await _insightsService.GetInsightsAsync(groupId, period, date, userId, cancellationToken);
            return Ok(result);
        }
    }
}
