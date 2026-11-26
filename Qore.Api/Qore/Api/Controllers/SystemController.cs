using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using BCrypt.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Qore.Data;
using Qore.Data.Entities;
using Qore.Shared.Enums;

namespace Qore.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SystemController : ControllerBase
{
	public sealed class RestoreDatabaseRequest
	{
		public IFormFile? File { get; set; }

		public string? DatabaseName { get; set; }
	}

	public sealed class BackupThresholdRequest
	{
		public int? ThresholdGb { get; set; }
	}

	public sealed class ConnectDatabaseRequest
	{
		public string? DatabaseName { get; set; }
	}

	public sealed class ResetSystemRequest
	{
		public bool Confirm { get; set; }
	}

	private const int MinBackupThresholdGb = 50;

	private const int MaxBackupThresholdGb = 150;

	private const long MaxRestoreBackupFileBytes = 5368709120L;

	private readonly QoreDbContext _context;

	private readonly IConfiguration _configuration;

	private readonly IWebHostEnvironment _environment;

	private readonly IHostApplicationLifetime _hostApplicationLifetime;

	private readonly ILogger<SystemController> _logger;

	public SystemController(QoreDbContext context, IConfiguration configuration, IWebHostEnvironment environment, IHostApplicationLifetime hostApplicationLifetime, ILogger<SystemController> logger)
	{
		_context = context;
		_configuration = configuration;
		_environment = environment;
		_hostApplicationLifetime = hostApplicationLifetime;
		_logger = logger;
	}

	[HttpGet("backup-status")]
	[Authorize(Policy = "ManagerOrAdmin")]
	public async Task<IActionResult> GetBackupStatus()
	{
		int thresholdGb = GetBackupThresholdGb();
		string currentDatabase = GetCurrentDatabaseName();
		string connectionString = GetCurrentConnectionString();
		double currentSizeGb = await GetDatabaseSizeGbAsync(connectionString, currentDatabase);
		string backupFolder = EnsureBackupFolder();
		return Ok(new
		{
			success = true,
			currentDatabase = currentDatabase,
			currentSizeGb = currentSizeGb,
			thresholdGb = thresholdGb,
			backupRecommended = (currentSizeGb >= (double)thresholdGb),
			lastBackupFile = GetLatestBackupFileName(backupFolder)
		});
	}

	[HttpPost("backup")]
	[Authorize(Policy = "ManagerOrAdmin")]
	public async Task<IActionResult> CreateBackup()
	{
		_ = 3;
		try
		{
			string backupFolder = EnsureBackupFolder();
			string databaseName = GetCurrentDatabaseName();
			string backupFileName = $"{databaseName}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.bak";
			string backupPath = Path.Combine(backupFolder, backupFileName);
			string connectionString = GetCurrentConnectionString();
			IActionResult result;
			await using (SqlConnection connection = new SqlConnection(connectionString))
			{
				await connection.OpenAsync();
				IActionResult actionResult;
				await using (SqlCommand command = new SqlCommand("BACKUP DATABASE [" + databaseName + "] TO DISK = @path WITH FORMAT, INIT, STATS = 10", connection))
				{
					command.Parameters.AddWithValue("@path", backupPath);
					await command.ExecuteNonQueryAsync();
					FileInfo backupFileInfo = new FileInfo(backupPath);
					actionResult = Ok(new
					{
						success = true,
						fileName = backupFileName,
						sizeBytes = backupFileInfo.Length,
						sizeGb = Math.Round((double)backupFileInfo.Length / 1073741824.0, 2),
						downloadUrl = "/api/system/download-backup/" + Uri.EscapeDataString(backupFileName),
						message = "Backup created successfully for " + databaseName + "."
					});
				}
				result = actionResult;
			}
			return result;
		}
		catch (Exception exception)
		{
			_logger.LogError(exception, "Database backup failed.");
			return StatusCode(500, new
			{
				success = false,
				message = "Backup failed. Verify that SQL Server can write to the backup folder and that the current login has backup permissions."
			});
		}
	}

