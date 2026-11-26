using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Qore.Data.Migrations;

[DbContext(typeof(QoreDbContext))]
[Migration("20260713_MarkQoreDarkBlueAsDefault")]
public class MarkQoreDarkBlueAsDefault : Migration
{
	protected override void Up(MigrationBuilder migrationBuilder)
	{
		migrationBuilder.Sql("UPDATE Themes SET IsDefault = 1 WHERE Name = 'Qore Dark Blue'");
	}

	protected override void Down(MigrationBuilder migrationBuilder)
	{
		migrationBuilder.Sql("UPDATE Themes SET IsDefault = 0 WHERE Name = 'Qore Dark Blue'");
	}

	protected override void BuildTargetModel(ModelBuilder modelBuilder)
	{
		modelBuilder.HasAnnotation("ProductVersion", "8.0.8").HasAnnotation("Relational:MaxIdentifierLength", 128);
		modelBuilder.UseIdentityColumns(1L);
	}
}
