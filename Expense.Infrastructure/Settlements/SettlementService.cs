using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Expense.Core.Application.Persistence;
using Expense.Core.Application.Settlements;
using Expense.Core.Application.Notifications;
using Expense.Core.Domain.Entities;
using Expense.Core.Common.Exceptions;
using Expense.Core.DTOs.ActivityLogs;
using Expense.Core.DTOs.Settlements;
using Expense.Core.DTOs.Notifications;
using Microsoft.EntityFrameworkCore;
using Expense.Core.Application.ActivityLogs;
using Expense.Core.Application.Balances;
using Expense.Core.Domain.Enums;
using FluentValidation;

namespace Expense.Infrastructure.Settlements
{
    public class SettlementService : ISettlementService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IActivityLogService _activityLogService;
        private readonly IBalanceService _balanceService;
        private readonly IRealtimeNotifier _notifier;
        private readonly IValidator<CreateSettlementDto> _createValidator;

        public SettlementService(
            IUnitOfWork unitOfWork, 
            IActivityLogService activityLogService,
            IBalanceService balanceService,
            IRealtimeNotifier notifier,
            IValidator<CreateSettlementDto> createValidator)
        {
            _unitOfWork = unitOfWork;
            _activityLogService = activityLogService;
            _balanceService = balanceService;
            _notifier = notifier;
            _createValidator = createValidator;
        }

        public async Task<SettlementDto> CreateSettlementAsync(Guid groupId, string payerUserId, CreateSettlementDto settlementDto, CancellationToken cancellationToken = default)
        {
            await _createValidator.ValidateAndThrowAsync(settlementDto, cancellationToken);

            // 1. Validate Group
            var group = await _unitOfWork.Groups.GetWithMembersAsync(groupId, cancellationToken);
            if (group == null)
            {
                throw new NotFoundException("Group.NotFound");
            }

            // 2. Validate Membership (Payer)
            if (!await _unitOfWork.Groups.IsMemberAsync(groupId, payerUserId, cancellationToken))
            {
                throw new GroupAccessDeniedException();
            }

            // 3. Validate Payee
            if (!await _unitOfWork.Groups.IsMemberAsync(groupId, settlementDto.PayeeUserId, cancellationToken))
            {
                throw new GroupAccessDeniedException();
            }

            if (payerUserId == settlementDto.PayeeUserId)
            {
                throw new BusinessException("Settlement.SelfPayment");
            }

            // Over-settlement Validation
            var balances = await _balanceService.GetGroupBalancesAsync(groupId, payerUserId, cancellationToken);
            var payerBalance = balances.FirstOrDefault(b => b.UserId == payerUserId)?.Balance ?? 0;
            var payeeBalance = balances.FirstOrDefault(b => b.UserId == settlementDto.PayeeUserId)?.Balance ?? 0;

            var settlementAmount = settlementDto.Amount * (settlementDto.ExchangeRate ?? 1m);

            if (payerBalance >= 0) 
                throw new BusinessException("Settlement.PayerHasNoDebt");
            
            if (payeeBalance <= 0)
                throw new BusinessException("Settlement.PayeeIsNotCreditor");

            if (settlementAmount > -payerBalance + 0.01m)
                throw new BusinessException("Settlement.AmountExceedsDebt");

            if (settlementAmount > payeeBalance + 0.01m)
                throw new BusinessException("Settlement.AmountExceedsCredit");

            // 4. Create Settlement
            var settlement = new Settlement
            {
                Id = Guid.NewGuid(),
                GroupId = groupId,
                PayerUserId = payerUserId,
                PayeeUserId = settlementDto.PayeeUserId,
                Amount = settlementDto.Amount,
                Currency = settlementDto.Currency,
                ExchangeRate = settlementDto.ExchangeRate,
                SettlementDate = settlementDto.SettlementDate,
                CreatedAt = DateTime.UtcNow
            };

            _unitOfWork.Repository<Settlement>().Add(settlement);

            // 5. Log Activity
            await _activityLogService.LogActivityAsync(groupId, payerUserId, ActivityType.Created, EntityType.Settlement, settlement.Id.ToString(), $"Amount: {settlement.Amount} {settlement.Currency}", cancellationToken);

            await _unitOfWork.SaveAsync(cancellationToken);

            await NotifyGroupMembersAsync(groupId, payerUserId, "Settlement_Created", new { SettlementId = settlement.Id, Amount = settlement.Amount, PayeeId = settlement.PayeeUserId }, cancellationToken);

            return MapToDto(settlement);
        }

