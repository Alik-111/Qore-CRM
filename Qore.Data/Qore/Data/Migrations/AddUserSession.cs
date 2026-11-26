using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.EntityFrameworkCore.Migrations.Operations.Builders;

namespace Qore.Data.Migrations;

[DbContext(typeof(QoreDbContext))]
[Migration("20260713110956_AddUserSession")]
public class AddUserSession : Migration
{
	protected override void Up(MigrationBuilder migrationBuilder)
	{
		migrationBuilder.CreateTable("UserSessions", (ColumnsBuilder table) =>
		{
			OperationBuilder<AddColumnOperation> id = table.Column<Guid>("uniqueidentifier");
			OperationBuilder<AddColumnOperation> userId = table.Column<Guid>("uniqueidentifier");
			int? maxLength = 100;
			return new
			{
				Id = id,
				UserId = userId,
				Jti = table.Column<string>("nvarchar(100)", null, maxLength),
				CreatedAt = table.Column<DateTime>("datetime2", null, null, rowVersion: false, null, nullable: false, null, "GETUTCDATE()"),
				ExpiresAt = table.Column<DateTime>("datetime2"),
				IsRevoked = table.Column<bool>("bit", null, null, rowVersion: false, null, nullable: false, false)
			};
		}, null, table =>
		{
			table.PrimaryKey("PK_UserSessions", x => x.Id);
			table.ForeignKey("FK_UserSessions_Users_UserId", x => x.UserId, "Users", "Id", null, ReferentialAction.NoAction, ReferentialAction.Cascade);
		});
		migrationBuilder.CreateIndex("IX_UserSessions_Jti", "UserSessions", "Jti", null, unique: true);
		migrationBuilder.CreateIndex("IX_UserSessions_UserId", "UserSessions", "UserId");
	}

	protected override void Down(MigrationBuilder migrationBuilder)
	{
		migrationBuilder.DropTable("UserSessions");
	}

