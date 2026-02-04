using System;

namespace Expense.Core.Domain.Entities
{
    public enum GroupRole
    {
        Admin = 1,
        Member = 2
    }

    public class GroupMember
    {
        public Guid Id { get; set; }
        public Guid GroupId { get; set; }
        public string UserId { get; set; } = string.Empty;
        public GroupRole Role { get; set; } = GroupRole.Member;
        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
        public Group? Group { get; set; }
    }
}
