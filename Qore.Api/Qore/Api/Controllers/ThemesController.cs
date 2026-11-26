using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Qore.Data;
using Qore.Data.Entities;
using Qore.Shared.DTOs;

namespace Qore.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ThemesController : ControllerBase
{
	private readonly QoreDbContext _context;

	public ThemesController(QoreDbContext context)
	{
		_context = context;
	}

	[HttpGet]
	public async Task<ActionResult<IEnumerable<ThemeDto>>> GetThemes()
	{
		return Ok(await (from t in _context.Themes
			orderby t.IsActive descending, t.Name
			select MapToDto(t)).ToListAsync());
	}

	[HttpGet("active")]
	public async Task<ActionResult<ThemeDto>> GetActiveTheme()
	{
		Theme theme = await _context.Themes.FirstOrDefaultAsync((Theme t) => t.IsActive);
		if (theme == null)
		{
			return NotFound(new
			{
				success = false,
				message = "No active theme found."
			});
		}
		return Ok(MapToDto(theme));
	}

	[HttpPost]
	public async Task<ActionResult<ThemeDto>> CreateTheme([FromBody] CreateThemeDto request)
	{
		if (!ModelState.IsValid)
		{
			return ValidationProblem(ModelState);
		}
		User user = await GetCurrentUserAsync();
		Guid? guid = ((user == null) ? (await ((IQueryable<User>)_context.Users.OrderBy((User u) => u.CreatedAt)).Select((Expression<Func<User, Guid?>>)((User u) => u.Id)).FirstOrDefaultAsync()) : new Guid?(user.Id));
		Guid? creatorUserId = guid;
		if (!creatorUserId.HasValue)
		{
			return StatusCode(500, new
			{
				success = false,
				message = "No user account is available to assign to the new theme."
			});
		}
		Theme theme = new Theme
		{
			Id = Guid.NewGuid(),
			Name = request.Name.Trim(),
			PrimaryColor = request.PrimaryColor.Trim(),
			SecondaryColor = request.SecondaryColor.Trim(),
			AccentColor = request.AccentColor.Trim(),
			BackgroundColor = request.BackgroundColor.Trim(),
			TextColor = request.TextColor.Trim(),
			IsActive = false,
			CreatedByUserId = creatorUserId.Value,
			CreatedAt = DateTime.UtcNow
		};
		_context.Themes.Add(theme);
		await _context.SaveChangesAsync();
		return CreatedAtAction("GetThemes", new
		{
			id = theme.Id
		}, MapToDto(theme));
	}

	[HttpPut("{id:guid}")]
	public async Task<ActionResult<ThemeDto>> UpdateTheme(Guid id, [FromBody] UpdateThemeDto request)
	{
		if (!ModelState.IsValid)
		{
			return ValidationProblem(ModelState);
		}
		Theme theme = await _context.Themes.FirstOrDefaultAsync((Theme t) => t.Id == id);
		if (theme == null)
		{
			return NotFound(new
			{
				success = false,
				message = "Theme not found."
			});
		}
		theme.Name = request.Name.Trim();
		theme.PrimaryColor = request.PrimaryColor.Trim();
		theme.SecondaryColor = request.SecondaryColor.Trim();
		theme.AccentColor = request.AccentColor.Trim();
		theme.BackgroundColor = request.BackgroundColor.Trim();
		theme.TextColor = request.TextColor.Trim();
		await _context.SaveChangesAsync();
		return Ok(MapToDto(theme));
	}

	[HttpPut("{id:guid}/activate")]
	public async Task<ActionResult<ThemeDto>> ActivateTheme(Guid id)
	{
		Theme theme = await _context.Themes.FirstOrDefaultAsync((Theme t) => t.Id == id);
		if (theme == null)
		{
			return NotFound(new
			{
				success = false,
				message = "Theme not found."
			});
		}
		foreach (Theme item in await _context.Themes.ToListAsync())
		{
			item.IsActive = item.Id == theme.Id;
		}
		await _context.SaveChangesAsync();
		return Ok(MapToDto(theme));
	}

	[HttpDelete("{id:guid}")]
	public async Task<ActionResult> DeleteTheme(Guid id)
	{
		Theme theme = await _context.Themes.FirstOrDefaultAsync((Theme t) => t.Id == id);
		if (theme == null)
		{
			return NotFound(new
			{
				success = false,
				message = "Theme not found."
			});
		}
		if (theme.IsActive)
		{
			return BadRequest(new
			{
				success = false,
				message = "The active theme cannot be deleted."
			});
		}
		if (theme.IsDefault)
		{
			return BadRequest(new
			{
				success = false,
				message = "The default theme cannot be deleted."
			});
		}
		_context.Themes.Remove(theme);
		await _context.SaveChangesAsync();
		return Ok(new
		{
			success = true,
			message = "Theme deleted."
		});
	}

	private async Task<User?> GetCurrentUserAsync()
	{
		string userIdClaim = User.FindFirstValue("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier") ?? User.FindFirstValue("UserId");
		if (!Guid.TryParse(userIdClaim, out var userId))
		{
			return null;
		}
		return await _context.Users.FirstOrDefaultAsync((User u) => u.Id == userId);
	}

	private static ThemeDto MapToDto(Theme theme)
	{
		return new ThemeDto
		{
			Id = theme.Id,
			Name = theme.Name,
			PrimaryColor = theme.PrimaryColor,
			SecondaryColor = theme.SecondaryColor,
			AccentColor = theme.AccentColor,
			BackgroundColor = theme.BackgroundColor,
			TextColor = theme.TextColor,
			IsActive = theme.IsActive,
			IsDefault = theme.IsDefault,
			CreatedAt = theme.CreatedAt
		};
	}
}
