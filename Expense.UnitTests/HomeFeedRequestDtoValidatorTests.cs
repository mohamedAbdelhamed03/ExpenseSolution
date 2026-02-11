using Expense.Core.DTOs.Home;
using Expense.Core.Features.Home.Validators;
using FluentValidation.TestHelper;
using Xunit;

namespace Expense.UnitTests
{
    public class HomeFeedRequestDtoValidatorTests
    {
        private readonly HomeFeedRequestDtoValidator _validator;

        public HomeFeedRequestDtoValidatorTests()
        {
            _validator = new HomeFeedRequestDtoValidator();
        }

        [Fact]
        public void ShouldHaveError_WhenPageIsZeroOrNegative()
        {
            var dto = new HomeFeedRequestDto { Page = 0 };
            var result = _validator.TestValidate(dto);
            result.ShouldHaveValidationErrorFor(x => x.Page);

            dto.Page = -1;
            result = _validator.TestValidate(dto);
            result.ShouldHaveValidationErrorFor(x => x.Page);
        }

        [Fact]
        public void ShouldHaveError_WhenPageSizeIsZeroOrNegative()
        {
            var dto = new HomeFeedRequestDto { PageSize = 0 };
            var result = _validator.TestValidate(dto);
            result.ShouldHaveValidationErrorFor(x => x.PageSize);

            dto.PageSize = -1;
            result = _validator.TestValidate(dto);
            result.ShouldHaveValidationErrorFor(x => x.PageSize);
        }

        [Fact]
        public void ShouldHaveError_WhenPageSizeIsGreaterThan100()
        {
            var dto = new HomeFeedRequestDto { PageSize = 101 };
            var result = _validator.TestValidate(dto);
            result.ShouldHaveValidationErrorFor(x => x.PageSize);
        }

        [Fact]
        public void ShouldNotHaveError_WhenDtoIsValid()
        {
            var dto = new HomeFeedRequestDto { Page = 1, PageSize = 20 };
            var result = _validator.TestValidate(dto);
            result.ShouldNotHaveAnyValidationErrors();
        }
    }
}
