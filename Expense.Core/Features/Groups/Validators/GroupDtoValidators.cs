using Expense.Core.DTOs.Groups;
using FluentValidation;

namespace Expense.Core.Features.Groups.Validators
{
    public class CreateGroupDtoValidator : AbstractValidator<CreateGroupDto>
    {
        public CreateGroupDtoValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithErrorCode("Group.Name.Required")
                .MaximumLength(100).WithErrorCode("Group.Name.MaxLength");
        }
    }
}