	[HttpGet("download-backup/{fileName}")]
	[Authorize(Policy = "ManagerOrAdmin")]
	public IActionResult DownloadBackup(string fileName)
	{
		string backupFolder = EnsureBackupFolder();
		string safeFileName = Path.GetFileName(fileName);
		if (string.IsNullOrWhiteSpace(safeFileName) || !string.Equals(safeFileName, fileName, StringComparison.Ordinal))
		{
			return BadRequest(new
			{
				success = false,
				message = "Invalid backup file name."
			});
		}
		string backupPath = Path.Combine(backupFolder, safeFileName);
		if (!System.IO.File.Exists(backupPath))
		{
			return NotFound(new
			{
				success = false,
				message = "Backup file was not found."
			});
		}
		return PhysicalFile(backupPath, "application/octet-stream", safeFileName);
	}

	[HttpPost("restore")]
	[Authorize(Policy = "ManagerOrAdmin")]
	public async Task<IActionResult> RestoreBackup([FromForm] RestoreDatabaseRequest request)
	{
		if (request.File == null || request.File.Length == 0L)
		{
			return BadRequest(new
			{
				success = false,
				message = "Please select a .bak file to restore."
			});
		}
		if (request.File.Length > 5368709120L)
		{
			return BadRequest(new
			{
				success = false,
				message = "Backup file is too large. Maximum supported size is 5 GB."
			});
		}
		if (string.IsNullOrWhiteSpace(request.DatabaseName))
		{
			return BadRequest(new
			{
				success = false,
				message = "A target database name is required."
			});
		}
		string targetDatabaseName = request.DatabaseName.Trim();
		string currentDatabase = GetCurrentDatabaseName();
		if (string.Equals(targetDatabaseName, currentDatabase, StringComparison.OrdinalIgnoreCase))
		{
			return BadRequest(new
			{
				success = false,
				message = "The target database name must be different from the current database to keep the application safe."
			});
		}
		if (!Regex.IsMatch(targetDatabaseName, "^[A-Za-z0-9_#$]+$"))
		{
			return BadRequest(new
			{
				success = false,
				message = "The target database name contains unsupported characters. Use letters, numbers, underscore, # or $ only."
			});
		}
		string extension = Path.GetExtension(request.File.FileName);
		if (!string.Equals(extension, ".bak", StringComparison.OrdinalIgnoreCase))
		{
			return BadRequest(new
			{
				success = false,
				message = "Only .bak backup files are supported for restore."
			});
		}
		string backupFolder = EnsureBackupFolder();
		string backupPath = Path.Combine(backupFolder, $"restore_{Guid.NewGuid():N}_{Path.GetFileName(request.File.FileName)}");
		await using (FileStream stream = System.IO.File.Create(backupPath))
		{
			await request.File.CopyToAsync(stream);
		}
		try
		{
			string masterConnectionString = BuildConnectionStringForDatabase(GetCurrentConnectionString(), "master");
			IActionResult result;
			await using (SqlConnection connection = new SqlConnection(masterConnectionString))
			{
				await connection.OpenAsync();
				if (await DatabaseExistsAsync(connection, targetDatabaseName))
				{
					await using SqlCommand dropCommand = new SqlCommand($"ALTER DATABASE [{targetDatabaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{targetDatabaseName}]", connection);
					await dropCommand.ExecuteNonQueryAsync();
				}
				IActionResult actionResult2;
				await using (SqlCommand restoreCommand = new SqlCommand("RESTORE DATABASE [" + targetDatabaseName + "] FROM DISK = @path WITH REPLACE, RECOVERY, STATS = 10", connection))
				{
					restoreCommand.Parameters.AddWithValue("@path", backupPath);
					await restoreCommand.ExecuteNonQueryAsync();
					IActionResult actionResult;
					await using (SqlCommand postRestoreCommand = new SqlCommand("ALTER DATABASE [" + targetDatabaseName + "] SET MULTI_USER WITH ROLLBACK IMMEDIATE", connection))
					{
						await postRestoreCommand.ExecuteNonQueryAsync();
						string newConnectionString = BuildConnectionStringForDatabase(GetCurrentConnectionString(), targetDatabaseName);
						await UpdateConnectionStringAsync(newConnectionString);
						ScheduleApplicationRestart();
						actionResult = Ok(new
						{
							success = true,
							message = "Database restored to '" + targetDatabaseName + "' and the application will reconnect automatically after restart.",
							targetDatabase = targetDatabaseName
						});
					}
					actionResult2 = actionResult;
				}
				result = actionResult2;
			}
			return result;
		}
		catch (Exception exception)
		{
			_logger.LogError(exception, "Restore operation failed for target database '{DatabaseName}'.", targetDatabaseName);
			return StatusCode(500, new
			{
				success = false,
				message = "Restore failed. Please verify the backup file and SQL Server permissions."
			});
		}
		finally
		{
			if (System.IO.File.Exists(backupPath))
			{
				System.IO.File.Delete(backupPath);
			}
		}
	}

