
using Expense.API.Extensions.StartupExtensions;
using Expense.API.Startup;
using Expense.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;

namespace Expense.API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Configure services using extension method
            builder.ConfigureApplicationServices();         

            var app = builder.Build();          

            // Initialize roles
            using (var scope = app.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                try
                {
                    RoleInitializer.InitializeAsync(services).Wait();
                }
                catch (Exception ex)
                {
                    var logger = services.GetRequiredService<ILogger<Program>>();
                    logger.LogError(ex, "An error occurred while seeding the database.");
                }
            }

            // Configure middleware using extension method
            app.ConfigureApplicationMiddleware();           

            try
            {
                app.Run();
            }
            catch (Exception ex)
            {
                var logger = app.Services.GetRequiredService<ILogger<Program>>();
                logger.LogCritical(ex, "Host terminated unexpectedly");
                throw;
            }
        }
    }
}
