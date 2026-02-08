using System;
using Expense.Core.DTOs.Settlements;
using FluentValidation;

namespace Expense.Core.Features.Settlements.Validators
{
    public class CreateSettlementDtoValidator : AbstractValidator<CreateSettlementDto>
    {
        public CreateSettlementDtoValidator()
        {
            RuleFor(x => x.PayeeUserId)
                .NotEmpty().WithErrorCode("Settlement.Payee.Required");

            RuleFor(x => x.Amount)
                .GreaterThan(0).WithErrorCode("Settlement.Amount.Positive");

            RuleFor(x => x.SettlementDate)
                .NotEmpty().WithErrorCode("Settlement.Date.Required")
                .LessThanOrEqualTo(DateTime.UtcNow.AddDays(1)).WithErrorCode("Settlement.Date.Future");

            RuleFor(x => x.Currency)
                .NotEmpty().WithErrorCode("Settlement.Currency.Required")
                .Length(3).WithErrorCode("Settlement.Currency.InvalidFormat")
                .Matches("^[A-Z]{3}$").WithErrorCode("Settlement.Currency.InvalidFormat");

            RuleFor(x => x.ExchangeRate)
                .GreaterThan(0).When(x => x.ExchangeRate.HasValue).WithErrorCode("Settlement.ExchangeRate.Positive");
        }
    }
}
