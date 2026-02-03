using Expense.Core.DTOs.Auth;
using FluentValidation;
using Microsoft.Extensions.Localization;

namespace Expense.Core.Features.Auth.Validators
{
    // Marker class for resources
    public class AuthDtoValidators { }

    public class RegisterDtoValidator : LocalizedAbstractValidator<RegisterDto>
    {
        public RegisterDtoValidator(IStringLocalizer<AuthDtoValidators> localizer) : base(localizer)
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage(_localizer["EmailRequired"])
                .EmailAddress().WithMessage(_localizer["EmailInvalid"]);

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage(_localizer["PasswordRequired"])
                .MinimumLength(6).WithMessage(_localizer["PasswordMinLength"])
                .Matches(@"[A-Z]").WithMessage(_localizer["PasswordUppercase"])
                .Matches(@"[a-z]").WithMessage(_localizer["PasswordLowercase"])
                .Matches(@"[0-9]").WithMessage(_localizer["PasswordDigit"]);

            RuleFor(x => x.ConfirmPassword)
                .Equal(x => x.Password).WithMessage(_localizer["PasswordsDoNotMatch"]);

            RuleFor(x => x.FirstName)
                .NotEmpty().WithMessage(_localizer["FirstNameRequired"])
                .MaximumLength(50).WithMessage(_localizer["FirstNameMaxLength"]);

            RuleFor(x => x.LastName)
                .NotEmpty().WithMessage(_localizer["LastNameRequired"])
                .MaximumLength(50).WithMessage(_localizer["LastNameMaxLength"]);

            RuleFor(x => x.PhoneNumber)
                .MaximumLength(20).WithMessage(_localizer["PhoneNumberMaxLength"])
                .When(x => !string.IsNullOrEmpty(x.PhoneNumber));
        }
    }

    public class LoginDtoValidator : LocalizedAbstractValidator<LoginDto>
    {
        public LoginDtoValidator(IStringLocalizer<AuthDtoValidators> localizer) : base(localizer)
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage(_localizer["EmailRequired"])
                .EmailAddress().WithMessage(_localizer["EmailInvalid"]);

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage(_localizer["PasswordRequired"]);
        }
    }

    public class RefreshTokenDtoValidator : LocalizedAbstractValidator<RefreshTokenDto>
    {
        public RefreshTokenDtoValidator(IStringLocalizer<AuthDtoValidators> localizer) : base(localizer)
        {
            RuleFor(x => x.AccessToken)
                .NotEmpty().WithMessage(_localizer["AccessTokenRequired"]);

            RuleFor(x => x.RefreshToken)
                .NotEmpty().WithMessage(_localizer["RefreshTokenRequired"]);
        }
    }

    public class ChangePasswordDtoValidator : LocalizedAbstractValidator<ChangePasswordDto>
    {
        public ChangePasswordDtoValidator(IStringLocalizer<AuthDtoValidators> localizer) : base(localizer)
        {
            RuleFor(x => x.CurrentPassword)
                .NotEmpty().WithMessage(_localizer["CurrentPasswordRequired"]);

            RuleFor(x => x.NewPassword)
                .NotEmpty().WithMessage(_localizer["NewPasswordRequired"])
                .MinimumLength(6).WithMessage(_localizer["PasswordMinLength"])
                .Matches(@"[A-Z]").WithMessage(_localizer["PasswordUppercase"])
                .Matches(@"[a-z]").WithMessage(_localizer["PasswordLowercase"])
                .Matches(@"[0-9]").WithMessage(_localizer["PasswordDigit"]);

            RuleFor(x => x.ConfirmPassword)
                .Equal(x => x.NewPassword).WithMessage(_localizer["PasswordsDoNotMatch"]);
        }
    }

    public class ResetPasswordDtoValidator : LocalizedAbstractValidator<ResetPasswordDto>
    {
        public ResetPasswordDtoValidator(IStringLocalizer<AuthDtoValidators> localizer) : base(localizer)
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage(_localizer["EmailRequired"])
                .EmailAddress().WithMessage(_localizer["EmailInvalid"]);

            RuleFor(x => x.Token)
                .NotEmpty().WithMessage(_localizer["TokenRequired"]);

            RuleFor(x => x.NewPassword)
                .NotEmpty().WithMessage(_localizer["NewPasswordRequired"])
                .MinimumLength(6).WithMessage(_localizer["PasswordMinLength"])
                .Matches(@"[A-Z]").WithMessage(_localizer["PasswordUppercase"])
                .Matches(@"[a-z]").WithMessage(_localizer["PasswordLowercase"])
                .Matches(@"[0-9]").WithMessage(_localizer["PasswordDigit"]);
                
            RuleFor(x => x.ConfirmPassword)
                .Equal(x => x.NewPassword).WithMessage(_localizer["PasswordsDoNotMatch"]);
        }
    }

    public class ForgotPasswordDtoValidator : LocalizedAbstractValidator<ForgotPasswordDto>
    {
        public ForgotPasswordDtoValidator(IStringLocalizer<AuthDtoValidators> localizer) : base(localizer)
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage(_localizer["EmailRequired"])
                .EmailAddress().WithMessage(_localizer["EmailInvalid"]);
        }
    }
}