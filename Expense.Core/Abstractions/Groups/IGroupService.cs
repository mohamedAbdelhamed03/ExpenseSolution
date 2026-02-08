using System;
using System.Threading.Tasks;
using Expense.Core.DTOs.Groups;

namespace Expense.Core.Abstractions.Groups
{
    public interface IGroupService
    {
        Task<GroupDto> CreateGroupAsync(string creatorUserId, CreateGroupDto dto, CancellationToken cancellationToken);
        Task<IEnumerable<GroupDto>> GetUserGroupsAsync(string userId, CancellationToken cancellationToken);
        Task<GroupDto?> GetGroupAsync(Guid groupId, string requesterUserId, CancellationToken cancellationToken);
        Task<bool> JoinGroupAsync(string userId, string inviteCode, CancellationToken cancellationToken);
        Task<bool> UpdateMemberRoleAsync(Guid groupId, string requesterUserId, string targetUserId, string newRole, CancellationToken cancellationToken);
        Task<bool> RemoveMemberAsync(Guid groupId, string requesterUserId, string targetUserId, CancellationToken cancellationToken);
    }
}
