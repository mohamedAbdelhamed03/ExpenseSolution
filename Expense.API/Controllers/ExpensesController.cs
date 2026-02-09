using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Expense.Core.Application.Expenses;
using Expense.Core.DTOs.Expenses;
using Expense.Core.DTOs.Shared;
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
        public async Task<ActionResult<APIResponse<ExpenseDto>>> Create(Guid groupId, [FromBody] CreateExpenseDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
            var result = await _expenseService.CreateExpenseAsync(groupId, userId, dto, HttpContext.RequestAborted);
            return Ok(APIResponse<ExpenseDto>.SuccessResponse(result, "Expense created successfully"));
        }

        [HttpGet]
        public async Task<ActionResult<APIResponse<IEnumerable<ExpenseDto>>>> List(Guid groupId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
            var result = await _expenseService.GetGroupExpensesAsync(groupId, userId, HttpContext.RequestAborted);
            return Ok(APIResponse<IEnumerable<ExpenseDto>>.SuccessResponse(result));
        }

        [HttpGet("{expenseId}")]
        public async Task<ActionResult<APIResponse<ExpenseDto>>> Get(Guid groupId, Guid expenseId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
            var result = await _expenseService.GetExpenseAsync(groupId, expenseId, userId, HttpContext.RequestAborted);
            
            if (result == null) return NotFound(APIResponse<object>.ErrorResponse("Expense not found"));

            return Ok(APIResponse<ExpenseDto>.SuccessResponse(result));
        }

        [HttpPut("{expenseId}")]
        public async Task<ActionResult<APIResponse<ExpenseDto>>> Update(Guid groupId, Guid expenseId, [FromBody] UpdateExpenseDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
            
            var result = await _expenseService.UpdateExpenseAsync(groupId, expenseId, userId, dto, HttpContext.RequestAborted);
            return Ok(APIResponse<ExpenseDto>.SuccessResponse(result, "Expense updated successfully"));
        }

        [HttpPatch("{expenseId}")]
        public async Task<ActionResult<APIResponse<ExpenseDto>>> UpdatePartial(Guid groupId, Guid expenseId, [FromBody] UpdateExpensePatchDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
            
            var result = await _expenseService.UpdateExpensePartialAsync(groupId, expenseId, userId, dto, HttpContext.RequestAborted);
            return Ok(APIResponse<ExpenseDto>.SuccessResponse(result, "Expense updated successfully"));
        }

        [HttpDelete("{expenseId}")]
        public async Task<ActionResult<APIResponse<bool>>> Delete(Guid groupId, Guid expenseId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
            
            var result = await _expenseService.DeleteExpenseAsync(groupId, expenseId, userId, HttpContext.RequestAborted);
            return Ok(APIResponse<bool>.SuccessResponse(result, "Expense deleted successfully"));
        }
    }
}
