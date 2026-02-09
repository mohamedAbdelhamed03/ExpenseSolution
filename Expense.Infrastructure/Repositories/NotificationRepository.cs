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
    public class NotificationRepository : Repository<Notification>, INotificationRepository
    {
        public NotificationRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<Notification>> GetUnreadAsync(string userId, CancellationToken cancellationToken = default)
        {
            return await _db.Set<Notification>()
                .Where(n => n.UserId == userId && !n.IsRead)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync(cancellationToken);
        }

        public async Task MarkAsReadAsync(string userId, IEnumerable<Guid> notificationIds, CancellationToken cancellationToken = default)
        {
            var notifications = await _db.Set<Notification>()
                .Where(n => n.UserId == userId && notificationIds.Contains(n.Id))
                .ToListAsync(cancellationToken);

            foreach (var n in notifications)
            {
                n.IsRead = true;
            }
        }
    }
}
