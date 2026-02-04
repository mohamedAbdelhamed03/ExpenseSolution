using Expense.Core.Abstractions.Persistence;
using Expense.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Expense.Infrastructure.Repositories
{

	public class UnitOfWork : IUnitOfWork
	{
		private readonly ApplicationDbContext _db;

		public UnitOfWork(ApplicationDbContext db)
		{
			_db = db;

		}
	}
}
