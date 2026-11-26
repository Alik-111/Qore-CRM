using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Qore.Data.Entities;

namespace Qore.Data;

public class QoreDbContext : DbContext
{
	public DbSet<User> Users => Set<User>();

	public DbSet<Client> Clients => Set<Client>();

	public DbSet<Opportunity> Opportunities => Set<Opportunity>();

	public DbSet<TaskItem> TaskItems => Set<TaskItem>();

	public DbSet<Document> Documents => Set<Document>();

	public DbSet<Theme> Themes => Set<Theme>();

	public DbSet<SystemSetting> SystemSettings => Set<SystemSetting>();

	public DbSet<UserSession> UserSessions => Set<UserSession>();

	public DbSet<Customer> Customers => Set<Customer>();

	public QoreDbContext(DbContextOptions<QoreDbContext> options)
		: base(options)
	{
	}

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		modelBuilder.Entity((EntityTypeBuilder<User> entity) =>
		{
			entity.HasKey((User e) => e.Id);
			entity.Property((User e) => e.Username).IsRequired().HasMaxLength(100);
			entity.HasIndex((User e) => e.Username).IsUnique();
			entity.Property((User e) => e.PasswordHash).IsRequired();
			entity.Property((User e) => e.PasswordSalt).IsRequired();
			entity.Property((User e) => e.FullName).IsRequired().HasMaxLength(200);
			entity.Property((User e) => e.Email).IsRequired().HasMaxLength(200);
			entity.Property((User e) => e.Role).HasConversion<string>();
			entity.Property((User e) => e.IsActive).HasDefaultValue(true);
			entity.Property((User e) => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
		});
		modelBuilder.Entity((EntityTypeBuilder<UserSession> entity) =>
		{
			entity.HasKey((UserSession e) => e.Id);
			entity.Property((UserSession e) => e.Jti).IsRequired().HasMaxLength(100);
			entity.Property((UserSession e) => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
			entity.Property((UserSession e) => e.ExpiresAt).IsRequired();
			entity.Property((UserSession e) => e.IsRevoked).HasDefaultValue(false);
			entity.HasIndex((UserSession e) => e.Jti).IsUnique();
			entity.HasOne((UserSession e) => e.User).WithMany((User u) => u.Sessions).HasForeignKey((UserSession e) => e.UserId)
				.OnDelete(DeleteBehavior.Cascade);
		});
		modelBuilder.Entity((EntityTypeBuilder<Client> entity) =>
		{
			entity.HasKey((Client e) => e.Id);
			entity.Property((Client e) => e.Name).IsRequired().HasMaxLength(200);
			entity.Property((Client e) => e.CompanyName).HasMaxLength(200);
			entity.Property((Client e) => e.Email).HasMaxLength(200);
			entity.Property((Client e) => e.Phone).HasMaxLength(50);
			entity.Property((Client e) => e.Address).HasMaxLength(500);
			entity.Property((Client e) => e.Notes).HasColumnType("nvarchar(max)");
			entity.Property((Client e) => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
			entity.Property((Client e) => e.UpdatedAt).HasDefaultValueSql("GETUTCDATE()");
			entity.HasOne((Client e) => e.AssignedToUser).WithMany().HasForeignKey((Client e) => e.AssignedToUserId)
				.OnDelete(DeleteBehavior.Restrict);
		});
		modelBuilder.Entity((EntityTypeBuilder<Opportunity> entity) =>
		{
			entity.HasKey((Opportunity e) => e.Id);
			entity.Property((Opportunity e) => e.Title).IsRequired().HasMaxLength(200);
			entity.Property((Opportunity e) => e.Value).HasColumnType("decimal(18,2)");
			entity.Property((Opportunity e) => e.Status).HasConversion<string>();
			entity.Property((Opportunity e) => e.Description).HasColumnType("nvarchar(max)");
			entity.Property((Opportunity e) => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
			entity.Property((Opportunity e) => e.UpdatedAt).HasDefaultValueSql("GETUTCDATE()");
			entity.HasOne((Opportunity e) => e.Client).WithMany((Client c) => c.Opportunities).HasForeignKey((Opportunity e) => e.ClientId)
				.OnDelete(DeleteBehavior.SetNull);
			entity.HasOne((Opportunity e) => e.AssignedToUser).WithMany().HasForeignKey((Opportunity e) => e.AssignedToUserId)
				.OnDelete(DeleteBehavior.Restrict);
		});
		modelBuilder.Entity((EntityTypeBuilder<TaskItem> entity) =>
		{
			entity.HasKey((TaskItem e) => e.Id);
			entity.Property((TaskItem e) => e.Title).IsRequired().HasMaxLength(200);
			entity.Property((TaskItem e) => e.Description).HasColumnType("nvarchar(max)");
			entity.Property((TaskItem e) => e.Status).HasConversion<string>();
			entity.Property((TaskItem e) => e.Priority).HasConversion<string>();
			entity.Property((TaskItem e) => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
			entity.Property((TaskItem e) => e.UpdatedAt).HasDefaultValueSql("GETUTCDATE()");
			entity.HasOne((TaskItem e) => e.AssignedToUser).WithMany().HasForeignKey((TaskItem e) => e.AssignedToUserId)
				.OnDelete(DeleteBehavior.Restrict);
			entity.HasOne((TaskItem e) => e.CreatedByUser).WithMany().HasForeignKey((TaskItem e) => e.CreatedByUserId)
				.OnDelete(DeleteBehavior.Restrict);
		});
		modelBuilder.Entity((EntityTypeBuilder<Document> entity) =>
		{
			entity.HasKey((Document e) => e.Id);
			entity.Property((Document e) => e.Name).IsRequired().HasMaxLength(200);
			entity.Property((Document e) => e.Description).HasColumnType("nvarchar(max)");
			entity.Property((Document e) => e.FileData).HasColumnType("varbinary(max)");
			entity.Property((Document e) => e.FileName).HasMaxLength(255);
			entity.Property((Document e) => e.ContentType).HasMaxLength(200);
			entity.Property((Document e) => e.VisibleToRole).HasConversion<string>();
			entity.Property((Document e) => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
			entity.HasOne((Document e) => e.UploadedByUser).WithMany().HasForeignKey((Document e) => e.UploadedByUserId)
				.OnDelete(DeleteBehavior.Restrict);
		});
		modelBuilder.Entity((EntityTypeBuilder<Theme> entity) =>
		{
			entity.HasKey((Theme e) => e.Id);
			entity.Property((Theme e) => e.Name).IsRequired().HasMaxLength(200);
			entity.Property((Theme e) => e.PrimaryColor).HasMaxLength(50);
			entity.Property((Theme e) => e.SecondaryColor).HasMaxLength(50);
			entity.Property((Theme e) => e.AccentColor).HasMaxLength(50);
			entity.Property((Theme e) => e.BackgroundColor).HasMaxLength(50);
			entity.Property((Theme e) => e.TextColor).HasMaxLength(50);
			entity.Property((Theme e) => e.IsActive).HasDefaultValue(false);
			entity.Property((Theme e) => e.IsDefault).HasDefaultValue(false);
			entity.Property((Theme e) => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
			entity.HasOne((Theme e) => e.CreatedByUser).WithMany().HasForeignKey((Theme e) => e.CreatedByUserId)
				.OnDelete(DeleteBehavior.Restrict);
		});
		modelBuilder.Entity((EntityTypeBuilder<SystemSetting> entity) =>
		{
			entity.HasKey((SystemSetting e) => e.Id);
			entity.Property((SystemSetting e) => e.Key).IsRequired().HasMaxLength(200);
			entity.HasIndex((SystemSetting e) => e.Key).IsUnique();
			entity.Property((SystemSetting e) => e.Value).HasColumnType("nvarchar(max)");
		});
		modelBuilder.Entity((EntityTypeBuilder<Customer> entity) =>
		{
			entity.HasKey((Customer e) => e.Id);
			entity.Property((Customer e) => e.Name).IsRequired().HasMaxLength(200);
			entity.Property((Customer e) => e.Email).IsRequired().HasMaxLength(200);
			entity.Property((Customer e) => e.Phone).HasMaxLength(50);
			entity.Property((Customer e) => e.Company).HasMaxLength(200);
			entity.Property((Customer e) => e.Status).HasConversion<string>();
			entity.Property((Customer e) => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
		});
	}
}
