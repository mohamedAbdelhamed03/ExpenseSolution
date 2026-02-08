using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Expense.Core.Domain.Entities;

namespace Expense.Core.Abstractions.Persistence
{
    public interface INotificationRepository : IRepository<Notification>
    {
        Task<IEnumerable<Notification>> GetUnreadAsync(string userId, CancellationToken cancellationToken = default);
        Task MarkAsReadAsync(string userId, IEnumerable<System.Guid> notificationIds, CancellationToken cancellationToken = default);
    }
}
