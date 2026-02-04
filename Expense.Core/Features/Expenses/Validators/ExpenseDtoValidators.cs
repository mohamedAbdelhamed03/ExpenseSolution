using Expense.Core.DTOs.Expenses;
using FluentValidation;

namespace Expense.Core.Features.Expenses.Validators
{
    public class CreateExpenseDtoValidator : AbstractValidator<CreateExpenseDto>
    {
        public CreateExpenseDtoValidator()
        {
            RuleFor(x => x.Amount)
                .GreaterThan(0).WithErrorCode("Expense.Amount.Positive");

            RuleFor(x => x.Description)
                .MaximumLength(500).WithErrorCode("Expense.Description.MaxLength");

            RuleFor(x => x.ExpenseDate)
                .NotEmpty().WithErrorCode("Expense.Date.Required");

            RuleFor(x => x.Splits)
                .NotEmpty().WithErrorCode("Expense.Splits.Required");

            RuleForEach(x => x.Splits).SetValidator(new ExpenseSplitDtoValidator());
        }
    }

    public class ExpenseSplitDtoValidator : AbstractValidator<ExpenseSplitDto>
    {
        public ExpenseSplitDtoValidator()
        {
            RuleFor(x => x.UserId)
                .NotEmpty().WithErrorCode("Expense.Split.UserId.Required");

            RuleFor(x => x.Amount)
                .GreaterThan(0).WithErrorCode("Expense.Split.Amount.Positive");
        }
    }
}
