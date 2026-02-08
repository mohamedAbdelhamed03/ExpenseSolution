using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Expense.Core.Abstractions.ActivityLogs;
using Expense.Core.Abstractions.Persistence;
using Expense.Core.Common.Exceptions;
using Expense.Core.Domain.Entities;
using Expense.Core.Domain.Enums;
using Expense.Core.DTOs.ActivityLogs;

namespace Expense.Infrastructure.ActivityLogs
{
    public class ActivityLogService : IActivityLogService
    {
        private readonly IUnitOfWork _unitOfWork;

        public ActivityLogService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task LogActivityAsync(Guid groupId, string userId, ActivityType action, EntityType entityType, string entityId, string? details, CancellationToken cancellationToken)
        {
            var log = new ActivityLog
            {
                Id = Guid.NewGuid(),
                GroupId = groupId,
                UserId = userId,
                Action = action,
                EntityType = entityType,
                EntityId = entityId,
                Details = details,
                Timestamp = DateTime.UtcNow
            };

            _unitOfWork.ActivityLogs.Add(log);
            // We do not call SaveAsync here to allow the caller to manage the transaction scope.
            // If the caller wants to save immediately, they should call SaveAsync on the UnitOfWork.
            // However, to support "fail safely" without breaking main operation if this was a separate call,
            // we are shifting to "Same UnitOfWork" pattern.
        }

        public async Task<IEnumerable<ActivityLogDto>> GetActivityLogsAsync(Guid groupId, string userId, int page, int pageSize, CancellationToken cancellationToken)
        {
            var isMember = await _unitOfWork.Groups.IsMemberAsync(groupId, userId, cancellationToken);
            if (!isMember)
            {
                throw new GroupAccessDeniedException("Group_NotMember");
            }

            var logs = await _unitOfWork.ActivityLogs.GetLogsForGroupAsync(groupId, page, pageSize, cancellationToken);

            return logs.Select(l => new ActivityLogDto
            {
                Id = l.Id,
                GroupId = l.GroupId,
                UserId = l.UserId,
                Action = l.Action.ToString(),
                EntityType = l.EntityType.ToString(),
                EntityId = l.EntityId,
                Details = l.Details,
                Timestamp = l.Timestamp
            });
        }
    }
}