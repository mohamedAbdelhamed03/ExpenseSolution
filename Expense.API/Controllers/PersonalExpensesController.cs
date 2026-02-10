using Expense.Core.Application.Services;
using Expense.Core.DTOs.Personal;
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
    [Route("api/personal-expenses")]
    [Authorize]
    public class PersonalExpensesController : ControllerBase
    {
        private readonly IPersonalExpenseService _service;

        public PersonalExpensesController(IPersonalExpenseService service)
        {
            _service = service;
        }

        [HttpPost]
        public async Task<ActionResult<APIResponse<PersonalExpenseDto>>> Create([FromBody] CreatePersonalExpenseDto dto)
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _service.CreateAsync(userId, dto);
            return CreatedAtAction(nameof(Get), new { }, APIResponse<PersonalExpenseDto>.SuccessResponse(result));
        }

        [HttpGet]
        public async Task<ActionResult<APIResponse<IEnumerable<PersonalExpenseDto>>>> Get([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _service.GetUserExpensesAsync(userId, page, pageSize);
            return Ok(APIResponse<IEnumerable<PersonalExpenseDto>>.SuccessResponse(result));
        }
    }
}