	protected override void BuildTargetModel(ModelBuilder modelBuilder)
	{
		modelBuilder.HasAnnotation("ProductVersion", "8.0.8").HasAnnotation("Relational:MaxIdentifierLength", 128);
		modelBuilder.UseIdentityColumns(1L);
		modelBuilder.Entity("Qore.Data.Entities.Client", (EntityTypeBuilder b) =>
		{
			b.Property<Guid>("Id").ValueGeneratedOnAdd().HasColumnType("uniqueidentifier");
			b.Property<string>("Address").IsRequired().HasMaxLength(500)
				.HasColumnType("nvarchar(500)");
			b.Property<Guid?>("AssignedToUserId").HasColumnType("uniqueidentifier");
			b.Property<string>("CompanyName").IsRequired().HasMaxLength(200)
				.HasColumnType("nvarchar(200)");
			b.Property<DateTime>("CreatedAt").ValueGeneratedOnAdd().HasColumnType("datetime2")
				.HasDefaultValueSql("GETUTCDATE()");
			b.Property<string>("Email").IsRequired().HasMaxLength(200)
				.HasColumnType("nvarchar(200)");
			b.Property<string>("Name").IsRequired().HasMaxLength(200)
				.HasColumnType("nvarchar(200)");
			b.Property<string>("Notes").IsRequired().HasColumnType("nvarchar(max)");
			b.Property<string>("Phone").IsRequired().HasMaxLength(50)
				.HasColumnType("nvarchar(50)");
			b.Property<DateTime>("UpdatedAt").ValueGeneratedOnAdd().HasColumnType("datetime2")
				.HasDefaultValueSql("GETUTCDATE()");
			b.HasKey("Id");
			b.HasIndex("AssignedToUserId");
			b.ToTable("Clients");
		});
		modelBuilder.Entity("Qore.Data.Entities.Customer", (EntityTypeBuilder b) =>
		{
			b.Property<int>("Id").ValueGeneratedOnAdd().HasColumnType("int");
			b.Property<int>("Id").UseIdentityColumn(1L);
			b.Property<string>("Company").IsRequired().HasMaxLength(200)
				.HasColumnType("nvarchar(200)");
			b.Property<DateTime>("CreatedAt").ValueGeneratedOnAdd().HasColumnType("datetime2")
				.HasDefaultValueSql("GETUTCDATE()");
			b.Property<string>("Email").IsRequired().HasMaxLength(200)
				.HasColumnType("nvarchar(200)");
			b.Property<string>("Name").IsRequired().HasMaxLength(200)
				.HasColumnType("nvarchar(200)");
			b.Property<string>("Phone").IsRequired().HasMaxLength(50)
				.HasColumnType("nvarchar(50)");
			b.Property<string>("Status").IsRequired().HasColumnType("nvarchar(max)");
			b.HasKey("Id");
			b.ToTable("Customers");
		});
		modelBuilder.Entity("Qore.Data.Entities.Document", (EntityTypeBuilder b) =>
		{
			b.Property<Guid>("Id").ValueGeneratedOnAdd().HasColumnType("uniqueidentifier");
			b.Property<string>("ContentType").IsRequired().HasMaxLength(200)
				.HasColumnType("nvarchar(200)");
			b.Property<DateTime>("CreatedAt").ValueGeneratedOnAdd().HasColumnType("datetime2")
				.HasDefaultValueSql("GETUTCDATE()");
			b.Property<string>("Description").IsRequired().HasColumnType("nvarchar(max)");
			b.Property<byte[]>("FileData").IsRequired().HasColumnType("varbinary(max)");
			b.Property<string>("FileName").IsRequired().HasMaxLength(255)
				.HasColumnType("nvarchar(255)");
			b.Property<string>("Name").IsRequired().HasMaxLength(200)
				.HasColumnType("nvarchar(200)");
			b.Property<Guid>("UploadedByUserId").HasColumnType("uniqueidentifier");
			b.Property<string>("VisibleToRole").HasColumnType("nvarchar(max)");
			b.HasKey("Id");
			b.HasIndex("UploadedByUserId");
			b.ToTable("Documents");
		});
		modelBuilder.Entity("Qore.Data.Entities.Opportunity", (EntityTypeBuilder b) =>
		{
			b.Property<Guid>("Id").ValueGeneratedOnAdd().HasColumnType("uniqueidentifier");
			b.Property<Guid>("AssignedToUserId").HasColumnType("uniqueidentifier");
			b.Property<Guid?>("ClientId").HasColumnType("uniqueidentifier");
			b.Property<DateTime>("CreatedAt").ValueGeneratedOnAdd().HasColumnType("datetime2")
				.HasDefaultValueSql("GETUTCDATE()");
			b.Property<string>("Description").IsRequired().HasColumnType("nvarchar(max)");
			b.Property<string>("Status").IsRequired().HasColumnType("nvarchar(max)");
			b.Property<string>("Title").IsRequired().HasMaxLength(200)
				.HasColumnType("nvarchar(200)");
			b.Property<DateTime>("UpdatedAt").ValueGeneratedOnAdd().HasColumnType("datetime2")
				.HasDefaultValueSql("GETUTCDATE()");
			b.Property<decimal>("Value").HasColumnType("decimal(18,2)");
			b.HasKey("Id");
			b.HasIndex("AssignedToUserId");
			b.HasIndex("ClientId");
			b.ToTable("Opportunities");
		});
		modelBuilder.Entity("Qore.Data.Entities.SystemSetting", (EntityTypeBuilder b) =>
		{
			b.Property<Guid>("Id").ValueGeneratedOnAdd().HasColumnType("uniqueidentifier");
			b.Property<string>("Key").IsRequired().HasMaxLength(200)
				.HasColumnType("nvarchar(200)");
			b.Property<string>("Value").IsRequired().HasColumnType("nvarchar(max)");
			b.HasKey("Id");
			b.HasIndex("Key").IsUnique();
			b.ToTable("SystemSettings");
		});
		modelBuilder.Entity("Qore.Data.Entities.TaskItem", (EntityTypeBuilder b) =>
		{
			b.Property<Guid>("Id").ValueGeneratedOnAdd().HasColumnType("uniqueidentifier");
			b.Property<Guid>("AssignedToUserId").HasColumnType("uniqueidentifier");
			b.Property<DateTime>("CreatedAt").ValueGeneratedOnAdd().HasColumnType("datetime2")
				.HasDefaultValueSql("GETUTCDATE()");
			b.Property<Guid>("CreatedByUserId").HasColumnType("uniqueidentifier");
			b.Property<string>("Description").IsRequired().HasColumnType("nvarchar(max)");
			b.Property<DateTime?>("DueDate").HasColumnType("datetime2");
			b.Property<string>("Priority").IsRequired().HasColumnType("nvarchar(max)");
			b.Property<string>("Status").IsRequired().HasColumnType("nvarchar(max)");
			b.Property<string>("Title").IsRequired().HasMaxLength(200)
				.HasColumnType("nvarchar(200)");
			b.Property<DateTime>("UpdatedAt").ValueGeneratedOnAdd().HasColumnType("datetime2")
				.HasDefaultValueSql("GETUTCDATE()");
			b.HasKey("Id");
			b.HasIndex("AssignedToUserId");
			b.HasIndex("CreatedByUserId");
			b.ToTable("TaskItems");
		});
		modelBuilder.Entity("Qore.Data.Entities.Theme", (EntityTypeBuilder b) =>
		{
			b.Property<Guid>("Id").ValueGeneratedOnAdd().HasColumnType("uniqueidentifier");
			b.Property<string>("AccentColor").IsRequired().HasMaxLength(50)
				.HasColumnType("nvarchar(50)");
			b.Property<string>("BackgroundColor").IsRequired().HasMaxLength(50)
				.HasColumnType("nvarchar(50)");
			b.Property<DateTime>("CreatedAt").ValueGeneratedOnAdd().HasColumnType("datetime2")
				.HasDefaultValueSql("GETUTCDATE()");
			b.Property<Guid>("CreatedByUserId").HasColumnType("uniqueidentifier");
			b.Property<bool>("IsActive").ValueGeneratedOnAdd().HasColumnType("bit")
				.HasDefaultValue(false);
			b.Property<bool>("IsDefault").ValueGeneratedOnAdd().HasColumnType("bit")
				.HasDefaultValue(false);
			b.Property<string>("Name").IsRequired().HasMaxLength(200)
				.HasColumnType("nvarchar(200)");
			b.Property<string>("PrimaryColor").IsRequired().HasMaxLength(50)
				.HasColumnType("nvarchar(50)");
			b.Property<string>("SecondaryColor").IsRequired().HasMaxLength(50)
				.HasColumnType("nvarchar(50)");
			b.Property<string>("TextColor").IsRequired().HasMaxLength(50)
				.HasColumnType("nvarchar(50)");
			b.HasKey("Id");
			b.HasIndex("CreatedByUserId");
			b.ToTable("Themes");
		});
		modelBuilder.Entity("Qore.Data.Entities.User", (EntityTypeBuilder b) =>
		{
			b.Property<Guid>("Id").ValueGeneratedOnAdd().HasColumnType("uniqueidentifier");
			b.Property<DateTime>("CreatedAt").ValueGeneratedOnAdd().HasColumnType("datetime2")
				.HasDefaultValueSql("GETUTCDATE()");
			b.Property<string>("Email").IsRequired().HasMaxLength(200)
				.HasColumnType("nvarchar(200)");
			b.Property<string>("FullName").IsRequired().HasMaxLength(200)
				.HasColumnType("nvarchar(200)");
			b.Property<bool>("IsActive").ValueGeneratedOnAdd().HasColumnType("bit")
				.HasDefaultValue(true);
			b.Property<DateTime?>("LastLoginAt").HasColumnType("datetime2");
			b.Property<string>("PasswordHash").IsRequired().HasColumnType("nvarchar(max)");
			b.Property<string>("PasswordSalt").IsRequired().HasColumnType("nvarchar(max)");
			b.Property<string>("Role").IsRequired().HasColumnType("nvarchar(max)");
			b.Property<string>("Username").IsRequired().HasMaxLength(100)
				.HasColumnType("nvarchar(100)");
			b.HasKey("Id");
			b.HasIndex("Username").IsUnique();
			b.ToTable("Users");
		});
		modelBuilder.Entity("Qore.Data.Entities.UserSession", (EntityTypeBuilder b) =>
		{
			b.Property<Guid>("Id").ValueGeneratedOnAdd().HasColumnType("uniqueidentifier");
			b.Property<DateTime>("CreatedAt").ValueGeneratedOnAdd().HasColumnType("datetime2")
				.HasDefaultValueSql("GETUTCDATE()");
			b.Property<DateTime>("ExpiresAt").HasColumnType("datetime2");
			b.Property<bool>("IsRevoked").ValueGeneratedOnAdd().HasColumnType("bit")
				.HasDefaultValue(false);
			b.Property<string>("Jti").IsRequired().HasMaxLength(100)
				.HasColumnType("nvarchar(100)");
			b.Property<Guid>("UserId").HasColumnType("uniqueidentifier");
			b.HasKey("Id");
			b.HasIndex("Jti").IsUnique();
			b.HasIndex("UserId");
			b.ToTable("UserSessions");
		});
		modelBuilder.Entity("Qore.Data.Entities.Client", (EntityTypeBuilder b) =>
		{
			b.HasOne("Qore.Data.Entities.User", "AssignedToUser").WithMany().HasForeignKey("AssignedToUserId")
				.OnDelete(DeleteBehavior.Restrict);
			b.Navigation("AssignedToUser");
		});
		modelBuilder.Entity("Qore.Data.Entities.Document", (EntityTypeBuilder b) =>
		{
			b.HasOne("Qore.Data.Entities.User", "UploadedByUser").WithMany().HasForeignKey("UploadedByUserId")
				.OnDelete(DeleteBehavior.Restrict)
				.IsRequired();
			b.Navigation("UploadedByUser");
		});
		modelBuilder.Entity("Qore.Data.Entities.Opportunity", (EntityTypeBuilder b) =>
		{
			b.HasOne("Qore.Data.Entities.User", "AssignedToUser").WithMany().HasForeignKey("AssignedToUserId")
				.OnDelete(DeleteBehavior.Restrict)
				.IsRequired();
			b.HasOne("Qore.Data.Entities.Client", "Client").WithMany("Opportunities").HasForeignKey("ClientId")
				.OnDelete(DeleteBehavior.SetNull);
			b.Navigation("AssignedToUser");
			b.Navigation("Client");
		});
		modelBuilder.Entity("Qore.Data.Entities.TaskItem", (EntityTypeBuilder b) =>
		{
			b.HasOne("Qore.Data.Entities.User", "AssignedToUser").WithMany().HasForeignKey("AssignedToUserId")
				.OnDelete(DeleteBehavior.Restrict)
				.IsRequired();
			b.HasOne("Qore.Data.Entities.User", "CreatedByUser").WithMany().HasForeignKey("CreatedByUserId")
				.OnDelete(DeleteBehavior.Restrict)
				.IsRequired();
			b.Navigation("AssignedToUser");
			b.Navigation("CreatedByUser");
		});
		modelBuilder.Entity("Qore.Data.Entities.Theme", (EntityTypeBuilder b) =>
		{
			b.HasOne("Qore.Data.Entities.User", "CreatedByUser").WithMany().HasForeignKey("CreatedByUserId")
				.OnDelete(DeleteBehavior.Restrict)
				.IsRequired();
			b.Navigation("CreatedByUser");
		});
		modelBuilder.Entity("Qore.Data.Entities.UserSession", (EntityTypeBuilder b) =>
		{
			b.HasOne("Qore.Data.Entities.User", "User").WithMany("Sessions").HasForeignKey("UserId")
				.OnDelete(DeleteBehavior.Cascade)
				.IsRequired();
			b.Navigation("User");
		});
		modelBuilder.Entity("Qore.Data.Entities.Client", (EntityTypeBuilder b) =>
		{
			b.Navigation("Opportunities");
		});
		modelBuilder.Entity("Qore.Data.Entities.User", (EntityTypeBuilder b) =>
		{
			b.Navigation("Sessions");
		});
	}
}
