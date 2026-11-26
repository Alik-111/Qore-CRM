using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Qore.Data;
using Qore.Data.Entities;
using Qore.Shared.DTOs;
using Qore.Shared.Enums;

namespace Qore.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DocumentsController : ControllerBase
{
	private const long MaxUploadFileBytes = 52428800L;

	private static readonly HashSet<string> AllowedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".txt", ".png", ".jpg", ".jpeg" };

	private readonly QoreDbContext _context;

	public DocumentsController(QoreDbContext context)
	{
		_context = context;
	}

	[HttpGet]
	public async Task<ActionResult<IEnumerable<DocumentDto>>> GetDocuments()
	{
		User currentUser = await GetCurrentUserAsync();
		if (currentUser == null)
		{
			return Unauthorized();
		}
		return Ok(await (from d in _context.Documents
			where d.VisibleToRole == (DocumentVisibility?)null || (int?)d.VisibleToRole == (int?)MapRole(currentUser.Role)
			orderby d.CreatedAt descending
			select MapToDto(d)).ToListAsync());
	}

	[HttpGet("{id:guid}/download")]
	public async Task<ActionResult> Download(Guid id)
	{
		User currentUser = await GetCurrentUserAsync();
		if (currentUser == null)
		{
			return Unauthorized();
		}
		Document document = await _context.Documents.FirstOrDefaultAsync((Document d) => d.Id == id);
		if (document == null)
		{
			return NotFound();
		}
		if (document.VisibleToRole.HasValue && document.VisibleToRole != MapRole(currentUser.Role))
		{
			return Forbid();
		}
		return File(document.FileData, document.ContentType, document.FileName);
	}

	[HttpPost("upload")]
	public async Task<ActionResult<DocumentDto>> Upload([FromForm] IFormFile file, [FromForm] string name, [FromForm] string description, [FromForm] string? visibleToRole)
	{
		User currentUser = await GetCurrentUserAsync();
		if (currentUser == null)
		{
			return Unauthorized();
		}
		if (file == null || file.Length == 0L)
		{
			return BadRequest(new
			{
				success = false,
				message = "A file is required."
			});
		}
		if (string.IsNullOrWhiteSpace(name))
		{
			return BadRequest(new
			{
				success = false,
				message = "Document name is required."
			});
		}
		if (file.Length > 52428800)
		{
			return BadRequest(new
			{
				success = false,
				message = "File is too large. Maximum supported size is 50 MB."
			});
		}
		string fileName = Path.GetFileName(file.FileName);
		string extension = Path.GetExtension(fileName);
		if (string.IsNullOrWhiteSpace(extension) || !AllowedExtensions.Contains(extension))
		{
			return BadRequest(new
			{
				success = false,
				message = "File type is not supported."
			});
		}
		DocumentVisibility? visibility = null;
		if (!string.IsNullOrWhiteSpace(visibleToRole) && Enum.TryParse<DocumentVisibility>(visibleToRole, ignoreCase: true, out var parsedVisibility))
		{
			visibility = parsedVisibility;
		}
		using MemoryStream stream = new MemoryStream();
		await file.CopyToAsync(stream);
		Document document = new Document
		{
			Id = Guid.NewGuid(),
			Name = name.Trim(),
			Description = (string.IsNullOrWhiteSpace(description) ? string.Empty : description.Trim()),
			FileData = stream.ToArray(),
			FileName = fileName,
			ContentType = (string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType),
			UploadedByUserId = currentUser.Id,
			VisibleToRole = visibility,
			CreatedAt = DateTime.UtcNow
		};
		_context.Documents.Add(document);
		await _context.SaveChangesAsync();
		return CreatedAtAction("Download", new
		{
			id = document.Id
		}, MapToDto(document));
	}

	[HttpDelete("{id:guid}")]
	public async Task<ActionResult> DeleteDocument(Guid id)
	{
		User currentUser = await GetCurrentUserAsync();
		if (currentUser == null)
		{
			return Unauthorized();
		}
		Document document = await _context.Documents.FirstOrDefaultAsync((Document d) => d.Id == id);
		if (document == null)
		{
			return NotFound();
		}
		if (document.UploadedByUserId != currentUser.Id && !IsManagerOrAdmin(currentUser.Role))
		{
			return Forbid();
		}
		_context.Documents.Remove(document);
		await _context.SaveChangesAsync();
		return Ok();
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

	private static bool IsManagerOrAdmin(UserRole role)
	{
		if (role != UserRole.Manager)
		{
			return role == UserRole.Admin;
		}
		return true;
	}

	private static DocumentVisibility MapRole(UserRole role)
	{
		return role switch
		{
			UserRole.Admin => DocumentVisibility.Admin, 
			UserRole.Manager => DocumentVisibility.Manager, 
			_ => DocumentVisibility.Employee, 
		};
	}

	private static DocumentDto MapToDto(Document document)
	{
		return new DocumentDto
		{
			Id = document.Id,
			Name = document.Name,
			Description = document.Description,
			FileName = document.FileName,
			ContentType = document.ContentType,
			UploadedByUserId = document.UploadedByUserId,
			VisibleToRole = document.VisibleToRole?.ToString(),
			CreatedAt = document.CreatedAt
		};
	}
}
