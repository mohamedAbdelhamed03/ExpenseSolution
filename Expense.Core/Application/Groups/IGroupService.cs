using System;
using System.Threading.Tasks;
using Expense.Core.DTOs.Groups;

namespace Expense.Core.Application.Groups
{
    public interface IGroupService
    {
        Task<GroupDto> CreateGroupAsync(string creatorUserId, CreateGroupDto dto, CancellationToken cancellationToken);
        Task<IEnumerable<GroupDto>> GetUserGroupsAsync(string userId, CancellationToken cancellationToken);
        Task<GroupDto?> GetGroupAsync(Guid groupId, string requesterUserId, CancellationToken cancellationToken);
        Task<bool> JoinGroupAsync(string userId, string inviteCode, CancellationToken cancellationToken);
        Task<bool> AddMemberByEmailAsync(Guid groupId, string requesterUserId, AddGroupMemberDto dto, CancellationToken cancellationToken);
        Task<bool> UpdateMemberRoleAsync(Guid groupId, string requesterUserId, string targetUserId, string newRole, CancellationToken cancellationToken);
        Task<bool> UpdateMemberRolePartialAsync(Guid groupId, string requesterUserId, string targetUserId, UpdateGroupMemberRolePatchDto dto, CancellationToken cancellationToken);
        Task<bool> RemoveMemberAsync(Guid groupId, string requesterUserId, string targetUserId, CancellationToken cancellationToken);
    }
}
