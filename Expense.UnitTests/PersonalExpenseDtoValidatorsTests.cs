using Expense.Core.DTOs.Personal;
using Expense.Core.Features.Personal.Validators;
using FluentValidation.TestHelper;
using System;
using Xunit;

namespace Expense.UnitTests
{
    public class PersonalExpenseDtoValidatorsTests
    {
        private readonly CreatePersonalExpenseDtoValidator _createValidator;
        private readonly UpdatePersonalExpenseDtoValidator _updateValidator;
        private readonly UpdatePersonalExpensePatchDtoValidator _patchValidator;

        public PersonalExpenseDtoValidatorsTests()
        {
            _createValidator = new CreatePersonalExpenseDtoValidator();
            _updateValidator = new UpdatePersonalExpenseDtoValidator();
            _patchValidator = new UpdatePersonalExpensePatchDtoValidator();
        }

        [Fact]
        public void Create_ShouldHaveError_WhenAmountIsZeroOrNegative()
        {
            var dto = new CreatePersonalExpenseDto { Amount = 0 };
            var result = _createValidator.TestValidate(dto);
            result.ShouldHaveValidationErrorFor(x => x.Amount);

            dto.Amount = -1;
            result = _createValidator.TestValidate(dto);
            result.ShouldHaveValidationErrorFor(x => x.Amount);
        }

        [Fact]
        public void Create_ShouldHaveError_WhenCurrencyIsInvalid()
        {
            var dto = new CreatePersonalExpenseDto { Currency = "" };
            var result = _createValidator.TestValidate(dto);
            result.ShouldHaveValidationErrorFor(x => x.Currency);

            dto.Currency = "US"; // Too short
            result = _createValidator.TestValidate(dto);
            result.ShouldHaveValidationErrorFor(x => x.Currency);

            dto.Currency = "USDD"; // Too long
            result = _createValidator.TestValidate(dto);
            result.ShouldHaveValidationErrorFor(x => x.Currency);
        }

        [Fact]
        public void Create_ShouldHaveError_WhenDateIsEmpty()
        {
            var dto = new CreatePersonalExpenseDto { Date = default };
            var result = _createValidator.TestValidate(dto);
            result.ShouldHaveValidationErrorFor(x => x.Date);
        }

        [Fact]
        public void Create_ShouldHaveError_WhenDescriptionIsTooLong()
        {
            var dto = new CreatePersonalExpenseDto { Description = new string('a', 501) };
            var result = _createValidator.TestValidate(dto);
            result.ShouldHaveValidationErrorFor(x => x.Description);
        }

        [Fact]
        public void Create_ShouldNotHaveError_WhenDtoIsValid()
        {
            var dto = new CreatePersonalExpenseDto
            {
                Amount = 100,
                Currency = "USD",
                Date = DateTime.UtcNow,
                Description = "Valid Description"
            };
            var result = _createValidator.TestValidate(dto);
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Patch_ShouldHaveError_WhenAmountIsInvalid_IfProvided()
        {
            var dto = new UpdatePersonalExpensePatchDto { Amount = 0 };
            var result = _patchValidator.TestValidate(dto);
            result.ShouldHaveValidationErrorFor(x => x.Amount);
        }

        [Fact]
        public void Patch_ShouldNotHaveError_WhenAmountIsNull()
        {
            var dto = new UpdatePersonalExpensePatchDto { Amount = null };
            var result = _patchValidator.TestValidate(dto);
            result.ShouldNotHaveValidationErrorFor(x => x.Amount);
        }

        [Fact]
        public void Patch_ShouldHaveError_WhenCurrencyIsInvalid_IfProvided()
        {
            var dto = new UpdatePersonalExpensePatchDto { Currency = "US" };
            var result = _patchValidator.TestValidate(dto);
            result.ShouldHaveValidationErrorFor(x => x.Currency);
        }

        [Fact]
        public void Patch_ShouldNotHaveError_WhenCurrencyIsNull()
        {
            var dto = new UpdatePersonalExpensePatchDto { Currency = null };
            var result = _patchValidator.TestValidate(dto);
            result.ShouldNotHaveValidationErrorFor(x => x.Currency);
        }
    }
}
