using Expense.Core.DTOs.Home;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Expense.Core.Application.Persistence
{
    public interface IHomeFeedRepository
    {
        Task<IEnumerable<HomeFeedItemDto>> GetFeedAsync(Guid userId, int page, int pageSize);
    }
}
