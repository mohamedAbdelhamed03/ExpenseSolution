namespace Expense.Core.DTOs.Categories
{
    public class UpdateCategoryPatchDto
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? LogoUrl { get; set; }
    }
}
