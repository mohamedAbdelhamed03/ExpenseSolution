using Expense.Core.Abstractions.Persistence;
using Expense.Core.Domain.Entities;
using Expense.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Expense.Infrastructure.Repositories
{
    public class GroupRepository : Repository<Group>, IGroupRepository
    {
        private readonly ApplicationDbContext _db;

        public GroupRepository(ApplicationDbContext db) : base(db)
        {
            _db = db;
        }

        public async Task<Group?> GetWithMembersAsync(Guid groupId, CancellationToken cancellationToken)
        {
            return await _db.Groups
                .AsNoTracking()
                .Include(g => g.Members)
                .FirstOrDefaultAsync(g => g.Id == groupId, cancellationToken);
        }

        public async Task<Group?> GetByInviteCodeAsync(string inviteCode, CancellationToken cancellationToken)
        {
            return await _db.Groups
                .AsNoTracking()
                .FirstOrDefaultAsync(g => g.InviteCode == inviteCode, cancellationToken);
        }

        public async Task<bool> IsMemberAsync(Guid groupId, string userId, CancellationToken cancellationToken)
        {
            return await _db.GroupMembers
                .AsNoTracking()
                .AnyAsync(m => m.GroupId == groupId && m.UserId == userId, cancellationToken);
        }

        public async Task<IEnumerable<Group>> GetGroupsForUserAsync(string userId, CancellationToken cancellationToken)
        {
            return await _db.Groups
                .AsNoTracking()
                .Include(g => g.Members)
                .Where(g => g.Members.Any(m => m.UserId == userId))
                .ToListAsync(cancellationToken);
        }

        public async Task<GroupMember?> GetMemberAsync(Guid groupId, string userId, CancellationToken cancellationToken)
        {
            return await _db.GroupMembers
                .FirstOrDefaultAsync(m => m.GroupId == groupId && m.UserId == userId, cancellationToken);
        }

        public void AddMember(GroupMember member)
        {
            _db.GroupMembers.Add(member);
        }

        public void UpdateMember(GroupMember member)
        {
            _db.GroupMembers.Update(member);
        }

        public void RemoveMember(GroupMember member)
        {
            _db.GroupMembers.Remove(member);
        }
    }
}
