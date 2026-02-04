using Expense.Core.DTOs.Auth;
using FluentValidation;

namespace Expense.Core.Features.Auth.Validators
{
    public class RegisterDtoValidator : AbstractValidator<RegisterDto>
    {
        public RegisterDtoValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithErrorCode("Auth.Email.Required")
                .EmailAddress().WithErrorCode("Auth.Email.Invalid");

            RuleFor(x => x.Password)
                .NotEmpty().WithErrorCode("Auth.Password.Required")
                .MinimumLength(6).WithErrorCode("Auth.Password.MinLength")
                .Matches(@"[A-Z]").WithErrorCode("Auth.Password.Uppercase")
                .Matches(@"[a-z]").WithErrorCode("Auth.Password.Lowercase")
                .Matches(@"[0-9]").WithErrorCode("Auth.Password.Digit");

            RuleFor(x => x.ConfirmPassword)
                .Equal(x => x.Password).WithErrorCode("Auth.Password.Mismatch");

            RuleFor(x => x.FirstName)
                .NotEmpty().WithErrorCode("Auth.FirstName.Required")
                .MaximumLength(50).WithErrorCode("Auth.FirstName.MaxLength");

            RuleFor(x => x.LastName)
                .NotEmpty().WithErrorCode("Auth.LastName.Required")
                .MaximumLength(50).WithErrorCode("Auth.LastName.MaxLength");

            RuleFor(x => x.PhoneNumber)
                .MaximumLength(20).WithErrorCode("Auth.PhoneNumber.MaxLength")
                .When(x => !string.IsNullOrEmpty(x.PhoneNumber));
        }
    }

    public class LoginDtoValidator : AbstractValidator<LoginDto>
    {
        public LoginDtoValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithErrorCode("Auth.Email.Required")
                .EmailAddress().WithErrorCode("Auth.Email.Invalid");

            RuleFor(x => x.Password)
                .NotEmpty().WithErrorCode("Auth.Password.Required");
        }
    }

    public class RefreshTokenDtoValidator : AbstractValidator<RefreshTokenDto>
    {
        public RefreshTokenDtoValidator()
        {
            RuleFor(x => x.AccessToken)
                .NotEmpty().WithErrorCode("Auth.Token.AccessRequired");

            RuleFor(x => x.RefreshToken)
                .NotEmpty().WithErrorCode("Auth.Token.RefreshRequired");
        }
    }

    public class ChangePasswordDtoValidator : AbstractValidator<ChangePasswordDto>
    {
        public ChangePasswordDtoValidator()
        {
            RuleFor(x => x.CurrentPassword)
                .NotEmpty().WithErrorCode("Auth.Password.CurrentRequired");

            RuleFor(x => x.NewPassword)
                .NotEmpty().WithErrorCode("Auth.Password.NewRequired")
                .MinimumLength(6).WithErrorCode("Auth.Password.MinLength")
                .Matches(@"[A-Z]").WithErrorCode("Auth.Password.Uppercase")
                .Matches(@"[a-z]").WithErrorCode("Auth.Password.Lowercase")
                .Matches(@"[0-9]").WithErrorCode("Auth.Password.Digit");

            RuleFor(x => x.ConfirmPassword)
                .Equal(x => x.NewPassword).WithErrorCode("Auth.Password.Mismatch");
        }
    }

    public class SocialLoginDtoValidator : AbstractValidator<SocialLoginDto>
    {
        public SocialLoginDtoValidator()
        {
            RuleFor(x => x.Token)
                .NotEmpty().WithErrorCode("Auth.Token.Required");

            RuleFor(x => x.Provider)
                .IsInEnum().WithErrorCode("Auth.Provider.Invalid");
        }
    }
    public class ResetPasswordDtoValidator : AbstractValidator<ResetPasswordDto>
    {
        public ResetPasswordDtoValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithErrorCode("Auth.Email.Required")
                .EmailAddress().WithErrorCode("Auth.Email.Invalid");

            RuleFor(x => x.Token)
                .NotEmpty().WithErrorCode("Auth.Token.Required");

            RuleFor(x => x.NewPassword)
                .NotEmpty().WithErrorCode("Auth.Password.NewRequired")
                .MinimumLength(6).WithErrorCode("Auth.Password.MinLength")
                .Matches(@"[A-Z]").WithErrorCode("Auth.Password.Uppercase")
                .Matches(@"[a-z]").WithErrorCode("Auth.Password.Lowercase")
                .Matches(@"[0-9]").WithErrorCode("Auth.Password.Digit");
                
            RuleFor(x => x.ConfirmPassword)
                .Equal(x => x.NewPassword).WithErrorCode("Auth.Password.Mismatch");
        }
    }

    public class ForgotPasswordDtoValidator : AbstractValidator<ForgotPasswordDto>
    {
        public ForgotPasswordDtoValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithErrorCode("Auth.Email.Required")
                .EmailAddress().WithErrorCode("Auth.Email.Invalid");
        }
    }
}