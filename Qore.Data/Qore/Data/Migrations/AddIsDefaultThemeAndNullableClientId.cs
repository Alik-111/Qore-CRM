using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Qore.Data.Migrations;

[DbContext(typeof(QoreDbContext))]
[Migration("20260713_AddIsDefaultThemeAndNullableClientId")]
public class AddIsDefaultThemeAndNullableClientId : Migration
{
	protected override void Up(MigrationBuilder migrationBuilder)
	{
		migrationBuilder.AddColumn<bool>("IsDefault", "Themes", "bit", null, null, rowVersion: false, null, nullable: false, false);
		Type typeFromHandle = typeof(Guid);
		migrationBuilder.AlterColumn<Guid>("ClientId", "Opportunities", "uniqueidentifier", null, null, rowVersion: false, null, nullable: true, null, null, null, typeFromHandle, "uniqueidentifier");
	}

	protected override void Down(MigrationBuilder migrationBuilder)
	{
		migrationBuilder.DropColumn("IsDefault", "Themes");
		Type typeFromHandle = typeof(Guid);
		migrationBuilder.AlterColumn<Guid>("ClientId", "Opportunities", "uniqueidentifier", null, null, rowVersion: false, null, nullable: false, null, null, null, typeFromHandle, "uniqueidentifier", null, null, oldRowVersion: false, oldNullable: true);
	}

	protected override void BuildTargetModel(ModelBuilder modelBuilder)
	{
		modelBuilder.HasAnnotation("ProductVersion", "8.0.8").HasAnnotation("Relational:MaxIdentifierLength", 128);
		modelBuilder.UseIdentityColumns(1L);
	}
}
