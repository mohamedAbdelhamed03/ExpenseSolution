using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Expense.Core.DTOs.Settlements;

namespace Expense.Core.Application.Settlements
{
    public interface ISettlementService
    {
        Task<SettlementDto> CreateSettlementAsync(Guid groupId, string payerUserId, CreateSettlementDto settlementDto, CancellationToken cancellationToken = default);
        Task<IEnumerable<SettlementDto>> GetGroupSettlementsAsync(Guid groupId, string userId, CancellationToken cancellationToken = default);
        Task<SettlementDto> GetSettlementAsync(Guid groupId, Guid settlementId, string userId, CancellationToken cancellationToken = default);
        Task DeleteSettlementAsync(Guid groupId, Guid settlementId, string userId, CancellationToken cancellationToken = default);
    }
}
