using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Expense.Core.Abstractions.Categories;
using Expense.Core.DTOs.Categories;
using Expense.Core.DTOs.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Expense.API.Controllers
{
    [ApiController]
    [Authorize]
    public class CategoriesController : ControllerBase
    {
        private readonly IExpenseCategoryService _categoryService;

        public CategoriesController(IExpenseCategoryService categoryService)
        {
            _categoryService = categoryService;
        }

        [HttpGet("api/groups/{groupId:guid}/categories")]
        public async Task<ActionResult<APIResponse<IEnumerable<ExpenseCategoryDto>>>> GetGroupCategories(Guid groupId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
            var result = await _categoryService.GetCategoriesAsync(groupId, userId, HttpContext.RequestAborted);
            return Ok(APIResponse<IEnumerable<ExpenseCategoryDto>>.SuccessResponse(result));
        }

        [HttpPost("api/groups/{groupId:guid}/categories")]
        public async Task<ActionResult<APIResponse<ExpenseCategoryDto>>> CreateCategory(Guid groupId, [FromBody] CreateExpenseCategoryDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
            var result = await _categoryService.CreateCategoryAsync(groupId, userId, dto, HttpContext.RequestAborted);
            return Ok(APIResponse<ExpenseCategoryDto>.SuccessResponse(result, "Category created successfully"));
        }

        [HttpPut("api/categories/{categoryId:guid}")]
        public async Task<ActionResult<APIResponse<ExpenseCategoryDto>>> UpdateCategory(Guid categoryId, [FromBody] UpdateExpenseCategoryDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
            var result = await _categoryService.UpdateCategoryAsync(categoryId, userId, dto, HttpContext.RequestAborted);
            if (result == null) return NotFound(APIResponse<object>.ErrorResponse("Category not found", statusCode: 404));

            return Ok(APIResponse<ExpenseCategoryDto>.SuccessResponse(result, "Category updated successfully"));
        }

        [HttpPatch("api/categories/{categoryId:guid}")]
        public async Task<ActionResult<APIResponse<ExpenseCategoryDto>>> UpdateCategoryPartial(Guid categoryId, [FromBody] UpdateCategoryPatchDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
            var result = await _categoryService.UpdateCategoryPartialAsync(categoryId, userId, dto, HttpContext.RequestAborted);
            if (result == null) return NotFound(APIResponse<object>.ErrorResponse("Category not found", statusCode: 404));

            return Ok(APIResponse<ExpenseCategoryDto>.SuccessResponse(result, "Category updated successfully"));
        }

        [HttpDelete("api/categories/{categoryId:guid}")]
        public async Task<ActionResult<APIResponse<object>>> DeleteCategory(Guid categoryId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
            await _categoryService.DeleteCategoryAsync(categoryId, userId, HttpContext.RequestAborted);
            return Ok(APIResponse<object>.SuccessResponse(null, "Category deleted successfully"));
        }
    }
}