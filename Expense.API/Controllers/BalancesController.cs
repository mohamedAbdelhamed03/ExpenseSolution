using System.Security.Claims;
using Expense.Core.Abstractions.Balances;
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
        public async Task<IActionResult> Get(Guid groupId)
        {
            var userId = User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier) ?? string.Empty;
            var result = await _balanceService.GetGroupBalancesAsync(groupId, userId);
            return Ok(result);
        }
    }
}
