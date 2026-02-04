using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Expense.Core.Domain.IdentityEntities;
using Expense.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ExpenseEntity = Expense.Core.Domain.Entities.Expense;

namespace Expense.Infrastructure.Data
{
	public class ApplicationDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>
	{
		public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
		{
		}

		public DbSet<RefreshToken> RefreshTokens { get; set; }
		public DbSet<Group> Groups { get; set; }
		public DbSet<GroupMember> GroupMembers { get; set; }
		public DbSet<ExpenseEntity> Expenses { get; set; }
		public DbSet<ExpenseSplit> ExpenseSplits { get; set; }

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

            modelBuilder.Entity<ApplicationUser>(entity =>
            {
                entity.HasIndex(u => u.GoogleId).IsUnique().HasFilter("[GoogleId] IS NOT NULL");
                entity.HasIndex(u => u.FacebookId).IsUnique().HasFilter("[FacebookId] IS NOT NULL");
                entity.Property(u => u.Provider).HasConversion<string>();
            });

			modelBuilder.Entity<Group>(entity =>
			{
				entity.HasKey(e => e.Id);
				entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
				entity.Property(e => e.CreatedByUserId).IsRequired();
				entity.Property(e => e.InviteCode).IsRequired().HasMaxLength(16);
				entity.Property(e => e.CreatedAt).IsRequired();
				entity.HasIndex(e => e.InviteCode).IsUnique();
			});

			modelBuilder.Entity<GroupMember>(entity =>
			{
				entity.HasKey(e => e.Id);
				entity.Property(e => e.UserId).IsRequired();
				entity.Property(e => e.JoinedAt).IsRequired();
				entity.HasIndex(e => new { e.GroupId, e.UserId }).IsUnique();
				entity.HasOne(e => e.Group)
					.WithMany(g => g.Members)
					.HasForeignKey(e => e.GroupId)
					.OnDelete(DeleteBehavior.Cascade);
			});

			modelBuilder.Entity<ExpenseEntity>(entity =>
			{
				entity.HasKey(e => e.Id);
				entity.Property(e => e.GroupId).IsRequired();
				entity.Property(e => e.PaidByUserId).IsRequired();
				entity.Property(e => e.Amount).IsRequired();
				entity.Property(e => e.ExpenseDate).IsRequired();
				entity.Property(e => e.CreatedAt).IsRequired();
				entity.HasOne(e => e.Group)
					.WithMany(g => g.Expenses)
					.HasForeignKey(e => e.GroupId)
					.OnDelete(DeleteBehavior.Cascade);
				entity.ToTable(t => t.HasCheckConstraint("CK_Expense_Amount_Positive", "[Amount] > 0"));
			});

			modelBuilder.Entity<ExpenseSplit>(entity =>
			{
				entity.HasKey(e => e.Id);
				entity.Property(e => e.UserId).IsRequired();
				entity.Property(e => e.Amount).IsRequired();
				entity.HasOne(e => e.Expense)
					.WithMany(x => x.Splits)
					.HasForeignKey(e => e.ExpenseId)
					.OnDelete(DeleteBehavior.Cascade);
				entity.ToTable(t => t.HasCheckConstraint("CK_ExpenseSplit_Amount_Positive", "[Amount] > 0"));
			});
		}
	}
}
