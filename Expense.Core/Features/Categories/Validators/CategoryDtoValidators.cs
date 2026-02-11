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

            RuleFor(x => x.Icon)
                .Matches(@"^[\u00a9\u00ae\u2000-\u3300\ud83c\ud000-\udfff\ud83d\ud000-\udfff\ud83e\ud000-\udfff]+$")
                .When(x => !string.IsNullOrEmpty(x.Icon))
                .WithErrorCode("Category.Icon.Invalid")
                .WithMessage("Icon must be a valid emoji");
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

            RuleFor(x => x.Icon)
                .Matches(@"^[\u00a9\u00ae\u2000-\u3300\ud83c\ud000-\udfff\ud83d\ud000-\udfff\ud83e\ud000-\udfff]+$")
                .When(x => !string.IsNullOrEmpty(x.Icon))
                .WithErrorCode("Category.Icon.Invalid")
                .WithMessage("Icon must be a valid emoji");
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
