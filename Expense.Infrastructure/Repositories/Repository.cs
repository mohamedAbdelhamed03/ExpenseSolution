using Expense.Core.Abstractions.Persistence;
using Expense.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Expense.Infrastructure.Repositories

{
	public class Repository<T> : IRepository<T> where T : class

	{
		private readonly ApplicationDbContext _db;
		internal DbSet<T> dbSet;
		public Repository(ApplicationDbContext db)
		{
			_db = db;
			this.dbSet = _db.Set<T>();
		}
		public async Task<T> Add(T entity)
		{
			await dbSet.AddAsync(entity);
			await _db.SaveChangesAsync();
			return entity;
		}

		public async Task<T> Get(Expression<Func<T, bool>>? filter, string[]? includes, bool noTracking)
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
			T? result = await query.FirstOrDefaultAsync();
			return result!;
		}

		public async Task<IEnumerable<T>> GetAll(Expression<Func<T, bool>>? filter, string[]? includes)
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

		public async Task<bool> Remove(T entity)
		{
			dbSet.Remove(entity);
			int rowsDeleted = await _db.SaveChangesAsync();
			return rowsDeleted > 0;

		}

		public async Task<bool> RemoveRange(IEnumerable<T> entity)
		{
			dbSet.RemoveRange(entity);

			int rowsDeleted = await _db.SaveChangesAsync();
			return rowsDeleted > 0;
		}
		public async Task<T> Update(T entity)
		{
			dbSet.Update(entity);
			await _db.SaveChangesAsync();
			return entity;

		}


	}
}
