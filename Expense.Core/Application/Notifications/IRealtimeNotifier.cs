using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Expense.Core.DTOs.Notifications;

namespace Expense.Core.Application.Notifications
{
    public interface IRealtimeNotifier
    {
        Task NotifyUserAsync(string userId, NotificationMessage message, CancellationToken cancellationToken = default);
        Task NotifyUsersAsync(IEnumerable<string> userIds, NotificationMessage message, CancellationToken cancellationToken = default);
    }
}
