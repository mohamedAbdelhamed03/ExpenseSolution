using System;
using System.Collections.Generic;

namespace Expense.Core.DTOs.Groups
{
    public class GroupDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string InviteCode { get; set; } = string.Empty;
        public IEnumerable<GroupMemberDto> Members { get; set; } = Array.Empty<GroupMemberDto>();
    }

    public class GroupMemberDto
    {
        public string UserId { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
    }
}
