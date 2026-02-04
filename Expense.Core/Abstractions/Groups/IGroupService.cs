using System;
using System.Threading.Tasks;
using Expense.Core.DTOs.Groups;

namespace Expense.Core.Abstractions.Groups
{
    public interface IGroupService
    {
        Task<GroupDto> CreateGroupAsync(string creatorUserId, CreateGroupDto dto);
        Task<GroupDto?> GetGroupAsync(Guid groupId, string requesterUserId);
        Task<bool> JoinGroupAsync(string userId, string inviteCode);
    }
}
