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
                .NotEmpty().WithErrorCode("Expense.Description.Required")
                .MaximumLength(500).WithErrorCode("Expense.Description.MaxLength");

            RuleFor(x => x.ExpenseDate)
                .NotEmpty().WithErrorCode("Expense.Date.Required")
                .LessThanOrEqualTo(DateTime.UtcNow.AddDays(1)).WithErrorCode("Expense.Date.Future");

            RuleForEach(x => x.Splits).SetValidator(new ExpenseSplitDtoValidator());

            RuleFor(x => x.Currency)
                .NotEmpty().WithErrorCode("Expense.Currency.Required")
                .Length(3).WithErrorCode("Expense.Currency.InvalidFormat");

            RuleFor(x => x.ExchangeRate)
                .GreaterThan(0).When(x => x.ExchangeRate.HasValue)
                .WithErrorCode("Expense.ExchangeRate.Positive");

            RuleFor(x => x)
                .Must(x => x.Splits == null || !x.Splits.Any() || Math.Abs(x.Splits.Sum(s => s.Amount) - x.Amount) < 0.01m)
                .WithErrorCode("Expense.Splits.SumMismatch");
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

    public class UpdateExpenseDtoValidator : AbstractValidator<UpdateExpenseDto>
    {
        public UpdateExpenseDtoValidator()
        {
            RuleFor(x => x.Amount)
                .GreaterThan(0).WithErrorCode("Expense.Amount.Positive");

            RuleFor(x => x.Description)
                .NotEmpty().WithErrorCode("Expense.Description.Required")
                .MaximumLength(500).WithErrorCode("Expense.Description.MaxLength");

            RuleFor(x => x.ExpenseDate)
                .NotEmpty().WithErrorCode("Expense.Date.Required")
                .LessThanOrEqualTo(DateTime.UtcNow.AddDays(1)).WithErrorCode("Expense.Date.Future");

            RuleFor(x => x.Currency)
                .NotEmpty().WithErrorCode("Expense.Currency.Required")
                .Length(3).WithErrorCode("Expense.Currency.InvalidFormat");
                
            RuleFor(x => x.ExchangeRate)
                .GreaterThan(0).When(x => x.ExchangeRate.HasValue)
                .WithErrorCode("Expense.ExchangeRate.Positive");
                
             RuleForEach(x => x.Splits).SetValidator(new ExpenseSplitDtoValidator());

             RuleFor(x => x)
                .Must(x => x.Splits == null || !x.Splits.Any() || Math.Abs(x.Splits.Sum(s => s.Amount) - x.Amount) < 0.01m)
                .WithErrorCode("Expense.Splits.SumMismatch");
        }
    }

    public class UpdateExpensePatchDtoValidator : AbstractValidator<UpdateExpensePatchDto>
    {
        public UpdateExpensePatchDtoValidator()
        {
            RuleFor(x => x)
                .Must(x => x.Description != null || x.CategoryId.HasValue || x.ExpenseDate.HasValue)
                .WithErrorCode("Patch.NoFieldsProvided");

            RuleFor(x => x.Description)
                .NotEmpty().When(x => x.Description != null).WithErrorCode("Expense.Description.Required")
                .MaximumLength(500).When(x => x.Description != null).WithErrorCode("Expense.Description.MaxLength");

            RuleFor(x => x.ExpenseDate)
                .LessThanOrEqualTo(DateTime.UtcNow.AddDays(1)).When(x => x.ExpenseDate.HasValue).WithErrorCode("Expense.Date.Future");
        }
    }
}
