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
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, APIResponse<PersonalExpenseDto>.SuccessResponse(result));
        }

        [HttpGet]
        public async Task<ActionResult<APIResponse<IEnumerable<PersonalExpenseDto>>>> Get([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _service.GetUserExpensesAsync(userId, page, pageSize);
            return Ok(APIResponse<IEnumerable<PersonalExpenseDto>>.SuccessResponse(result));
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<APIResponse<PersonalExpenseDto>>> GetById(Guid id)
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _service.GetByIdAsync(userId, id);
            return Ok(APIResponse<PersonalExpenseDto>.SuccessResponse(result));
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<APIResponse<PersonalExpenseDto>>> Update(Guid id, [FromBody] UpdatePersonalExpenseDto dto)
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _service.UpdateAsync(userId, id, dto);
            return Ok(APIResponse<PersonalExpenseDto>.SuccessResponse(result));
        }

        [HttpPatch("{id}")]
        public async Task<ActionResult<APIResponse<PersonalExpenseDto>>> UpdatePatch(Guid id, [FromBody] UpdatePersonalExpensePatchDto dto)
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _service.UpdatePatchAsync(userId, id, dto);
            return Ok(APIResponse<PersonalExpenseDto>.SuccessResponse(result));
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult<APIResponse<object>>> Delete(Guid id)
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            await _service.DeleteAsync(userId, id);
            return Ok(APIResponse<object>.SuccessResponse(null, "Expense deleted successfully"));
        }
    }
}
