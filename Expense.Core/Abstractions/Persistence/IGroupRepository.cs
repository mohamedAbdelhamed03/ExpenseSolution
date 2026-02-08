using System;
using System.Threading;
using System.Threading.Tasks;
using Expense.Core.Domain.Entities;
using System.Collections.Generic;

namespace Expense.Core.Abstractions.Persistence
{
    public interface IGroupRepository : IRepository<Group>
    {
        Task<Group?> GetWithMembersAsync(Guid groupId, CancellationToken cancellationToken);
        Task<Group?> GetByInviteCodeAsync(string inviteCode, CancellationToken cancellationToken);
        Task<bool> IsMemberAsync(Guid groupId, string userId, CancellationToken cancellationToken);
        Task<IEnumerable<Group>> GetGroupsForUserAsync(string userId, CancellationToken cancellationToken);
        Task<GroupMember?> GetMemberAsync(Guid groupId, string userId, CancellationToken cancellationToken);
        void AddMember(GroupMember member);
        void UpdateMember(GroupMember member);
        void RemoveMember(GroupMember member);
    }
}
