using Expense.Core.DTOs.Auth;
using Expense.Core.Features.Auth.Validators;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Expense.Core.Features.DependencyInjection
{
    public static class ValidationRegistration
    {
        public static IServiceCollection AddAuthValidation(this IServiceCollection services)
        {
            services.AddScoped<IValidator<RegisterDto>, RegisterDtoValidator>();
            services.AddScoped<IValidator<LoginDto>, LoginDtoValidator>();
            services.AddScoped<IValidator<RefreshTokenDto>, RefreshTokenDtoValidator>();
            services.AddScoped<IValidator<ChangePasswordDto>, ChangePasswordDtoValidator>();
            services.AddScoped<IValidator<ResetPasswordDto>, ResetPasswordDtoValidator>();
            services.AddScoped<IValidator<ForgotPasswordDto>, ForgotPasswordDtoValidator>();
            
            return services;
        }
    }
}