using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Expense.Core.Abstractions.Expenses;
using Expense.Core.DTOs.Expenses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Expense.API.Controllers
{
    [ApiController]
    [Route("api/groups/{groupId:guid}/expenses")]
    [Authorize]
    public class ExpensesController : ControllerBase
    {
        private readonly IExpenseService _expenseService;
        public ExpensesController(IExpenseService expenseService)
        {
            _expenseService = expenseService;
        }

        [HttpPost]
        public async Task<IActionResult> Create(Guid groupId, [FromBody] CreateExpenseDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
            var result = await _expenseService.CreateExpenseAsync(groupId, userId, dto);
            return Ok(result);
        }

        [HttpGet]
        public async Task<IActionResult> List(Guid groupId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
            var result = await _expenseService.GetGroupExpensesAsync(groupId, userId);
            return Ok(result);
        }
    }
}
