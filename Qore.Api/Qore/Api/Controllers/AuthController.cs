using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using BCrypt.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Qore.Data;
using Qore.Data.Entities;

namespace Qore.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
	public sealed class LoginRequest
	{
		public string Username { get; set; } = string.Empty;

		public string Password { get; set; } = string.Empty;
	}

	public sealed class ChangePasswordRequest
	{
		public string CurrentPassword { get; set; } = string.Empty;

		public string NewPassword { get; set; } = string.Empty;
	}

	private readonly QoreDbContext _context;

	private readonly IConfiguration _configuration;

	public AuthController(QoreDbContext context, IConfiguration configuration)
	{
		_context = context;
		_configuration = configuration;
	}

	[HttpPost("login")]
	public async Task<IActionResult> Login([FromBody] LoginRequest request)
	{
		if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
		{
			return BadRequest(new
			{
				success = false,
				message = "Username and password are required."
			});
		}
		User user = await _context.Users.FirstOrDefaultAsync((User u) => u.Username == request.Username);
		if (user == null || !user.IsActive)
		{
			return Unauthorized(new
			{
				success = false,
				message = "Invalid username or inactive account."
			});
		}
		if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
		{
			return Unauthorized(new
			{
				success = false,
				message = "Invalid username or password."
			});
		}
		DateTime now = DateTime.UtcNow;
		DateTime expiresAt = now.AddHours(8.0);
		string jti = Guid.NewGuid().ToString();
		foreach (UserSession session in await _context.UserSessions.Where((UserSession s) => s.UserId == user.Id && !s.IsRevoked && s.ExpiresAt >= now).ToListAsync())
		{
			session.IsRevoked = true;
		}
		UserSession userSession = new UserSession
		{
			Id = Guid.NewGuid(),
			UserId = user.Id,
			Jti = jti,
			CreatedAt = now,
			ExpiresAt = expiresAt,
			IsRevoked = false
		};
		_context.UserSessions.Add(userSession);
		user.LastLoginAt = now;
		await _context.SaveChangesAsync();
		string token = GenerateJwtToken(user, jti, expiresAt);
		return Ok(new
		{
			success = true,
			token = token,
			user = new
			{
				id = user.Id,
				username = user.Username,
				fullName = user.FullName,
				role = user.Role.ToString()
			}
		});
	}

	[Authorize]
	[HttpPost("logout")]
	public async Task<IActionResult> Logout()
	{
		string userIdClaim = User.FindFirstValue("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier") ?? User.FindFirstValue("UserId");
		string jti = User.FindFirstValue("jti");
		if (!Guid.TryParse(userIdClaim, out var userId) || string.IsNullOrWhiteSpace(jti))
		{
			return Unauthorized(new
			{
				success = false,
				message = "Invalid session."
			});
		}
		UserSession session = await _context.UserSessions.FirstOrDefaultAsync((UserSession s) => s.UserId == userId && s.Jti == jti);
		if (session != null)
		{
			session.IsRevoked = true;
			await _context.SaveChangesAsync();
		}
		return Ok(new
		{
			success = true,
			message = "Logged out successfully."
		});
	}

	[Authorize]
	[HttpGet("validate-session")]
	public IActionResult ValidateSession()
	{
		return Ok(new
		{
			success = true
		});
	}

	[Authorize]
	[HttpPost("change-password")]
	public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
	{
		if (string.IsNullOrWhiteSpace(request.CurrentPassword) || string.IsNullOrWhiteSpace(request.NewPassword))
		{
			return BadRequest(new
			{
				success = false,
				message = "Current and new passwords are required."
			});
		}
		string userIdClaim = User.FindFirstValue("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier") ?? User.FindFirstValue("UserId");
		if (!Guid.TryParse(userIdClaim, out var userId))
		{
			return Unauthorized(new
			{
				success = false,
				message = "Invalid user context."
			});
		}
		User user = await _context.Users.FirstOrDefaultAsync((User u) => u.Id == userId);
		if (user == null || !user.IsActive)
		{
			return Unauthorized(new
			{
				success = false,
				message = "User not found or inactive."
			});
		}
		if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
		{
			return BadRequest(new
			{
				success = false,
				message = "Current password is incorrect."
			});
		}
		if (!IsValidPassword(request.NewPassword))
		{
			return BadRequest(new
			{
				success = false,
				message = "Password must be at least 8 characters and include at least one uppercase letter and one number."
			});
		}
		user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
		user.PasswordSalt = string.Empty;
		await _context.SaveChangesAsync();
		return Ok(new
		{
			success = true,
			message = "Password changed successfully."
		});
	}

	private string GenerateJwtToken(User user, string jti, DateTime expiresAt)
	{
		string jwtSecret = _configuration["Jwt:Secret"];
		if (string.IsNullOrWhiteSpace(jwtSecret))
		{
			throw new InvalidOperationException("Jwt:Secret is required.");
		}
		string jwtIssuer = _configuration["Jwt:Issuer"] ?? "Qore.Api";
		string jwtAudience = _configuration["Jwt:Audience"] ?? "Qore.Client";
		byte[] signingKey = SHA256.HashData(Encoding.UTF8.GetBytes(jwtSecret));
		SymmetricSecurityKey securityKey = new SymmetricSecurityKey(signingKey);
		List<Claim> claims = new List<Claim>
		{
			new Claim("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier", user.Id.ToString()),
			new Claim("UserId", user.Id.ToString()),
			new Claim("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name", user.Username),
			new Claim("Username", user.Username),
			new Claim("http://schemas.microsoft.com/ws/2008/06/identity/claims/role", user.Role.ToString()),
			new Claim("Role", user.Role.ToString()),
			new Claim("jti", jti)
		};
		SecurityTokenDescriptor tokenDescriptor = new SecurityTokenDescriptor
		{
			Subject = new ClaimsIdentity(claims),
			Expires = expiresAt,
			Issuer = jwtIssuer,
			Audience = jwtAudience,
			SigningCredentials = new SigningCredentials(securityKey, "http://www.w3.org/2001/04/xmldsig-more#hmac-sha256")
		};
		JwtSecurityTokenHandler tokenHandler = new JwtSecurityTokenHandler();
		SecurityToken token = tokenHandler.CreateToken(tokenDescriptor);
		return tokenHandler.WriteToken(token);
	}

	private static bool IsValidPassword(string password)
	{
		if (string.IsNullOrWhiteSpace(password) || password.Length < 8)
		{
			return false;
		}
		bool hasUpper = password.Any(char.IsUpper);
		bool hasDigit = password.Any(char.IsDigit);
		return hasUpper & hasDigit;
	}
}
