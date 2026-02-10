using Expense.Core.Application.Home;
using Expense.Core.DTOs.Home;
using Expense.Core.DTOs.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Expense.API.Controllers
{
    [Authorize]
    [Route("api/home")]
    [ApiController]
    public class HomeController : ControllerBase
    {
        private readonly IHomeFeedService _homeFeedService;

        public HomeController(IHomeFeedService homeFeedService)
        {
            _homeFeedService = homeFeedService;
        }

        [HttpGet]
        public async Task<ActionResult<APIResponse<IEnumerable<HomeFeedItemDto>>>> GetFeed([FromQuery] HomeFeedRequestDto request)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdStr, out var userId))
            {
                return Unauthorized(APIResponse<object>.ErrorResponse("Invalid User ID"));
            }

            var items = await _homeFeedService.GetFeedAsync(userId, request.Page, request.PageSize);
            
            return Ok(APIResponse<IEnumerable<HomeFeedItemDto>>.SuccessResponse(items, "Home feed retrieved successfully"));
        }
    }
}
