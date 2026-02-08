using Expense.Core.Abstractions.Persistence;
using Expense.Infrastructure.Data;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Expense.Infrastructure.Repositories
{
	public class UnitOfWork : IUnitOfWork
	{
		private readonly ApplicationDbContext _db;
		private Hashtable _repositories;
        public IExpenseRepository Expenses { get; private set; }
        public IGroupRepository Groups { get; private set; }
        public IBalanceRepository Balances { get; private set; }
        public IExpenseCategoryRepository Categories { get; private set; }
        public IActivityLogRepository ActivityLogs { get; private set; }
        public INotificationRepository Notifications { get; private set; }

		public UnitOfWork(ApplicationDbContext db)
		{
			_db = db;
			_repositories = new Hashtable();
            Expenses = new ExpenseRepository(_db);
            Groups = new GroupRepository(_db);
            Balances = new BalanceRepository(_db);
            Categories = new ExpenseCategoryRepository(_db);
            ActivityLogs = new ActivityLogRepository(_db);
            Notifications = new NotificationRepository(_db);
		}

		public IRepository<T> Repository<T>() where T : class
		{
			if (_repositories == null)
				_repositories = new Hashtable();

			var type = typeof(T).Name;

			if (!_repositories.ContainsKey(type))
			{
				var repositoryType = typeof(Repository<>);
				var repositoryInstance = Activator.CreateInstance(repositoryType.MakeGenericType(typeof(T)), _db);
				_repositories.Add(type, repositoryInstance);
			}

			return (IRepository<T>)_repositories[type]!;
		}

		public async Task<int> SaveAsync(CancellationToken cancellationToken = default)
		{
			return await _db.SaveChangesAsync(cancellationToken);
		}
	}
}
