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

		public UnitOfWork(ApplicationDbContext db)
		{
			_db = db;
			_repositories = new Hashtable();
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