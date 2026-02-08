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

    public class UpdateGroupMemberRolePatchDtoValidator : AbstractValidator<UpdateGroupMemberRolePatchDto>
    {
        public UpdateGroupMemberRolePatchDtoValidator()
        {
             RuleFor(x => x)
                .Must(x => x.Role != null)
                .WithErrorCode("Patch.NoFieldsProvided")
                .WithMessage("At least one field must be provided for update");

            RuleFor(x => x.Role)
                .NotEmpty().When(x => x.Role != null).WithErrorCode("Group.Role.Required")
                .IsEnumName(typeof(Expense.Core.Domain.Entities.GroupRole), caseSensitive: false)
                .When(x => x.Role != null).WithErrorCode("Group.Role.Invalid");
        }
    }
}
