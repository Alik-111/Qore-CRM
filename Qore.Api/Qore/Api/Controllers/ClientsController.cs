using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
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
[Authorize]
public class ClientsController : ControllerBase
{
	private readonly QoreDbContext _context;

	public ClientsController(QoreDbContext context)
	{
		_context = context;
	}

	[HttpGet]
	public async Task<ActionResult<IEnumerable<ClientDto>>> GetClients([FromQuery] string? search)
	{
		User currentUser = await GetCurrentUserAsync();
		if (currentUser == null)
		{
			return Unauthorized();
		}
		IQueryable<Client> query = _context.Clients.AsQueryable();
		if (!string.IsNullOrWhiteSpace(search))
		{
			query = query.Where((Client c) => c.Name.Contains(search) || c.CompanyName.Contains(search));
		}
		if (IsEmployee(currentUser.Role))
		{
			query = query.Where((Client c) => c.AssignedToUserId == null || c.AssignedToUserId == currentUser.Id);
		}
		return Ok(await (from c in query
			orderby c.Name
			select MapToDto(c)).ToListAsync());
	}

	[HttpGet("{id:guid}")]
	public async Task<ActionResult<ClientDto>> GetClient(Guid id)
	{
		User currentUser = await GetCurrentUserAsync();
		if (currentUser == null)
		{
			return Unauthorized();
		}
		Client client = await _context.Clients.FirstOrDefaultAsync((Client c) => c.Id == id);
		if (client == null)
		{
			return NotFound();
		}
		if (IsEmployee(currentUser.Role) && client.AssignedToUserId.HasValue && client.AssignedToUserId != currentUser.Id)
		{
			return Forbid();
		}
		return Ok(MapToDto(client));
	}

	[HttpPost]
	public async Task<ActionResult<ClientDto>> CreateClient([FromBody] CreateClientDto request)
	{
		if (!ModelState.IsValid)
		{
			return ValidationProblem(ModelState);
		}
		User currentUser = await GetCurrentUserAsync();
		if (currentUser == null)
		{
			return Unauthorized();
		}
		if (IsEmployee(currentUser.Role) && request.AssignedToUserId.HasValue && request.AssignedToUserId != currentUser.Id)
		{
			return Forbid();
		}
		Client client = new Client
		{
			Id = Guid.NewGuid(),
			Name = request.Name.Trim(),
			CompanyName = request.CompanyName.Trim(),
			Email = request.Email.Trim(),
			Phone = request.Phone.Trim(),
			Address = request.Address.Trim(),
			Notes = request.Notes.Trim(),
			AssignedToUserId = request.AssignedToUserId,
			CreatedAt = DateTime.UtcNow,
			UpdatedAt = DateTime.UtcNow
		};
		_context.Clients.Add(client);
		await _context.SaveChangesAsync();
		return CreatedAtAction("GetClient", new
		{
			id = client.Id
		}, MapToDto(client));
	}

	[HttpPut("{id:guid}")]
	public async Task<ActionResult<ClientDto>> UpdateClient(Guid id, [FromBody] UpdateClientDto request)
	{
		if (!ModelState.IsValid)
		{
			return ValidationProblem(ModelState);
		}
		User currentUser = await GetCurrentUserAsync();
		if (currentUser == null)
		{
			return Unauthorized();
		}
		Client client = await _context.Clients.FirstOrDefaultAsync((Client c) => c.Id == id);
		if (client == null)
		{
			return NotFound();
		}
		if (IsEmployee(currentUser.Role))
		{
			if (client.AssignedToUserId != currentUser.Id)
			{
				return Forbid();
			}
			if (request.AssignedToUserId.HasValue && request.AssignedToUserId != currentUser.Id)
			{
				return Forbid();
			}
		}
		client.Name = request.Name.Trim();
		client.CompanyName = request.CompanyName.Trim();
		client.Email = request.Email.Trim();
		client.Phone = request.Phone.Trim();
		client.Address = request.Address.Trim();
		client.Notes = request.Notes.Trim();
		client.AssignedToUserId = request.AssignedToUserId ?? client.AssignedToUserId;
		client.UpdatedAt = DateTime.UtcNow;
		await _context.SaveChangesAsync();
		return Ok(MapToDto(client));
	}

	[HttpDelete("{id:guid}")]
	public async Task<ActionResult> DeleteClient(Guid id)
	{
		User currentUser = await GetCurrentUserAsync();
		if (currentUser == null)
		{
			return Unauthorized();
		}
		if (IsEmployee(currentUser.Role))
		{
			return Forbid();
		}
		Client client = await _context.Clients.FirstOrDefaultAsync((Client c) => c.Id == id);
		if (client == null)
		{
			return NotFound();
		}
		_context.Clients.Remove(client);
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

	private static bool IsEmployee(UserRole role)
	{
		return role == UserRole.Employee;
	}

	private static ClientDto MapToDto(Client client)
	{
		return new ClientDto
		{
			Id = client.Id,
			Name = client.Name,
			CompanyName = client.CompanyName,
			Email = client.Email,
			Phone = client.Phone,
			Address = client.Address,
			Notes = client.Notes,
			AssignedToUserId = client.AssignedToUserId,
			CreatedAt = client.CreatedAt,
			UpdatedAt = client.UpdatedAt
		};
	}
}