	[HttpPost("connect-database")]
	[Authorize(Policy = "ManagerOrAdmin")]
	public async Task<IActionResult> ConnectDatabase([FromBody] ConnectDatabaseRequest request)
	{
		if (string.IsNullOrWhiteSpace(request.DatabaseName))
		{
			return BadRequest(new
			{
				success = false,
				message = "A target database name is required."
			});
		}
		string targetDatabaseName = request.DatabaseName.Trim();
		if (!Regex.IsMatch(targetDatabaseName, "^[A-Za-z0-9_#$]+$"))
		{
			return BadRequest(new
			{
				success = false,
				message = "The target database name contains unsupported characters. Use letters, numbers, underscore, # or $ only."
			});
		}
		string currentDatabase = GetCurrentDatabaseName();
		if (string.Equals(targetDatabaseName, currentDatabase, StringComparison.OrdinalIgnoreCase))
		{
			return Ok(new
			{
				success = true,
				message = "Already connected to '" + targetDatabaseName + "'.",
				targetDatabase = targetDatabaseName
			});
		}
		string masterConnectionString = BuildConnectionStringForDatabase(GetCurrentConnectionString(), "master");
		try
		{
			IActionResult result;
			await using (SqlConnection connection = new SqlConnection(masterConnectionString))
			{
				await connection.OpenAsync();
				if (!(await DatabaseExistsAsync(connection, targetDatabaseName)))
				{
					result = BadRequest(new
					{
						success = false,
						message = "Database '" + targetDatabaseName + "' does not exist."
					});
				}
				else
				{
					string newConnectionString = BuildConnectionStringForDatabase(GetCurrentConnectionString(), targetDatabaseName);
					await UpdateConnectionStringAsync(newConnectionString);
					ScheduleApplicationRestart();
					result = Ok(new
					{
						success = true,
						message = "Connecting Qore to '" + targetDatabaseName + "'. The application will reconnect automatically after restart.",
						targetDatabase = targetDatabaseName
					});
				}
			}
			return result;
		}
		catch (Exception exception)
		{
			_logger.LogError(exception, "Failed to connect application to target database '{DatabaseName}'.", targetDatabaseName);
			return StatusCode(500, new
			{
				success = false,
				message = "Failed to connect to the selected database."
			});
		}
	}

	[HttpPost("backup-threshold")]
	[Authorize(Policy = "ManagerOrAdmin")]
	public async Task<IActionResult> UpdateBackupThreshold([FromBody] BackupThresholdRequest request)
	{
		if (!request.ThresholdGb.HasValue)
		{
			return BadRequest(new
			{
				success = false,
				message = "A threshold value is required."
			});
		}
		int thresholdGb = request.ThresholdGb.Value;
		if (thresholdGb < 50 || thresholdGb > 150)
		{
			return BadRequest(new
			{
				success = false,
				message = $"Threshold must be between {50} and {150} GB."
			});
		}
		await UpdateBackupThresholdAsync(thresholdGb);
		return Ok(new
		{
			success = true,
			thresholdGb = thresholdGb,
			message = $"Backup threshold updated to {thresholdGb} GB."
		});
	}

