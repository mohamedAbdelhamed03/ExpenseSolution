using System;

namespace Expense.Core.DTOs.Personal
{
    public class CreatePersonalExpenseDto
    {
        public decimal Amount { get; set; }

        public string Currency { get; set; } = "EGP";

        public DateTime Date { get; set; }

        public string Description { get; set; } = string.Empty;
        public Guid? CategoryId { get; set; }
    }

    public class PersonalExpenseDto
    {
        public Guid Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Currency { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public string Description { get; set; } = string.Empty;
        public Guid? CategoryId { get; set; }
        public string? CategoryName { get; set; }
        public string? CategoryLogoUrl { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class UpdatePersonalExpenseDto : CreatePersonalExpenseDto
    {
    }

    public class UpdatePersonalExpensePatchDto
    {
        public decimal? Amount { get; set; }

        public string? Currency { get; set; }

        public DateTime? Date { get; set; }

        public string? Description { get; set; }

        public Guid? CategoryId { get; set; }
    }
}
