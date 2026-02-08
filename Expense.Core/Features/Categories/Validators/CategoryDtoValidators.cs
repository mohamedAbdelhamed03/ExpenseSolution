using Expense.Core.DTOs.Categories;
using FluentValidation;

namespace Expense.Core.Features.Categories.Validators
{
    public class CreateExpenseCategoryDtoValidator : AbstractValidator<CreateExpenseCategoryDto>
    {
        public CreateExpenseCategoryDtoValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithErrorCode("Category.Name.Required")
                .MaximumLength(50).WithErrorCode("Category.Name.MaxLength");

            RuleFor(x => x.Description)
                .MaximumLength(200).WithErrorCode("Category.Description.MaxLength");
        }
    }

    public class UpdateExpenseCategoryDtoValidator : AbstractValidator<UpdateExpenseCategoryDto>
    {
        public UpdateExpenseCategoryDtoValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithErrorCode("Category.Name.Required")
                .MaximumLength(50).WithErrorCode("Category.Name.MaxLength");

            RuleFor(x => x.Description)
                .MaximumLength(200).WithErrorCode("Category.Description.MaxLength");
        }
    }
}
