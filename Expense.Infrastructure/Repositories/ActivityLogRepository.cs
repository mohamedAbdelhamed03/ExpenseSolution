using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Expense.Core.Application.Persistence;
using Expense.Core.Domain.Entities;
using Expense.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Expense.Infrastructure.Repositories
{
    public class ActivityLogRepository : Repository<ActivityLog>, IActivityLogRepository
    {
        private readonly ApplicationDbContext _db;

        public ActivityLogRepository(ApplicationDbContext db) : base(db)
        {
            _db = db;
        }

        public async Task<IEnumerable<ActivityLog>> GetLogsForGroupAsync(Guid groupId, int page, int pageSize, CancellationToken cancellationToken)
        {
            return await _db.ActivityLogs
                .AsNoTracking()
                .Where(l => l.GroupId == groupId)
                .OrderByDescending(l => l.Timestamp)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);
        }

        public async Task<int> GetCountForGroupAsync(Guid groupId, CancellationToken cancellationToken)
        {
            return await _db.ActivityLogs
                .AsNoTracking()
                .CountAsync(l => l.GroupId == groupId, cancellationToken);
        }
    }
}