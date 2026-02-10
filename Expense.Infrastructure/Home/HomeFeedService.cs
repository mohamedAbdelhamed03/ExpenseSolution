using Expense.Core.Application.Home;
using Expense.Core.Application.Persistence;
using Expense.Core.DTOs.Home;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Expense.Infrastructure.Home
{
    public class HomeFeedService : IHomeFeedService
    {
        private readonly IUnitOfWork _unitOfWork;

        public HomeFeedService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<IEnumerable<HomeFeedItemDto>> GetFeedAsync(Guid userId, int page, int pageSize)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 10;
            if (pageSize > 50) pageSize = 50;

            return await _unitOfWork.HomeFeed.GetFeedAsync(userId, page, pageSize);
        }
    }
}
