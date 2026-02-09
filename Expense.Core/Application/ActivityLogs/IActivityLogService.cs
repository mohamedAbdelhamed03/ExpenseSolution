using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Expense.Core.Domain.Enums;
using Expense.Core.DTOs.ActivityLogs;

namespace Expense.Core.Application.ActivityLogs
{
    public interface IActivityLogService
    {
        Task LogActivityAsync(Guid groupId, string userId, ActivityType action, EntityType entityType, string entityId, string? details, CancellationToken cancellationToken);
        Task<IEnumerable<ActivityLogDto>> GetActivityLogsAsync(Guid groupId, string userId, int page, int pageSize, CancellationToken cancellationToken);
    }
}