	[HttpPost("reset")]
	[Authorize(Policy = "AdminOnly")]
	public async Task<IActionResult> Reset([FromBody] ResetSystemRequest request)
	{
		if (request == null || !request.Confirm)
		{
			return BadRequest(new
			{
				success = false,
				message = "Reset confirmation is required. Send { \"confirm\": true }."
			});
		}
		string initialAdminUsername = _configuration["QORE_INITIAL_ADMIN_USERNAME"]?.Trim() ?? "admin";
		string initialAdminPassword = _configuration["QORE_INITIAL_ADMIN_PASSWORD"] ?? string.Empty;
		if (string.IsNullOrWhiteSpace(initialAdminUsername) || initialAdminPassword.Length < 12)
		{
			return BadRequest(new
			{
				success = false,
				message = "Set QORE_INITIAL_ADMIN_USERNAME and QORE_INITIAL_ADMIN_PASSWORD (at least 12 characters) before resetting."
			});
		}
		IActionResult result;
		await using (IDbContextTransaction transaction = await _context.Database.BeginTransactionAsync())
		{
			try
			{
				List<TaskItem> tasks = await _context.TaskItems.ToListAsync();
				if (tasks.Count > 0)
				{
					_context.TaskItems.RemoveRange(tasks);
				}
				List<Document> documents = await _context.Documents.ToListAsync();
				if (documents.Count > 0)
				{
					_context.Documents.RemoveRange(documents);
				}
				List<Opportunity> opportunities = await _context.Opportunities.ToListAsync();
				if (opportunities.Count > 0)
				{
					_context.Opportunities.RemoveRange(opportunities);
				}
				List<Client> clients = await _context.Clients.ToListAsync();
				if (clients.Count > 0)
				{
					_context.Clients.RemoveRange(clients);
				}
				List<Theme> themes = await _context.Themes.ToListAsync();
				if (themes.Count > 0)
				{
					_context.Themes.RemoveRange(themes);
				}
				List<User> users = await _context.Users.ToListAsync();
				if (users.Count > 0)
				{
					_context.Users.RemoveRange(users);
				}
				await _context.SaveChangesAsync();
				User adminUser = new User
				{
					Id = Guid.NewGuid(),
					Username = initialAdminUsername,
					PasswordHash = BCrypt.Net.BCrypt.HashPassword(initialAdminPassword),
					PasswordSalt = string.Empty,
					FullName = "System Administrator",
					Email = "admin@qore.local",
					Role = UserRole.Admin,
					IsActive = true,
					CreatedAt = DateTime.UtcNow
				};
				_context.Users.Add(adminUser);
				await _context.SaveChangesAsync();
				_context.Themes.Add(new Theme
				{
					Id = Guid.NewGuid(),
					Name = "Qore Dark Blue",
					PrimaryColor = "#0A1F44",
					SecondaryColor = "#16213E",
					AccentColor = "#2E86FF",
					BackgroundColor = "#0D1321",
					TextColor = "#E8EDF5",
					IsActive = true,
					IsDefault = true,
					CreatedByUserId = adminUser.Id,
					CreatedAt = DateTime.UtcNow
				});
				await _context.SaveChangesAsync();
				await transaction.CommitAsync();
				result = Ok(new
				{
					success = true,
					message = "System reset complete. The configured administrator and Qore Dark Blue theme were restored."
				});
			}
			catch (Exception exception)
			{
				_logger.LogError(exception, "System reset failed and transaction was rolled back.");
				await transaction.RollbackAsync();
				result = StatusCode(500, new
				{
					success = false,
					message = "Reset failed and was rolled back."
				});
			}
		}
		return result;
	}

	private string GetCurrentConnectionString()
	{
		return _configuration.GetConnectionString("QoreDb") ?? string.Empty;
	}

	private string GetCurrentDatabaseName()
	{
		string connectionString = GetCurrentConnectionString();
		if (string.IsNullOrWhiteSpace(connectionString))
		{
			return "QoreDB";
		}
		SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder(connectionString);
		return builder.InitialCatalog ?? "QoreDB";
	}

	private string BuildConnectionStringForDatabase(string connectionString, string databaseName)
	{
		SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder(connectionString)
		{
			InitialCatalog = databaseName
		};
		return builder.ConnectionString;
	}

