using System;
using System.Runtime.CompilerServices;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using BCrypt.Net;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Qore.Data;
using Qore.Data.Entities;
using Qore.Shared.Enums;

[CompilerGenerated]
internal class Program
{
	private static async Task Main(string[] args)
	{
		WebApplicationBuilder builder = WebApplication.CreateBuilder(new WebApplicationOptions
		{
			Args = args,
			ContentRootPath = AppContext.BaseDirectory
		});
		string jwtSecret = builder.Configuration["Jwt:Secret"];
		if (string.IsNullOrWhiteSpace(jwtSecret))
		{
			throw new InvalidOperationException("Jwt:Secret is required.");
		}
		string jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "Qore.Api";
		string jwtAudience = builder.Configuration["Jwt:Audience"] ?? "Qore.Client";
		byte[] signingKeyBytes = SHA256.HashData(Encoding.UTF8.GetBytes(jwtSecret));
		builder.Services.AddAuthentication("Bearer").AddJwtBearer((JwtBearerOptions options) =>
		{
			options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
			options.SaveToken = true;
			options.TokenValidationParameters = new TokenValidationParameters
			{
				ValidateIssuerSigningKey = true,
				IssuerSigningKey = new SymmetricSecurityKey(signingKeyBytes),
				ValidateIssuer = true,
				ValidIssuer = jwtIssuer,
				ValidateAudience = true,
				ValidAudience = jwtAudience
			};
			options.Events = new JwtBearerEvents
			{
				OnTokenValidated = async (TokenValidatedContext context) =>
				{
					string jti = context.Principal?.FindFirstValue("jti");
					string userIdClaim = context.Principal?.FindFirstValue("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier") ?? context.Principal?.FindFirstValue("UserId");
					if (string.IsNullOrWhiteSpace(jti) || string.IsNullOrWhiteSpace(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
					{
						context.Fail("Invalid token session");
					}
					else
					{
						QoreDbContext db2 = context.HttpContext.RequestServices.GetRequiredService<QoreDbContext>();
						UserSession session = await db2.UserSessions.FirstOrDefaultAsync((UserSession s) => s.UserId == userId && s.Jti == jti);
						if (session == null || session.IsRevoked || session.ExpiresAt < DateTime.UtcNow)
						{
							context.Fail("Session is no longer valid.");
						}
					}
				}
			};
		});
		builder.Services.AddAuthorization((AuthorizationOptions options) =>
		{
			options.AddPolicy("AdminOnly", (AuthorizationPolicyBuilder policy) =>
			{
				policy.RequireRole(UserRole.Admin.ToString());
			});
			options.AddPolicy("ManagerOrAdmin", (AuthorizationPolicyBuilder policy) =>
			{
				policy.RequireRole(UserRole.Manager.ToString(), UserRole.Admin.ToString());
			});
			options.AddPolicy("AnyAuthenticated", (AuthorizationPolicyBuilder policy) =>
			{
				policy.RequireAuthenticatedUser();
			});
		});
		builder.Services.AddControllers();
		builder.Services.AddEndpointsApiExplorer();
		builder.Services.AddSwaggerGen();
		string connectionString = builder.Configuration.GetConnectionString("QoreDb");
		if (string.IsNullOrWhiteSpace(connectionString))
		{
			throw new InvalidOperationException("Connection string 'QoreDb' was not found.");
		}
		builder.Services.AddDbContext<QoreDbContext>((DbContextOptionsBuilder options) =>
		{
			options.UseSqlServer(connectionString);
		});
		string configuredUrls = builder.Configuration["ASPNETCORE_URLS"] ?? builder.Configuration["Urls"];
		if (!string.IsNullOrWhiteSpace(configuredUrls))
		{
			builder.WebHost.UseUrls(configuredUrls);
		}
		WebApplication app = builder.Build();
		try
		{
			using IServiceScope scope = app.Services.CreateScope();
			QoreDbContext db = scope.ServiceProvider.GetRequiredService<QoreDbContext>();
			await db.Database.MigrateAsync();
			app.Logger.LogInformation("[Qore] Database created and migrated successfully");
			if (!(await db.Users.AnyAsync()))
			{
				string initialAdminUsername = builder.Configuration["QORE_INITIAL_ADMIN_USERNAME"]?.Trim() ?? "admin";
				string initialAdminPassword = builder.Configuration["QORE_INITIAL_ADMIN_PASSWORD"] ?? string.Empty;
				if (string.IsNullOrWhiteSpace(initialAdminUsername) || initialAdminPassword.Length < 12)
				{
					throw new InvalidOperationException("Set QORE_INITIAL_ADMIN_USERNAME and QORE_INITIAL_ADMIN_PASSWORD (at least 12 characters) before starting with an empty database.");
				}
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
				db.Users.Add(adminUser);
				await db.SaveChangesAsync();
				app.Logger.LogInformation("[Qore] Initial administrator account seeded");
			}
			User defaultAdmin = await db.Users.FirstOrDefaultAsync((User u) => u.Role == UserRole.Admin);
			if (defaultAdmin == null)
			{
				throw new InvalidOperationException("At least one administrator account must exist before seeding themes.");
			}
			if (!(await db.Themes.AnyAsync()))
			{
				db.Themes.Add(new Theme
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
					CreatedAt = DateTime.UtcNow,
					CreatedByUserId = defaultAdmin.Id
				});
				await db.SaveChangesAsync();
				app.Logger.LogInformation("[Qore] Default theme seeded");
			}
		}
		catch (Exception exception)
		{
			app.Logger.LogError(exception, "[Qore] Database initialization failed");
			throw;
		}
		if (app.Environment.IsDevelopment())
		{
			app.UseSwagger();
			app.UseSwaggerUI();
		}
		else
		{
			app.UseHsts();
			app.UseHttpsRedirection();
		}
		DefaultFilesOptions defaultFilesOptions = new DefaultFilesOptions
		{
			DefaultFileNames = new string[1] { "login.html" }
		};
		app.UseDefaultFiles(defaultFilesOptions);
		app.UseStaticFiles(new StaticFileOptions
		{
			OnPrepareResponse = (StaticFileResponseContext context) =>
			{
				string text = context.File?.PhysicalPath ?? string.Empty;
				if (text.EndsWith(".js", StringComparison.OrdinalIgnoreCase) || text.EndsWith(".css", StringComparison.OrdinalIgnoreCase) || text.EndsWith(".html", StringComparison.OrdinalIgnoreCase))
				{
					context.Context.Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
					context.Context.Response.Headers["Pragma"] = "no-cache";
					context.Context.Response.Headers["Expires"] = "0";
				}
			}
		});
		app.UseRouting();
		app.Use(async (HttpContext context, Func<Task> next) =>
		{
			context.Response.Headers["X-Content-Type-Options"] = "nosniff";
			context.Response.Headers["X-Frame-Options"] = "DENY";
			context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
			await next();
		});
		app.UseAuthentication();
		app.UseAuthorization();
		app.MapControllers();
		app.Run();
	}
}
