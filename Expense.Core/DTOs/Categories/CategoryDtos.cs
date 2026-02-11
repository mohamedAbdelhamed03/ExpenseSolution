using System;

namespace Expense.Core.DTOs.Categories
{
    public class ExpenseCategoryDto
    {
        public Guid Id { get; set; }
        public Guid GroupId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? LogoUrl { get; set; }
        public bool IsSystem { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CreateExpenseCategoryDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? LogoUrl { get; set; }
    }

    public class UpdateExpenseCategoryDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? LogoUrl { get; set; }
    }
}