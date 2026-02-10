using Expense.Core.Domain.Enums;
using Microsoft.AspNetCore.Identity;

namespace Expense.Infrastructure.Identity
{
    public class ApplicationUser : IdentityUser<Guid>
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? ProfilePicture { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public int TokenVersion { get; set; } = 1;

        // Social Auth Fields
        public string? GoogleId { get; set; }
        public string? FacebookId { get; set; }
        public AuthProvider Provider { get; set; } = AuthProvider.Local;
    }
}