using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Expense.Core.Abstractions.Debts;
using Expense.Core.DTOs.Debts;
using Expense.Core.DTOs.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Expense.API.Controllers
{
    [ApiController]
    [Route("api/groups/{groupId:guid}/debts")]
    [Authorize]
    public class DebtsController : ControllerBase
    {
        private readonly IDebtSimplificationService _debtService;

        public DebtsController(IDebtSimplificationService debtService)
        {
            _debtService = debtService;
        }

        [HttpGet("simplified")]
        public async Task<ActionResult<APIResponse<IEnumerable<SimplifiedDebtGroupDto>>>> GetSimplified(Guid groupId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
            var result = await _debtService.GetSimplifiedDebtsAsync(groupId, userId, HttpContext.RequestAborted);
            return Ok(APIResponse<IEnumerable<SimplifiedDebtGroupDto>>.SuccessResponse(result));
        }
    }
}
