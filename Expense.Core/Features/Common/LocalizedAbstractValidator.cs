using FluentValidation;
using Microsoft.Extensions.Localization;

namespace Expense.Core.Features.Common
{
    public abstract class LocalizedAbstractValidator<T> : AbstractValidator<T>
    {
        protected readonly IStringLocalizer _localizer;

        protected LocalizedAbstractValidator(IStringLocalizer localizer)
        {
            _localizer = localizer;
        }
    }
}
