using Expense.Core.DTOs.Home;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Expense.Core.Application.Home
{
    public interface IHomeFeedService
    {
        Task<IEnumerable<HomeFeedItemDto>> GetFeedAsync(Guid userId, int page, int pageSize);
    }
}
