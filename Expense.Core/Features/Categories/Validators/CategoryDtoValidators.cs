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

    public class UpdateCategoryPatchDtoValidator : AbstractValidator<UpdateCategoryPatchDto>
    {
        public UpdateCategoryPatchDtoValidator()
        {
             RuleFor(x => x)
                .Must(x => x.Name != null)
                .WithErrorCode("Patch.NoFieldsProvided")
                .WithMessage("At least one field must be provided for update");

            RuleFor(x => x.Name)
                .NotEmpty().When(x => x.Name != null).WithErrorCode("Category.Name.Required")
                .MaximumLength(50).When(x => x.Name != null).WithErrorCode("Category.Name.MaxLength");
        }
    }
}