        public async Task<IEnumerable<SettlementDto>> GetGroupSettlementsAsync(Guid groupId, string userId, CancellationToken cancellationToken = default)
        {
            if (!await _unitOfWork.Groups.IsMemberAsync(groupId, userId, cancellationToken))
            {
                throw new GroupAccessDeniedException();
            }

            var settlements = await _unitOfWork.Repository<Settlement>()
                .GetAll(s => s.GroupId == groupId);

            return settlements.Select(MapToDto);
        }

        public async Task<SettlementDto> GetSettlementAsync(Guid groupId, Guid settlementId, string userId, CancellationToken cancellationToken = default)
        {
            if (!await _unitOfWork.Groups.IsMemberAsync(groupId, userId, cancellationToken))
            {
                throw new GroupAccessDeniedException();
            }

            var settlement = await _unitOfWork.Repository<Settlement>()
                .Get(s => s.Id == settlementId);

            if (settlement == null || settlement.GroupId != groupId)
            {
                throw new BusinessException("Settlement.NotFound");
            }

            return MapToDto(settlement);
        }

        public async Task DeleteSettlementAsync(Guid groupId, Guid settlementId, string userId, CancellationToken cancellationToken = default)
        {
            if (!await _unitOfWork.Groups.IsMemberAsync(groupId, userId, cancellationToken))
            {
                throw new GroupAccessDeniedException();
            }

            var settlement = await _unitOfWork.Repository<Settlement>()
                .Get(s => s.Id == settlementId);

            if (settlement == null || settlement.GroupId != groupId)
            {
                throw new BusinessException("Settlement.NotFound");
            }

            // Check if user is payer or payee or admin
            // For simplicity, we allow the payer (creator) or group creator to delete.
            // We need to check if the user is the payer or group admin.
            // Assuming GroupRepository has a way to check roles or we fetch group.
            
            // Re-fetch group to check admin status if needed, or rely on payer check.
            // Let's enforce: Only Payer (Creator of settlement) can delete.
            if (settlement.PayerUserId != userId)
            {
                 // Check if admin
                 var group = await _unitOfWork.Groups.Get(g => g.Id == groupId);
                 if (group != null && group.CreatedByUserId != userId)
                 {
                     throw new GroupAccessDeniedException();
                 }
            }

            _unitOfWork.Repository<Settlement>().Remove(settlement);

            // Log Activity
            await _activityLogService.LogActivityAsync(groupId, userId, ActivityType.Deleted, EntityType.Settlement, settlementId.ToString(), $"Amount: {settlement.Amount} {settlement.Currency}", cancellationToken);

            await _unitOfWork.SaveAsync(cancellationToken);

            await NotifyGroupMembersAsync(groupId, userId, "Settlement_Deleted", new { SettlementId = settlementId }, cancellationToken);
        }

        private async Task NotifyGroupMembersAsync(Guid groupId, string actorUserId, string type, object payload, CancellationToken cancellationToken)
        {
            try
            {
                var members = await _unitOfWork.Repository<GroupMember>().GetAll(m => m.GroupId == groupId);
                var userIds = members.Select(m => m.UserId).Where(u => u != actorUserId).Distinct().ToList();

                if (userIds.Any())
                {
                    var message = new NotificationMessage
                    {
                        Type = type,
                        GroupId = groupId,
                        ActorUserId = actorUserId,
                        Payload = payload,
                        Timestamp = DateTime.UtcNow
                    };
                    await _notifier.NotifyUsersAsync(userIds, message, cancellationToken);
                }
            }
            catch
            {
                // Fire and forget
            }
        }

        private static SettlementDto MapToDto(Settlement settlement)
        {
            return new SettlementDto
            {
                Id = settlement.Id,
                GroupId = settlement.GroupId,
                PayerUserId = settlement.PayerUserId,
                PayeeUserId = settlement.PayeeUserId,
                Amount = settlement.Amount,
                Currency = settlement.Currency,
                ExchangeRate = settlement.ExchangeRate,
                SettlementDate = settlement.SettlementDate,
                CreatedAt = settlement.CreatedAt
            };
        }
    }
}
