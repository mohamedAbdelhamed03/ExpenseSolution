namespace Expense.Core.DTOs.Groups
{
    public class CreateGroupDto
    {
        public string Name { get; set; } = string.Empty;
        public string? LogoUrl { get; set; }
    }
}
