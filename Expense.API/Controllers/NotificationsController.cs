using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Expense.Core.Application.Persistence;
using Expense.Core.DTOs.Notifications;
using Expense.Core.DTOs.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Expense.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class NotificationsController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;

        public NotificationsController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        [HttpGet("unread")]
        public async Task<ActionResult<APIResponse<IEnumerable<NotificationDto>>>> GetUnreadNotifications(CancellationToken cancellationToken)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var notifications = await _unitOfWork.Notifications.GetUnreadAsync(userId, cancellationToken);

            var dtos = notifications.Select(n => new NotificationDto
            {
                Id = n.Id,
                UserId = n.UserId,
                Type = n.Type,
                Payload = n.Message,
                IsRead = n.IsRead,
                CreatedAt = n.CreatedAt
            });

            return Ok(APIResponse<IEnumerable<NotificationDto>>.SuccessResponse(dtos));
        }

        [HttpPost("mark-read")]
        public async Task<ActionResult<APIResponse<bool>>> MarkAsRead([FromBody] MarkReadDto dto, CancellationToken cancellationToken)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            await _unitOfWork.Notifications.MarkAsReadAsync(userId, dto.NotificationIds, cancellationToken);
            await _unitOfWork.SaveAsync(cancellationToken);

            return Ok(APIResponse<bool>.SuccessResponse(true));
        }

        [HttpPost("{id}/read")]
        public async Task<ActionResult<APIResponse<bool>>> MarkSingleAsRead(Guid id, CancellationToken cancellationToken)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            await _unitOfWork.Notifications.MarkAsReadAsync(userId, new[] { id }, cancellationToken);
            await _unitOfWork.SaveAsync(cancellationToken);

            return Ok(APIResponse<bool>.SuccessResponse(true));
        }
    }
}
