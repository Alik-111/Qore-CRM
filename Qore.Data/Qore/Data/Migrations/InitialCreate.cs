using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.EntityFrameworkCore.Migrations.Operations.Builders;

namespace Qore.Data.Migrations;

[DbContext(typeof(QoreDbContext))]
[Migration("20260709112007_InitialCreate")]
public class InitialCreate : Migration
{
	protected override void Up(MigrationBuilder migrationBuilder)
	{
		migrationBuilder.CreateTable("Customers", (ColumnsBuilder table) =>
		{
			OperationBuilder<AddColumnOperation> id = table.Column<int>("int").Annotation("SqlServer:Identity", "1, 1");
			int? maxLength = 200;
			OperationBuilder<AddColumnOperation> name = table.Column<string>("nvarchar(200)", null, maxLength);
			maxLength = 200;
			OperationBuilder<AddColumnOperation> email = table.Column<string>("nvarchar(200)", null, maxLength);
			maxLength = 50;
			OperationBuilder<AddColumnOperation> phone = table.Column<string>("nvarchar(50)", null, maxLength);
			maxLength = 200;
			return new
			{
				Id = id,
				Name = name,
				Email = email,
				Phone = phone,
				Company = table.Column<string>("nvarchar(200)", null, maxLength),
				Status = table.Column<string>("nvarchar(max)"),
				CreatedAt = table.Column<DateTime>("datetime2", null, null, rowVersion: false, null, nullable: false, null, "GETUTCDATE()")
			};
		}, null, table =>
		{
			table.PrimaryKey("PK_Customers", x => x.Id);
		});
		migrationBuilder.CreateTable("SystemSettings", (ColumnsBuilder table) =>
		{
			OperationBuilder<AddColumnOperation> id = table.Column<Guid>("uniqueidentifier");
			int? maxLength = 200;
			return new
			{
				Id = id,
				Key = table.Column<string>("nvarchar(200)", null, maxLength),
				Value = table.Column<string>("nvarchar(max)")
			};
		}, null, table =>
		{
			table.PrimaryKey("PK_SystemSettings", x => x.Id);
		});
		migrationBuilder.CreateTable("Users", (ColumnsBuilder table) =>
		{
			OperationBuilder<AddColumnOperation> id = table.Column<Guid>("uniqueidentifier");
			int? maxLength = 100;
			OperationBuilder<AddColumnOperation> username = table.Column<string>("nvarchar(100)", null, maxLength);
			OperationBuilder<AddColumnOperation> passwordHash = table.Column<string>("nvarchar(max)");
			OperationBuilder<AddColumnOperation> passwordSalt = table.Column<string>("nvarchar(max)");
			maxLength = 200;
			OperationBuilder<AddColumnOperation> fullName = table.Column<string>("nvarchar(200)", null, maxLength);
			maxLength = 200;
			return new
			{
				Id = id,
				Username = username,
				PasswordHash = passwordHash,
				PasswordSalt = passwordSalt,
				FullName = fullName,
				Email = table.Column<string>("nvarchar(200)", null, maxLength),
				Role = table.Column<string>("nvarchar(max)"),
				IsActive = table.Column<bool>("bit", null, null, rowVersion: false, null, nullable: false, true),
				CreatedAt = table.Column<DateTime>("datetime2", null, null, rowVersion: false, null, nullable: false, null, "GETUTCDATE()"),
				LastLoginAt = table.Column<DateTime>("datetime2", null, null, rowVersion: false, null, nullable: true)
			};
		}, null, table =>
		{
			table.PrimaryKey("PK_Users", x => x.Id);
		});
		migrationBuilder.CreateTable("Clients", (ColumnsBuilder table) =>
		{
			OperationBuilder<AddColumnOperation> id = table.Column<Guid>("uniqueidentifier");
			int? maxLength = 200;
			OperationBuilder<AddColumnOperation> name = table.Column<string>("nvarchar(200)", null, maxLength);
			maxLength = 200;
			OperationBuilder<AddColumnOperation> companyName = table.Column<string>("nvarchar(200)", null, maxLength);
			maxLength = 200;
			OperationBuilder<AddColumnOperation> email = table.Column<string>("nvarchar(200)", null, maxLength);
			maxLength = 50;
			OperationBuilder<AddColumnOperation> phone = table.Column<string>("nvarchar(50)", null, maxLength);
			maxLength = 500;
			return new
			{
				Id = id,
				Name = name,
				CompanyName = companyName,
				Email = email,
				Phone = phone,
				Address = table.Column<string>("nvarchar(500)", null, maxLength),
				Notes = table.Column<string>("nvarchar(max)"),
				AssignedToUserId = table.Column<Guid>("uniqueidentifier", null, null, rowVersion: false, null, nullable: true),
				CreatedAt = table.Column<DateTime>("datetime2", null, null, rowVersion: false, null, nullable: false, null, "GETUTCDATE()"),
				UpdatedAt = table.Column<DateTime>("datetime2", null, null, rowVersion: false, null, nullable: false, null, "GETUTCDATE()")
			};
		}, null, table =>
		{
			table.PrimaryKey("PK_Clients", x => x.Id);
			table.ForeignKey("FK_Clients_Users_AssignedToUserId", x => x.AssignedToUserId, "Users", "Id", null, ReferentialAction.NoAction, ReferentialAction.Restrict);
		});
		migrationBuilder.CreateTable("Documents", (ColumnsBuilder table) =>
		{
			OperationBuilder<AddColumnOperation> id = table.Column<Guid>("uniqueidentifier");
			int? maxLength = 200;
			OperationBuilder<AddColumnOperation> name = table.Column<string>("nvarchar(200)", null, maxLength);
			OperationBuilder<AddColumnOperation> description = table.Column<string>("nvarchar(max)");
			OperationBuilder<AddColumnOperation> fileData = table.Column<byte[]>("varbinary(max)");
			maxLength = 255;
			OperationBuilder<AddColumnOperation> fileName = table.Column<string>("nvarchar(255)", null, maxLength);
			maxLength = 200;
			return new
			{
				Id = id,
				Name = name,
				Description = description,
				FileData = fileData,
				FileName = fileName,
				ContentType = table.Column<string>("nvarchar(200)", null, maxLength),
				UploadedByUserId = table.Column<Guid>("uniqueidentifier"),
				VisibleToRole = table.Column<string>("nvarchar(max)", null, null, rowVersion: false, null, nullable: true),
				CreatedAt = table.Column<DateTime>("datetime2", null, null, rowVersion: false, null, nullable: false, null, "GETUTCDATE()")
			};
		}, null, table =>
		{
			table.PrimaryKey("PK_Documents", x => x.Id);
			table.ForeignKey("FK_Documents_Users_UploadedByUserId", x => x.UploadedByUserId, "Users", "Id", null, ReferentialAction.NoAction, ReferentialAction.Restrict);
		});
		migrationBuilder.CreateTable("TaskItems", (ColumnsBuilder table) =>
		{
			OperationBuilder<AddColumnOperation> id = table.Column<Guid>("uniqueidentifier");
			int? maxLength = 200;
			return new
			{
				Id = id,
				Title = table.Column<string>("nvarchar(200)", null, maxLength),
				Description = table.Column<string>("nvarchar(max)"),
				AssignedToUserId = table.Column<Guid>("uniqueidentifier"),
				CreatedByUserId = table.Column<Guid>("uniqueidentifier"),
				Status = table.Column<string>("nvarchar(max)"),
				Priority = table.Column<string>("nvarchar(max)"),
				DueDate = table.Column<DateTime>("datetime2", null, null, rowVersion: false, null, nullable: true),
				CreatedAt = table.Column<DateTime>("datetime2", null, null, rowVersion: false, null, nullable: false, null, "GETUTCDATE()"),
				UpdatedAt = table.Column<DateTime>("datetime2", null, null, rowVersion: false, null, nullable: false, null, "GETUTCDATE()")
			};
		}, null, table =>
		{
			table.PrimaryKey("PK_TaskItems", x => x.Id);
			table.ForeignKey("FK_TaskItems_Users_AssignedToUserId", x => x.AssignedToUserId, "Users", "Id", null, ReferentialAction.NoAction, ReferentialAction.Restrict);
			table.ForeignKey("FK_TaskItems_Users_CreatedByUserId", x => x.CreatedByUserId, "Users", "Id", null, ReferentialAction.NoAction, ReferentialAction.Restrict);
		});
		migrationBuilder.CreateTable("Themes", (ColumnsBuilder table) =>
		{
			OperationBuilder<AddColumnOperation> id = table.Column<Guid>("uniqueidentifier");
			int? maxLength = 200;
			OperationBuilder<AddColumnOperation> name = table.Column<string>("nvarchar(200)", null, maxLength);
			maxLength = 50;
			OperationBuilder<AddColumnOperation> primaryColor = table.Column<string>("nvarchar(50)", null, maxLength);
			maxLength = 50;
			OperationBuilder<AddColumnOperation> secondaryColor = table.Column<string>("nvarchar(50)", null, maxLength);
			maxLength = 50;
			OperationBuilder<AddColumnOperation> accentColor = table.Column<string>("nvarchar(50)", null, maxLength);
			maxLength = 50;
			OperationBuilder<AddColumnOperation> backgroundColor = table.Column<string>("nvarchar(50)", null, maxLength);
			maxLength = 50;
			return new
			{
				Id = id,
				Name = name,
				PrimaryColor = primaryColor,
				SecondaryColor = secondaryColor,
				AccentColor = accentColor,
				BackgroundColor = backgroundColor,
				TextColor = table.Column<string>("nvarchar(50)", null, maxLength),
				IsActive = table.Column<bool>("bit", null, null, rowVersion: false, null, nullable: false, false),
				CreatedByUserId = table.Column<Guid>("uniqueidentifier"),
				CreatedAt = table.Column<DateTime>("datetime2", null, null, rowVersion: false, null, nullable: false, null, "GETUTCDATE()")
			};
		}, null, table =>
		{
			table.PrimaryKey("PK_Themes", x => x.Id);
			table.ForeignKey("FK_Themes_Users_CreatedByUserId", x => x.CreatedByUserId, "Users", "Id", null, ReferentialAction.NoAction, ReferentialAction.Restrict);
		});
		migrationBuilder.CreateTable("Opportunities", (ColumnsBuilder table) =>
		{
			OperationBuilder<AddColumnOperation> id = table.Column<Guid>("uniqueidentifier");
			int? maxLength = 200;
			return new
			{
				Id = id,
				Title = table.Column<string>("nvarchar(200)", null, maxLength),
				ClientId = table.Column<Guid>("uniqueidentifier"),
				Value = table.Column<decimal>("decimal(18,2)"),
				Status = table.Column<string>("nvarchar(max)"),
				AssignedToUserId = table.Column<Guid>("uniqueidentifier"),
				Description = table.Column<string>("nvarchar(max)"),
				CreatedAt = table.Column<DateTime>("datetime2", null, null, rowVersion: false, null, nullable: false, null, "GETUTCDATE()"),
				UpdatedAt = table.Column<DateTime>("datetime2", null, null, rowVersion: false, null, nullable: false, null, "GETUTCDATE()")
			};
		}, null, table =>
		{
			table.PrimaryKey("PK_Opportunities", x => x.Id);
			table.ForeignKey("FK_Opportunities_Clients_ClientId", x => x.ClientId, "Clients", "Id", null, ReferentialAction.NoAction, ReferentialAction.Cascade);
			table.ForeignKey("FK_Opportunities_Users_AssignedToUserId", x => x.AssignedToUserId, "Users", "Id", null, ReferentialAction.NoAction, ReferentialAction.Restrict);
		});
		migrationBuilder.CreateIndex("IX_Clients_AssignedToUserId", "Clients", "AssignedToUserId");
		migrationBuilder.CreateIndex("IX_Documents_UploadedByUserId", "Documents", "UploadedByUserId");
		migrationBuilder.CreateIndex("IX_Opportunities_AssignedToUserId", "Opportunities", "AssignedToUserId");
		migrationBuilder.CreateIndex("IX_Opportunities_ClientId", "Opportunities", "ClientId");
		migrationBuilder.CreateIndex("IX_SystemSettings_Key", "SystemSettings", "Key", null, unique: true);
		migrationBuilder.CreateIndex("IX_TaskItems_AssignedToUserId", "TaskItems", "AssignedToUserId");
		migrationBuilder.CreateIndex("IX_TaskItems_CreatedByUserId", "TaskItems", "CreatedByUserId");
		migrationBuilder.CreateIndex("IX_Themes_CreatedByUserId", "Themes", "CreatedByUserId");
		migrationBuilder.CreateIndex("IX_Users_Username", "Users", "Username", null, unique: true);
	}

	protected override void Down(MigrationBuilder migrationBuilder)
	{
		migrationBuilder.DropTable("Customers");
		migrationBuilder.DropTable("Documents");
		migrationBuilder.DropTable("Opportunities");
		migrationBuilder.DropTable("SystemSettings");
		migrationBuilder.DropTable("TaskItems");
		migrationBuilder.DropTable("Themes");
		migrationBuilder.DropTable("Clients");
		migrationBuilder.DropTable("Users");
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
			b.Property<Guid>("ClientId").HasColumnType("uniqueidentifier");
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
				.OnDelete(DeleteBehavior.Cascade)
				.IsRequired();
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
		modelBuilder.Entity("Qore.Data.Entities.Client", (EntityTypeBuilder b) =>
		{
			b.Navigation("Opportunities");
		});
	}
}
