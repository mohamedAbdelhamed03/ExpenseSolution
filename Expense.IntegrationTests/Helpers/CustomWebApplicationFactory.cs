using Expense.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Linq;

using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Expense.IntegrationTests.Helpers
{
    public class CustomWebApplicationFactory<TProgram> : WebApplicationFactory<TProgram> where TProgram : class
    {
        static CustomWebApplicationFactory()
        {
            System.Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Test");
            System.Environment.SetEnvironmentVariable("JwtSettings__Key", "TEST_JWT_KEY_12345678901234567890123456789012");
            System.Environment.SetEnvironmentVariable("JwtSettings__Issuer", "ExpenseAPI");
            System.Environment.SetEnvironmentVariable("JwtSettings__Audience", "ExpenseUsers");
            System.Environment.SetEnvironmentVariable("Cloudinary__CloudName", "test");
            System.Environment.SetEnvironmentVariable("Cloudinary__ApiKey", "test");
            System.Environment.SetEnvironmentVariable("Cloudinary__ApiSecret", "test");
            System.Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", "Server=(localdb)\\mssqllocaldb;Database=ExpenseTestDb;Trusted_Connection=true;MultipleActiveResultSets=true");
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Test");

            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["JwtSettings:Key"] = "TEST_JWT_KEY_12345678901234567890123456789012",
                    ["JwtSettings:Issuer"] = "ExpenseAPI",
                    ["JwtSettings:Audience"] = "ExpenseUsers",
                    ["Cloudinary:CloudName"] = "test",
                    ["Cloudinary:ApiKey"] = "test",
                    ["Cloudinary:ApiSecret"] = "test"
                });
            });

            builder.ConfigureTestServices(services =>
            {
                var descriptors = services.Where(
                    d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>) ||
                         d.ServiceType == typeof(DbContextOptions) ||
                         d.ServiceType == typeof(ApplicationDbContext) ||
                         d.ServiceType == typeof(IDbContextOptionsConfiguration<ApplicationDbContext>)).ToList();

                foreach (var d in descriptors)
                {
                    services.Remove(d);
                }

                services.AddDbContext<ApplicationDbContext>(options =>
                {
                    options.UseInMemoryDatabase("InMemoryDbForTesting");
                });
            });
        }
    }
}
