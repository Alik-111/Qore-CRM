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
public class OpportunitiesController : ControllerBase
{
	private readonly QoreDbContext _context;

	public OpportunitiesController(QoreDbContext context)
	{
		_context = context;
	}

	[HttpGet]
	public async Task<ActionResult<IEnumerable<OpportunityDto>>> GetOpportunities([FromQuery] string? status)
	{
		User currentUser = await GetCurrentUserAsync();
		if (currentUser == null)
		{
			return Unauthorized();
		}
		IQueryable<Opportunity> query = _context.Opportunities.AsQueryable();
		if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<OpportunityStatus>(status, ignoreCase: true, out var parsedStatus))
		{
			query = query.Where((Opportunity o) => (int)o.Status == (int)parsedStatus);
		}
		if (IsEmployee(currentUser.Role))
		{
			query = query.Where((Opportunity o) => o.AssignedToUserId == currentUser.Id);
		}
		return Ok(await (from o in query
			orderby o.CreatedAt descending
			select MapToDto(o)).ToListAsync());
	}

	[HttpGet("{id:guid}")]
	public async Task<ActionResult<OpportunityDto>> GetOpportunity(Guid id)
	{
		User currentUser = await GetCurrentUserAsync();
		if (currentUser == null)
		{
			return Unauthorized();
		}
		Opportunity opportunity = await _context.Opportunities.FirstOrDefaultAsync((Opportunity o) => o.Id == id);
		if (opportunity == null)
		{
			return NotFound();
		}
		if (IsEmployee(currentUser.Role) && opportunity.AssignedToUserId != currentUser.Id)
		{
			return Forbid();
		}
		return Ok(MapToDto(opportunity));
	}

	[HttpPost]
	public async Task<ActionResult<OpportunityDto>> CreateOpportunity([FromBody] CreateOpportunityDto request)
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
		if (IsEmployee(currentUser.Role) && request.AssignedToUserId != currentUser.Id)
		{
			return Forbid();
		}
		if (!Enum.TryParse<OpportunityStatus>(request.Status, ignoreCase: true, out var status))
		{
			return BadRequest(new
			{
				success = false,
				message = "Invalid status."
			});
		}
		Opportunity opportunity = new Opportunity
		{
			Id = Guid.NewGuid(),
			Title = request.Title.Trim(),
			ClientId = request.ClientId,
			Value = request.Value,
			Status = status,
			AssignedToUserId = request.AssignedToUserId,
			Description = request.Description.Trim(),
			CreatedAt = DateTime.UtcNow,
			UpdatedAt = DateTime.UtcNow
		};
		_context.Opportunities.Add(opportunity);
		await _context.SaveChangesAsync();
		return CreatedAtAction("GetOpportunity", new
		{
			id = opportunity.Id
		}, MapToDto(opportunity));
	}

	[HttpPut("{id:guid}")]
	public async Task<ActionResult<OpportunityDto>> UpdateOpportunity(Guid id, [FromBody] UpdateOpportunityDto request)
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
		Opportunity opportunity = await _context.Opportunities.FirstOrDefaultAsync((Opportunity o) => o.Id == id);
		if (opportunity == null)
		{
			return NotFound();
		}
		if (IsEmployee(currentUser.Role) && opportunity.AssignedToUserId != currentUser.Id)
		{
			return Forbid();
		}
		opportunity.Title = request.Title.Trim();
		opportunity.ClientId = request.ClientId;
		opportunity.Value = request.Value;
		opportunity.AssignedToUserId = request.AssignedToUserId;
		opportunity.Description = request.Description.Trim();
		opportunity.UpdatedAt = DateTime.UtcNow;
		await _context.SaveChangesAsync();
		return Ok(MapToDto(opportunity));
	}

	[HttpPut("{id:guid}/status")]
	public async Task<ActionResult<OpportunityDto>> UpdateStatus(Guid id, [FromBody] UpdateOpportunityStatusDto request)
	{
		User currentUser = await GetCurrentUserAsync();
		if (currentUser == null)
		{
			return Unauthorized();
		}
		Opportunity opportunity = await _context.Opportunities.FirstOrDefaultAsync((Opportunity o) => o.Id == id);
		if (opportunity == null)
		{
			return NotFound();
		}
		if (IsEmployee(currentUser.Role) && opportunity.AssignedToUserId != currentUser.Id)
		{
			return Forbid();
		}
		if (!Enum.TryParse<OpportunityStatus>(request.Status, ignoreCase: true, out var status))
		{
			return BadRequest(new
			{
				success = false,
				message = "Invalid status."
			});
		}
		opportunity.Status = status;
		opportunity.UpdatedAt = DateTime.UtcNow;
		await _context.SaveChangesAsync();
		return Ok(MapToDto(opportunity));
	}

	[HttpDelete("{id:guid}")]
	public async Task<ActionResult> DeleteOpportunity(Guid id)
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
		Opportunity opportunity = await _context.Opportunities.FirstOrDefaultAsync((Opportunity o) => o.Id == id);
		if (opportunity == null)
		{
			return NotFound();
		}
		_context.Opportunities.Remove(opportunity);
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

	private static OpportunityDto MapToDto(Opportunity opportunity)
	{
		return new OpportunityDto
		{
			Id = opportunity.Id,
			Title = opportunity.Title,
			ClientId = opportunity.ClientId,
			Value = opportunity.Value,
			Status = opportunity.Status.ToString(),
			AssignedToUserId = opportunity.AssignedToUserId,
			Description = opportunity.Description,
			CreatedAt = opportunity.CreatedAt,
			UpdatedAt = opportunity.UpdatedAt
		};
	}
}
