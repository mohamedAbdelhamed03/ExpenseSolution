using System;
using System.ComponentModel.DataAnnotations;

namespace Expense.Core.DTOs.Personal
{
    public class CreatePersonalExpenseDto
    {
        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
        public decimal Amount { get; set; }

        [Required]
        [StringLength(3)]
        public string Currency { get; set; } = "EGP";

        [Required]
        public DateTime Date { get; set; }

        [StringLength(500)]
        public string Description { get; set; } = string.Empty;
    }

    public class PersonalExpenseDto
    {
        public Guid Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Currency { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public string Description { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
