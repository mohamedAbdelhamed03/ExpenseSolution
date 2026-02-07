using System;
using System.Linq;
using System.Threading.Tasks;
using Expense.Core.Abstractions.Groups;
using Expense.Core.Abstractions.Persistence;
using Expense.Core.Domain.Entities;
using Expense.Core.DTOs.Groups;

namespace Expense.Infrastructure.Groups
{
    public class GroupService : IGroupService
    {
        private readonly IUnitOfWork _unitOfWork;
        
        public GroupService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<GroupDto> CreateGroupAsync(string creatorUserId, CreateGroupDto dto, CancellationToken cancellationToken)
        {
            var inviteCode = Guid.NewGuid().ToString("N")[..8];
            var group = new Group
            {
                Name = dto.Name,
                CreatedByUserId = creatorUserId,
                InviteCode = inviteCode
            };
            
            _unitOfWork.Groups.Add(group);
            
            var member = new GroupMember
            {
                Group = group,
                UserId = creatorUserId,
                Role = GroupRole.Admin
            };
            
            _unitOfWork.Groups.AddMember(member);
            await _unitOfWork.SaveAsync(cancellationToken);

            // Map directly from entity since we have all data in memory
            return new GroupDto
            {
                Id = group.Id,
                Name = group.Name,
                InviteCode = group.InviteCode,
                Members = new[] 
                { 
                    new GroupMemberDto 
                    { 
                        UserId = member.UserId, 
                        Role = member.Role.ToString() 
                    } 
                }
            };
        }

        public async Task<GroupDto?> GetGroupAsync(Guid groupId, string requesterUserId, CancellationToken cancellationToken)
        {
            var isMember = await _unitOfWork.Groups.IsMemberAsync(groupId, requesterUserId, cancellationToken);
                
            if (!isMember) return null;

            var group = await _unitOfWork.Groups.GetWithMembersAsync(groupId, cancellationToken);

            if (group == null) return null;

            return new GroupDto
            {
                Id = group.Id,
                Name = group.Name,
                InviteCode = group.InviteCode,
                Members = group.Members.Select(m => new GroupMemberDto
                {
                    UserId = m.UserId,
                    Role = m.Role.ToString()
                })
            };
        }

        public async Task<bool> JoinGroupAsync(string userId, string inviteCode, CancellationToken cancellationToken)
        {
            var group = await _unitOfWork.Groups.GetByInviteCodeAsync(inviteCode, cancellationToken);
                
            if (group == null) return false;

            var existingMember = await _unitOfWork.Groups.IsMemberAsync(group.Id, userId, cancellationToken);
                
            if (existingMember) return true;

            var member = new GroupMember
            {
                GroupId = group.Id,
                UserId = userId,
                Role = GroupRole.Member
            };
            
            _unitOfWork.Groups.AddMember(member);
            await _unitOfWork.SaveAsync(cancellationToken);
            
            return true;
        }
    }
}
