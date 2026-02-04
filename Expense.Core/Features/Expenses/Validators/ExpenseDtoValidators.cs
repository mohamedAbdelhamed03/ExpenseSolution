using Expense.Core.DTOs.Expenses;
using Expense.Core.Features.Common;
using FluentValidation;
using Microsoft.Extensions.Localization;

namespace Expense.Core.Features.Expenses.Validators
{
    public class ExpenseDtoValidators { }

    public class CreateExpenseDtoValidator : LocalizedAbstractValidator<CreateExpenseDto>
    {
        public CreateExpenseDtoValidator(IStringLocalizer<ExpenseDtoValidators> localizer) : base(localizer)
        {
            RuleFor(x => x.Amount)
                .GreaterThan(0).WithMessage(_localizer["AmountGreaterThanZero"]);

            RuleFor(x => x.Description)
                .MaximumLength(500).WithMessage(_localizer["DescriptionMaxLength"]);

            RuleFor(x => x.ExpenseDate)
                .NotEmpty().WithMessage(_localizer["DateRequired"]);

            RuleFor(x => x.Splits)
                .NotEmpty().WithMessage(_localizer["SplitsRequired"]);

            RuleForEach(x => x.Splits).SetValidator(new ExpenseSplitDtoValidator(localizer));
        }
    }

    public class ExpenseSplitDtoValidator : LocalizedAbstractValidator<ExpenseSplitDto>
    {
        public ExpenseSplitDtoValidator(IStringLocalizer<ExpenseDtoValidators> localizer) : base(localizer)
        {
            RuleFor(x => x.UserId)
                .NotEmpty().WithMessage(_localizer["SplitUserIdRequired"]);

            RuleFor(x => x.Amount)
                .GreaterThan(0).WithMessage(_localizer["SplitAmountGreaterThanZero"]);
        }
    }
}
