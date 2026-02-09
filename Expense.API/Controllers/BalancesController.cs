using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Expense.Core.Application.Balances;
using Expense.Core.DTOs.Balances;
using Expense.Core.DTOs.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Expense.API.Controllers
{
    [ApiController]
    [Route("api/groups/{groupId:guid}/balances")]
    [Authorize]
    public class BalancesController : ControllerBase
    {
        private readonly IBalanceService _balanceService;
        public BalancesController(IBalanceService balanceService)
        {
            _balanceService = balanceService;
        }

        [HttpGet]
        public async Task<ActionResult<APIResponse<IEnumerable<BalanceDto>>>> Get(Guid groupId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
            var result = await _balanceService.GetGroupBalancesAsync(groupId, userId, HttpContext.RequestAborted);
            return Ok(APIResponse<IEnumerable<BalanceDto>>.SuccessResponse(result));
        }
    }
}
