using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Qore.Data;

public class QoreDbContextFactory : IDesignTimeDbContextFactory<QoreDbContext>
{
	public QoreDbContext CreateDbContext(string[] args)
	{
		string basePath = Directory.GetCurrentDirectory();
		string configPath = Path.Combine(basePath, "appsettings.json");
		if (!File.Exists(configPath))
		{
			configPath = Path.Combine(basePath, "..", "Qore.Api", "appsettings.json");
		}
		string connectionString = new ConfigurationBuilder().SetBasePath(Path.GetDirectoryName(configPath) ?? basePath).AddJsonFile(Path.GetFileName(configPath), optional: false, reloadOnChange: false).Build()
			.GetConnectionString("QoreDb");
		if (string.IsNullOrWhiteSpace(connectionString))
		{
			throw new InvalidOperationException("Connection string 'QoreDb' was not found in appsettings.json.");
		}
		DbContextOptionsBuilder<QoreDbContext> dbContextOptionsBuilder = new DbContextOptionsBuilder<QoreDbContext>();
		dbContextOptionsBuilder.UseSqlServer(connectionString);
		return new QoreDbContext(dbContextOptionsBuilder.Options);
	}
}
