using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Expense.Core.Domain.Entities;

namespace Expense.Core.Abstractions.Persistence
{
    public interface IActivityLogRepository : IRepository<ActivityLog>
    {
        Task<IEnumerable<ActivityLog>> GetLogsForGroupAsync(Guid groupId, int page, int pageSize, CancellationToken cancellationToken);
        Task<int> GetCountForGroupAsync(Guid groupId, CancellationToken cancellationToken);
    }
}