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

        public async Task<GroupDto> CreateGroupAsync(string creatorUserId, CreateGroupDto dto)
        {
            var inviteCode = Guid.NewGuid().ToString("N")[..8];
            var group = new Group
            {
                Name = dto.Name,
                CreatedByUserId = creatorUserId,
                InviteCode = inviteCode
            };
            
            await _unitOfWork.Repository<Group>().Add(group);
            
            var member = new GroupMember
            {
                Group = group,
                UserId = creatorUserId,
                Role = GroupRole.Admin
            };
            
            await _unitOfWork.Repository<GroupMember>().Add(member);
            await _unitOfWork.SaveAsync();

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

        public async Task<GroupDto?> GetGroupAsync(Guid groupId, string requesterUserId)
        {
            var member = await _unitOfWork.Repository<GroupMember>()
                .Get(m => m.GroupId == groupId && m.UserId == requesterUserId);
                
            if (member == null) return null;

            var group = await _unitOfWork.Repository<Group>()
                .Get(g => g.Id == groupId, includes: new[] { "Members" });

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

        public async Task<bool> JoinGroupAsync(string userId, string inviteCode)
        {
            var group = await _unitOfWork.Repository<Group>()
                .Get(g => g.InviteCode == inviteCode);
                
            if (group == null) return false;

            var existingMember = await _unitOfWork.Repository<GroupMember>()
                .Get(m => m.GroupId == group.Id && m.UserId == userId);
                
            if (existingMember != null) return true;

            var member = new GroupMember
            {
                GroupId = group.Id,
                UserId = userId,
                Role = GroupRole.Member
            };
            
            await _unitOfWork.Repository<GroupMember>().Add(member);
            await _unitOfWork.SaveAsync();
            
            return true;
        }
    }
}