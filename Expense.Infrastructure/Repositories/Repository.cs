using Expense.Core.Abstractions.Persistence;
using Expense.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Expense.Infrastructure.Repositories
{
	public class Repository<T> : IRepository<T> where T : class
	{
		protected readonly ApplicationDbContext _db;
		internal DbSet<T> dbSet;
		public Repository(ApplicationDbContext db)
		{
			_db = db;
			this.dbSet = _db.Set<T>();
		}

		public void Add(T entity)
		{
			dbSet.Add(entity);
		}

		public async Task<T?> Get(Expression<Func<T, bool>>? filter = null, bool noTracking = false, params Expression<Func<T, object>>[] includes)
		{
			IQueryable<T> query = dbSet;
			
			if (includes != null)
			{
				foreach (var include in includes)
				{
					query = query.Include(include);
				}
			}
			
			if (filter != null)
			{
				query = query.Where(filter);
			}
			
			if (noTracking)
			{
				query = query.AsNoTracking();
			}
			
			return await query.FirstOrDefaultAsync();
		}

		public async Task<IEnumerable<T>> GetAll(Expression<Func<T, bool>>? filter = null, params Expression<Func<T, object>>[] includes)
		{
			IQueryable<T> query = dbSet;
			
			if (includes != null)
			{
				foreach (var include in includes)
				{
					query = query.Include(include);
				}
			}
			
			if (filter != null)
			{
				query = query.Where(filter);
			}

			return await query.ToListAsync();
		}

		public void Remove(T entity)
		{
			dbSet.Remove(entity);
		}

		public void RemoveRange(IEnumerable<T> entity)
		{
			dbSet.RemoveRange(entity);
		}

		public void Update(T entity)
		{
			dbSet.Update(entity);
		}

		public async Task<bool> Exists(Expression<Func<T, bool>> filter)
		{
			return await dbSet.AnyAsync(filter);
		}
	}
}