using FluentValidation;
using Expense.Core.DTOs.Personal;

namespace Expense.Core.Features.Personal.Validators
{
    public class CreatePersonalExpenseDtoValidator : AbstractValidator<CreatePersonalExpenseDto>
    {
        public CreatePersonalExpenseDtoValidator()
        {
            RuleFor(x => x.Amount)
                .GreaterThan(0)
                .WithErrorCode("PersonalExpense.Amount.Invalid");

            RuleFor(x => x.Currency)
                .NotEmpty()
                .Length(3)
                .WithErrorCode("PersonalExpense.Currency.Invalid");

            RuleFor(x => x.Date)
                .NotEmpty()
                .WithErrorCode("PersonalExpense.Date.Required");

            RuleFor(x => x.Description)
                .MaximumLength(500)
                .WithErrorCode("PersonalExpense.Description.TooLong");

            RuleFor(x => x.CategoryId)
                .NotEqual(Guid.Empty)
                .When(x => x.CategoryId.HasValue)
                .WithErrorCode("PersonalExpense.Category.Invalid");
        }
    }

    public class UpdatePersonalExpenseDtoValidator : AbstractValidator<UpdatePersonalExpenseDto>
    {
        public UpdatePersonalExpenseDtoValidator()
        {
            RuleFor(x => x.Amount)
                .GreaterThan(0)
                .WithErrorCode("PersonalExpense.Amount.Invalid");

            RuleFor(x => x.Currency)
                .NotEmpty()
                .Length(3)
                .WithErrorCode("PersonalExpense.Currency.Invalid");

            RuleFor(x => x.Date)
                .NotEmpty()
                .WithErrorCode("PersonalExpense.Date.Required");

            RuleFor(x => x.Description)
                .MaximumLength(500)
                .WithErrorCode("PersonalExpense.Description.TooLong");

            RuleFor(x => x.CategoryId)
                .NotEqual(Guid.Empty)
                .When(x => x.CategoryId.HasValue)
                .WithErrorCode("PersonalExpense.Category.Invalid");
        }
    }

    public class UpdatePersonalExpensePatchDtoValidator : AbstractValidator<UpdatePersonalExpensePatchDto>
    {
        public UpdatePersonalExpensePatchDtoValidator()
        {
            RuleFor(x => x.Amount)
                .GreaterThan(0)
                .When(x => x.Amount.HasValue)
                .WithErrorCode("PersonalExpense.Amount.Invalid");

            RuleFor(x => x.Currency)
                .Length(3)
                .When(x => !string.IsNullOrEmpty(x.Currency))
                .WithErrorCode("PersonalExpense.Currency.Invalid");

            RuleFor(x => x.Description)
                .MaximumLength(500)
                .When(x => !string.IsNullOrEmpty(x.Description))
                .WithErrorCode("PersonalExpense.Description.TooLong");

            RuleFor(x => x.CategoryId)
                .NotEqual(Guid.Empty)
                .When(x => x.CategoryId.HasValue)
                .WithErrorCode("PersonalExpense.Category.Invalid");
        }
    }
}
