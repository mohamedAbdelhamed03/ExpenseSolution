using System.Reflection;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Expense.Core.Features.DependencyInjection
{
    public static class ValidationRegistration
    {
        public static IServiceCollection AddCoreValidation(this IServiceCollection services)
        {
            services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
            return services;
        }
    }
}