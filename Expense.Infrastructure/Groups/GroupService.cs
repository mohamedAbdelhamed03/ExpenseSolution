using System;
using System.Linq;
using System.Threading.Tasks;
using Expense.Core.Abstractions.Groups;
using Expense.Core.Abstractions.Persistence;
using Expense.Core.Domain.Entities;
using Expense.Core.DTOs.Groups;
using Microsoft.EntityFrameworkCore;

namespace Expense.Infrastructure.Groups
{
    public class GroupService : IGroupService
    {
        private readonly IApplicationDbContext _context;
        public GroupService(IApplicationDbContext context)
        {
            _context = context;
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
            _context.Groups.Add(group);
            var member = new GroupMember
            {
                Group = group,
                UserId = creatorUserId,
                Role = GroupRole.Admin
            };
            _context.GroupMembers.Add(member);
            await _context.SaveChangesAsync();
            var result = await _context.Groups
                .Where(g => g.Id == group.Id)
                .Select(g => new GroupDto
                {
                    Id = g.Id,
                    Name = g.Name,
                    InviteCode = g.InviteCode,
                    Members = g.Members.Select(m => new GroupMemberDto
                    {
                        UserId = m.UserId,
                        Role = m.Role.ToString()
                    })
                })
                .FirstAsync();
            return result;
        }

        public async Task<GroupDto?> GetGroupAsync(Guid groupId, string requesterUserId)
        {
            var isMember = await _context.GroupMembers.AnyAsync(m => m.GroupId == groupId && m.UserId == requesterUserId);
            if (!isMember) return null;
            var group = await _context.Groups
                .Where(g => g.Id == groupId)
                .Select(g => new GroupDto
                {
                    Id = g.Id,
                    Name = g.Name,
                    InviteCode = g.InviteCode,
                    Members = g.Members.Select(m => new GroupMemberDto
                    {
                        UserId = m.UserId,
                        Role = m.Role.ToString()
                    })
                })
                .FirstOrDefaultAsync();
            return group;
        }

        public async Task<bool> JoinGroupAsync(string userId, string inviteCode)
        {
            var group = await _context.Groups.FirstOrDefaultAsync(g => g.InviteCode == inviteCode);
            if (group == null) return false;
            var exists = await _context.GroupMembers.AnyAsync(m => m.GroupId == group.Id && m.UserId == userId);
            if (exists) return true;
            var member = new GroupMember
            {
                GroupId = group.Id,
                UserId = userId,
                Role = GroupRole.Member
            };
            _context.GroupMembers.Add(member);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
