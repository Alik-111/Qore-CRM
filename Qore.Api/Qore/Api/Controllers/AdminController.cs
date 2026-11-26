using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BCrypt.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Qore.Data;
using Qore.Data.Entities;
using Qore.Shared.DTOs;
using Qore.Shared.Enums;

namespace Qore.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "ManagerOrAdmin")]
public class AdminController : ControllerBase
{
	public sealed class ResetPasswordDto
	{
		public string NewPassword { get; set; } = string.Empty;
	}

	private readonly QoreDbContext _context;

	public AdminController(QoreDbContext context)
	{
		_context = context;
	}

	[HttpGet("users")]
	public async Task<ActionResult<IEnumerable<UserResponseDto>>> GetUsers()
	{
		return Ok(await (from u in _context.Users
			orderby u.Username
			select new UserResponseDto
			{
				Id = u.Id,
				Username = u.Username,
				FullName = u.FullName,
				Email = u.Email,
				Role = u.Role.ToString(),
				IsActive = u.IsActive,
				CreatedAt = u.CreatedAt,
				LastLoginAt = u.LastLoginAt
			}).ToListAsync());
	}

	[HttpPost("users")]
	public async Task<ActionResult<UserResponseDto>> CreateUser([FromBody] CreateUserDto request)
	{
		if (!ModelState.IsValid)
		{
			return ValidationProblem(ModelState);
		}
		if (!IsValidPassword(request.Password))
		{
			return BadRequest(new
			{
				success = false,
				message = "Password must be at least 8 characters and contain at least one number."
			});
		}
		if (await _context.Users.AnyAsync((User u) => u.Username == request.Username))
		{
			return Conflict(new
			{
				success = false,
				message = "Username already exists."
			});
		}
		if (await _context.Users.AnyAsync((User u) => u.Email == request.Email))
		{
			return Conflict(new
			{
				success = false,
				message = "Email already exists."
			});
		}
		if (!Enum.TryParse<UserRole>(request.Role, ignoreCase: true, out var role))
		{
			return BadRequest(new
			{
				success = false,
				message = "Invalid role."
			});
		}
		User user = new User
		{
			Id = Guid.NewGuid(),
			Username = request.Username.Trim(),
			PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
			PasswordSalt = string.Empty,
			FullName = request.FullName.Trim(),
			Email = request.Email.Trim(),
			Role = role,
			IsActive = true,
			CreatedAt = DateTime.UtcNow
		};
		_context.Users.Add(user);
		await _context.SaveChangesAsync();
		return CreatedAtAction("GetUsers", new
		{
			id = user.Id
		}, MapToDto(user));
	}

	[HttpPut("users/{id:guid}")]
	public async Task<ActionResult<UserResponseDto>> UpdateUser(Guid id, [FromBody] UpdateUserDto request)
	{
		if (!ModelState.IsValid)
		{
			return ValidationProblem(ModelState);
		}
		User user = await _context.Users.FirstOrDefaultAsync((User u) => u.Id == id);
		if (user == null)
		{
			return NotFound(new
			{
				success = false,
				message = "User not found."
			});
		}
		if (await _context.Users.AnyAsync((User u) => u.Id != id && u.Username == request.Username))
		{
			return Conflict(new
			{
				success = false,
				message = "Username already exists."
			});
		}
		if (await _context.Users.AnyAsync((User u) => u.Id != id && u.Email == request.Email))
		{
			return Conflict(new
			{
				success = false,
				message = "Email already exists."
			});
		}
		if (!Enum.TryParse<UserRole>(request.Role, ignoreCase: true, out var role))
		{
			return BadRequest(new
			{
				success = false,
				message = "Invalid role."
			});
		}
		user.Username = request.Username.Trim();
		user.FullName = request.FullName.Trim();
		user.Email = request.Email.Trim();
		user.Role = role;
		await _context.SaveChangesAsync();
		return Ok(MapToDto(user));
	}

	[HttpPut("users/{id:guid}/reset-password")]
	public async Task<ActionResult> ResetPassword(Guid id, [FromBody] ResetPasswordDto request)
	{
		if (!IsValidPassword(request.NewPassword))
		{
			return BadRequest(new
			{
				success = false,
				message = "Password must be at least 8 characters and contain at least one number."
			});
		}
		User user = await _context.Users.FirstOrDefaultAsync((User u) => u.Id == id);
		if (user == null)
		{
			return NotFound(new
			{
				success = false,
				message = "User not found."
			});
		}
		user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
		user.PasswordSalt = string.Empty;
		await _context.SaveChangesAsync();
		return Ok(new
		{
			success = true,
			message = "Password reset successfully."
		});
	}

	[HttpPut("users/{id:guid}/activate")]
	public async Task<ActionResult> ActivateUser(Guid id)
	{
		User user = await _context.Users.FirstOrDefaultAsync((User u) => u.Id == id);
		if (user == null)
		{
			return NotFound(new
			{
				success = false,
				message = "User not found."
			});
		}
		user.IsActive = true;
		await _context.SaveChangesAsync();
		return Ok(new
		{
			success = true,
			message = "User activated."
		});
	}

	[HttpPut("users/{id:guid}/deactivate")]
	public async Task<ActionResult> DeactivateUser(Guid id)
	{
		User user = await _context.Users.FirstOrDefaultAsync((User u) => u.Id == id);
		if (user == null)
		{
			return NotFound(new
			{
				success = false,
				message = "User not found."
			});
		}
		user.IsActive = false;
		await _context.SaveChangesAsync();
		return Ok(new
		{
			success = true,
			message = "User deactivated."
		});
	}

	[HttpDelete("users/{id:guid}")]
	public async Task<ActionResult> DeleteUser(Guid id)
	{
		User user = await _context.Users.FirstOrDefaultAsync((User u) => u.Id == id);
		if (user == null)
		{
			return NotFound(new
			{
				success = false,
				message = "User not found."
			});
		}
		int adminCount = await _context.Users.CountAsync((User u) => (int)u.Role == 0);
		if (user.Role == UserRole.Admin && adminCount <= 1)
		{
			return BadRequest(new
			{
				success = false,
				message = "Cannot delete the last remaining Admin user."
			});
		}
		_context.Users.Remove(user);
		await _context.SaveChangesAsync();
		return Ok(new
		{
			success = true,
			message = "User deleted."
		});
	}

	private static UserResponseDto MapToDto(User user)
	{
		return new UserResponseDto
		{
			Id = user.Id,
			Username = user.Username,
			FullName = user.FullName,
			Email = user.Email,
			Role = user.Role.ToString(),
			IsActive = user.IsActive,
			CreatedAt = user.CreatedAt,
			LastLoginAt = user.LastLoginAt
		};
	}

	private static bool IsValidPassword(string password)
	{
		if (password.Length >= 8 && password.Any(char.IsDigit))
		{
			return password.Any(char.IsUpper);
		}
		return false;
	}
}
