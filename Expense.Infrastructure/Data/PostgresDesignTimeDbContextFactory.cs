// using Microsoft.EntityFrameworkCore;
// using Microsoft.EntityFrameworkCore.Design;

// namespace Expense.Infrastructure.Data
// {
//     public class PostgresDesignTimeDbContextFactory : IDesignTimeDbContextFactory<PostgresApplicationDbContext>
//     {
//         public PostgresApplicationDbContext CreateDbContext(string[] args)
//         {
//             var connectionString =
//                 Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
//                 ?? Environment.GetEnvironmentVariable("DefaultConnection");

//             if (string.IsNullOrWhiteSpace(connectionString))
//             {
//                 throw new InvalidOperationException("Missing connection string. Set ConnectionStrings__DefaultConnection environment variable before running EF commands.");
//             }

//             var optionsBuilder = new DbContextOptionsBuilder<PostgresApplicationDbContext>();
//             optionsBuilder.UseNpgsql(connectionString);

//             return new PostgresApplicationDbContext(optionsBuilder.Options);
//         }
//     }
// }
