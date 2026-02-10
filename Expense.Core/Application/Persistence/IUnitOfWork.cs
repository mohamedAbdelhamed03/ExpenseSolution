using Expense.Core.Application.Persistence;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Expense.Core.Application.Persistence
{
	public interface IUnitOfWork
	{
		IRepository<T> Repository<T>() where T : class;
        IExpenseRepository Expenses { get; }
        IGroupRepository Groups { get; }
        IBalanceRepository Balances { get; }
        IExpenseCategoryRepository Categories { get; }
        IActivityLogRepository ActivityLogs { get; }
        INotificationRepository Notifications { get; }
        IHomeFeedRepository HomeFeed { get; }
        IPersonalExpenseRepository PersonalExpenses { get; }
		Task<int> SaveAsync(CancellationToken cancellationToken = default);
	}
}
