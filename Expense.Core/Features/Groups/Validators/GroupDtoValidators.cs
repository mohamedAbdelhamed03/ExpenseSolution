using Expense.Core.DTOs.Groups;
using Expense.Core.Features.Common;
using FluentValidation;
using Microsoft.Extensions.Localization;

namespace Expense.Core.Features.Groups.Validators
{
    public class GroupDtoValidators { }

    public class CreateGroupDtoValidator : LocalizedAbstractValidator<CreateGroupDto>
    {
        public CreateGroupDtoValidator(IStringLocalizer<GroupDtoValidators> localizer) : base(localizer)
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage(_localizer["NameRequired"])
                .MaximumLength(100).WithMessage(_localizer["NameMaxLength"]);
        }
    }
}
