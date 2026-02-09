using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Expense.Core.Application.Settlements;
// using Expense.Core.Common.Responses; // Removing invalid reference
using Expense.Core.Common.Exceptions; // Fixing invalid reference
using Expense.Core.DTOs.Settlements;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Expense.Core.DTOs.Shared; // APIResponse is likely here

namespace Expense.API.Controllers
{
    [ApiController]
    [Route("api/groups/{groupId}/settlements")]
    [Authorize]
    public class SettlementsController : ControllerBase
    {
        private readonly ISettlementService _settlementService;

        public SettlementsController(ISettlementService settlementService)
        {
            _settlementService = settlementService;
        }

        [HttpPost]
        public async Task<ActionResult<APIResponse<SettlementDto>>> Create(Guid groupId, [FromBody] CreateSettlementDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
            var result = await _settlementService.CreateSettlementAsync(groupId, userId, dto, HttpContext.RequestAborted);
            return Ok(APIResponse<SettlementDto>.SuccessResponse(result, "Settlement created successfully"));
        }

        [HttpGet]
        public async Task<ActionResult<APIResponse<IEnumerable<SettlementDto>>>> List(Guid groupId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
            var result = await _settlementService.GetGroupSettlementsAsync(groupId, userId, HttpContext.RequestAborted);
            return Ok(APIResponse<IEnumerable<SettlementDto>>.SuccessResponse(result));
        }

        [HttpGet("{settlementId}")]
        public async Task<ActionResult<APIResponse<SettlementDto>>> Get(Guid groupId, Guid settlementId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
            var result = await _settlementService.GetSettlementAsync(groupId, settlementId, userId, HttpContext.RequestAborted);
            if (result == null) return NotFound(APIResponse<object>.ErrorResponse("Settlement not found"));
            return Ok(APIResponse<SettlementDto>.SuccessResponse(result));
        }

        [HttpDelete("{settlementId}")]
        public async Task<ActionResult<APIResponse<object>>> Delete(Guid groupId, Guid settlementId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
            await _settlementService.DeleteSettlementAsync(groupId, settlementId, userId, HttpContext.RequestAborted);
            return Ok(APIResponse<object>.SuccessResponse(null, "Settlement deleted successfully"));
        }
    }
}
