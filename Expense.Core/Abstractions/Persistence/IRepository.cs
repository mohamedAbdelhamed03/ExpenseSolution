using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Expense.Core.Abstractions.Persistence
{
	public interface IRepository<T> where T : class
	{

		void Add(T entity);
		Task<T?> Get(Expression<Func<T, bool>>? filter = null, bool noTracking = false, params Expression<Func<T, object>>[] includes);
		Task<IEnumerable<T>> GetAll(Expression<Func<T, bool>>? filter = null, params Expression<Func<T, object>>[] includes);
		void Update(T entity);
		void Remove(T entity);
		void RemoveRange(IEnumerable<T> entity);
		Task<bool> Exists(Expression<Func<T, bool>> filter);
	}
}