	private string EnsureBackupFolder()
	{
		string configuredPath = _configuration["Backup:FolderPath"];
		string backupFolder = (string.IsNullOrWhiteSpace(configuredPath) ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Qore", "Backups") : configuredPath);
		backupFolder = Path.GetFullPath(backupFolder);
		Directory.CreateDirectory(backupFolder);
		return backupFolder;
	}

	private string? GetLatestBackupFileName(string backupFolder)
	{
		if (!Directory.Exists(backupFolder))
		{
			return null;
		}
		return Directory.GetFiles(backupFolder, "*.bak").OrderByDescending(System.IO.File.GetLastWriteTimeUtc).Select(Path.GetFileName)
			.FirstOrDefault();
	}

	private int GetBackupThresholdGb()
	{
		if (!int.TryParse(_configuration["Backup:ThresholdGb"], out var threshold))
		{
			return 50;
		}
		return Math.Clamp(threshold, 50, 150);
	}

	private async Task UpdateBackupThresholdAsync(int thresholdGb)
	{
		string settingsPath = Path.Combine(_environment.ContentRootPath, "appsettings.json");
		if (!(JsonNode.Parse(await System.IO.File.ReadAllTextAsync(settingsPath)) is JsonObject root))
		{
			throw new InvalidOperationException("Unable to read application settings file.");
		}
		JsonObject backupNode = root["Backup"] as JsonObject;
		if (backupNode == null)
		{
			backupNode = (JsonObject)(root["Backup"] = new JsonObject());
		}
		backupNode["ThresholdGb"] = thresholdGb;
		await System.IO.File.WriteAllTextAsync(settingsPath, root.ToJsonString(new JsonSerializerOptions
		{
			WriteIndented = true
		}));
		Environment.SetEnvironmentVariable("Backup__ThresholdGb", thresholdGb.ToString());
	}

	private async Task<double> GetDatabaseSizeGbAsync(string connectionString, string databaseName)
	{
		_ = 3;
		try
		{
			double result2;
			await using (SqlConnection connection = new SqlConnection(connectionString))
			{
				await connection.OpenAsync();
				double num;
				await using (SqlCommand command = new SqlCommand("SELECT CAST(SUM(CAST(size AS bigint) * 8.0 / 1024.0 / 1024.0) AS decimal(12, 2)) FROM sys.master_files WHERE database_id = DB_ID(@databaseName)", connection))
				{
					command.Parameters.AddWithValue("@databaseName", databaseName);
					object result = await command.ExecuteScalarAsync();
					if (result is decimal sizeGb)
					{
						num = Math.Round((double)sizeGb, 2);
					}
					else
					{
						num = ((!(result is double doubleValue)) ? 0.0 : Math.Round(doubleValue, 2));
					}
				}
				result2 = num;
			}
			return result2;
		}
		catch
		{
			return 0.0;
		}
	}

	private async Task<bool> DatabaseExistsAsync(SqlConnection connection, string databaseName)
	{
		bool result;
		await using (SqlCommand command = new SqlCommand("SELECT 1 FROM sys.databases WHERE name = @databaseName", connection))
		{
			command.Parameters.AddWithValue("@databaseName", databaseName);
			result = await command.ExecuteScalarAsync() != null;
		}
		return result;
	}

	private async Task UpdateConnectionStringAsync(string newConnectionString)
	{
		string settingsPath = Path.Combine(_environment.ContentRootPath, "appsettings.json");
		if (!(JsonNode.Parse(await System.IO.File.ReadAllTextAsync(settingsPath)) is JsonObject root))
		{
			throw new InvalidOperationException("Unable to read application settings file.");
		}
		JsonObject connectionStrings = root["ConnectionStrings"] as JsonObject;
		if (connectionStrings == null)
		{
			connectionStrings = (JsonObject)(root["ConnectionStrings"] = new JsonObject());
		}
		connectionStrings["QoreDb"] = newConnectionString;
		await System.IO.File.WriteAllTextAsync(settingsPath, root.ToJsonString(new JsonSerializerOptions
		{
			WriteIndented = true
		}));
		Environment.SetEnvironmentVariable("ConnectionStrings__QoreDb", newConnectionString);
	}

	private void ScheduleApplicationRestart()
	{
		Task.Run(() =>
		{
			Thread.Sleep(2000);
			RestartApplication();
		});
	}

	private void RestartApplication()
	{
		try
		{
			string processPath = Environment.ProcessPath;
			string assemblyPath = Path.Combine(AppContext.BaseDirectory, "Qore.Api.dll");
			if (!System.IO.File.Exists(assemblyPath))
			{
				assemblyPath = Path.Combine(_environment.ContentRootPath, "bin", "Debug", "net8.0", "Qore.Api.dll");
			}
			ProcessStartInfo startInfo = new ProcessStartInfo
			{
				WorkingDirectory = _environment.ContentRootPath,
				UseShellExecute = false,
				CreateNoWindow = true
			};
			if (System.IO.File.Exists(assemblyPath))
			{
				startInfo.FileName = "dotnet";
				startInfo.ArgumentList.Add(assemblyPath);
			}
			else if (!string.IsNullOrWhiteSpace(processPath))
			{
				startInfo.FileName = processPath;
			}
			else
			{
				startInfo.FileName = "dotnet";
				startInfo.ArgumentList.Add("Qore.Api.dll");
			}
			Process.Start(startInfo);
			_hostApplicationLifetime.StopApplication();
		}
		catch
		{
		}
	}
}
