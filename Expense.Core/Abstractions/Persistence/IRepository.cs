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

		Task<T> Add(T entity);
		Task<T> Get(Expression<Func<T, bool>>? filter = null, string[]? includes = null, bool noTracking = false);
		Task<IEnumerable<T>> GetAll(Expression<Func<T, bool>>? filter = null, string[]? includes = null);
		Task<T> Update(T entity);
		Task<bool> Remove(T entity);
		Task<bool> RemoveRange(IEnumerable<T> entity);
	}
}
