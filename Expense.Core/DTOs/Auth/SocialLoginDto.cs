using Expense.Core.Domain.Enums;

namespace Expense.Core.DTOs.Auth
{
    public class SocialLoginDto
    {
        public string Token { get; set; } = string.Empty;
        
        public AuthProvider Provider { get; set; }
    }
}
