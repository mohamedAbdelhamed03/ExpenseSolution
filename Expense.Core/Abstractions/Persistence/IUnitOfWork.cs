using Expense.Core.Abstractions.Persistence;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Expense.Core.Abstractions.Persistence
{
	public interface IUnitOfWork
	{
		IRepository<T> Repository<T>() where T : class;
        IExpenseRepository Expenses { get; }
        IGroupRepository Groups { get; }
        IBalanceRepository Balances { get; }
        IExpenseCategoryRepository Categories { get; }
        IActivityLogRepository ActivityLogs { get; }
		Task<int> SaveAsync(CancellationToken cancellationToken = default);
	}
}
