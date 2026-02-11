using Expense.Core.DTOs.Home;
using FluentValidation;

namespace Expense.Core.Features.Home.Validators
{
    public class HomeFeedRequestDtoValidator : AbstractValidator<HomeFeedRequestDto>
    {
        public HomeFeedRequestDtoValidator()
        {
            RuleFor(x => x.Page)
                .GreaterThan(0)
                .WithErrorCode("HomeFeed.Page.Invalid");

            RuleFor(x => x.PageSize)
                .GreaterThan(0)
                .LessThanOrEqualTo(100)
                .WithErrorCode("HomeFeed.PageSize.Invalid");
        }
    }
}
