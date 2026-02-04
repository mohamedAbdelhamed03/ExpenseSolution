using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Expense.Core.Domain.IdentityEntities;
using Expense.Core.Domain.Entities;
using Expense.Core.Abstractions.Persistence;

namespace Expense.Infrastructure.Data
{
	public class ApplicationDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>, IApplicationDbContext
	{
		public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
		{
		}

		public DbSet<RefreshToken> RefreshTokens { get; set; }

		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			base.OnModelCreating(modelBuilder);

			modelBuilder.Entity<RefreshToken>(entity =>
			{
				entity.HasKey(e => e.Id);
				entity.Property(e => e.Token).IsRequired();
				entity.Property(e => e.UserId).IsRequired();
				entity.Property(e => e.ExpiresAt).IsRequired();
				entity.Property(e => e.IsRevoked).IsRequired();
				entity.Property(e => e.CreatedAt).IsRequired();
				
				entity.HasIndex(e => e.Token).IsUnique();
				entity.HasIndex(e => e.UserId);
			});
		}
	}
}
