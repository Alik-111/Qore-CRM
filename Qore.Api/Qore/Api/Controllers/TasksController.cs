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
public class TasksController : ControllerBase
{
	private readonly QoreDbContext _context;

	public TasksController(QoreDbContext context)
	{
		_context = context;
	}

	[HttpGet]
	public async Task<ActionResult<IEnumerable<TaskDto>>> GetTasks()
	{
		User currentUser = await GetCurrentUserAsync();
		if (currentUser == null)
		{
			return Unauthorized();
		}
		IQueryable<TaskItem> query = _context.TaskItems.AsQueryable();
		if (IsEmployee(currentUser.Role))
		{
			query = query.Where((TaskItem t) => t.AssignedToUserId == currentUser.Id);
		}
		return Ok(await (from t in query
			orderby t.CreatedAt descending
			select MapToDto(t)).ToListAsync());
	}

	[HttpPost]
	public async Task<ActionResult<TaskDto>> CreateTask([FromBody] CreateTaskDto request)
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
		if (!Enum.TryParse<TaskItemStatus>(request.Status, ignoreCase: true, out var status))
		{
			return BadRequest(new
			{
				success = false,
				message = "Invalid status."
			});
		}
		if (!Enum.TryParse<TaskPriority>(request.Priority, ignoreCase: true, out var priority))
		{
			return BadRequest(new
			{
				success = false,
				message = "Invalid priority."
			});
		}
		TaskItem task = new TaskItem
		{
			Id = Guid.NewGuid(),
			Title = request.Title.Trim(),
			Description = request.Description.Trim(),
			AssignedToUserId = request.AssignedToUserId,
			CreatedByUserId = currentUser.Id,
			Status = status,
			Priority = priority,
			DueDate = request.DueDate,
			CreatedAt = DateTime.UtcNow,
			UpdatedAt = DateTime.UtcNow
		};
		_context.TaskItems.Add(task);
		await _context.SaveChangesAsync();
		return CreatedAtAction("GetTasks", new
		{
			id = task.Id
		}, MapToDto(task));
	}

	[HttpPut("{id:guid}/status")]
	public async Task<ActionResult<TaskDto>> UpdateStatus(Guid id, [FromBody] UpdateTaskStatusDto request)
	{
		User currentUser = await GetCurrentUserAsync();
		if (currentUser == null)
		{
			return Unauthorized();
		}
		TaskItem task = await _context.TaskItems.FirstOrDefaultAsync((TaskItem t) => t.Id == id);
		if (task == null)
		{
			return NotFound();
		}
		if (IsEmployee(currentUser.Role) && task.AssignedToUserId != currentUser.Id)
		{
			return Forbid();
		}
		if (!Enum.TryParse<TaskItemStatus>(request.Status, ignoreCase: true, out var status))
		{
			return BadRequest(new
			{
				success = false,
				message = "Invalid status."
			});
		}
		task.Status = status;
		task.UpdatedAt = DateTime.UtcNow;
		await _context.SaveChangesAsync();
		return Ok(MapToDto(task));
	}

	[HttpPut("{id:guid}")]
	public async Task<ActionResult<TaskDto>> UpdateTask(Guid id, [FromBody] UpdateTaskDto request)
	{
		User currentUser = await GetCurrentUserAsync();
		if (currentUser == null)
		{
			return Unauthorized();
		}
		TaskItem task = await _context.TaskItems.FirstOrDefaultAsync((TaskItem t) => t.Id == id);
		if (task == null)
		{
			return NotFound();
		}
		if (!IsManagerOrAdmin(currentUser.Role) && task.CreatedByUserId != currentUser.Id)
		{
			return Forbid();
		}
		if (!Enum.TryParse<TaskItemStatus>(request.Status, ignoreCase: true, out var status))
		{
			return BadRequest(new
			{
				success = false,
				message = "Invalid status."
			});
		}
		if (!Enum.TryParse<TaskPriority>(request.Priority, ignoreCase: true, out var priority))
		{
			return BadRequest(new
			{
				success = false,
				message = "Invalid priority."
			});
		}
		task.Title = request.Title.Trim();
		task.Description = request.Description.Trim();
		task.AssignedToUserId = request.AssignedToUserId;
		task.Status = status;
		task.Priority = priority;
		task.DueDate = request.DueDate;
		task.UpdatedAt = DateTime.UtcNow;
		await _context.SaveChangesAsync();
		return Ok(MapToDto(task));
	}

	[HttpDelete("{id:guid}")]
	public async Task<ActionResult> DeleteTask(Guid id)
	{
		User currentUser = await GetCurrentUserAsync();
		if (currentUser == null)
		{
			return Unauthorized();
		}
		if (!IsManagerOrAdmin(currentUser.Role))
		{
			return Forbid();
		}
		TaskItem task = await _context.TaskItems.FirstOrDefaultAsync((TaskItem t) => t.Id == id);
		if (task == null)
		{
			return NotFound();
		}
		_context.TaskItems.Remove(task);
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

	private static bool IsManagerOrAdmin(UserRole role)
	{
		if (role != UserRole.Manager)
		{
			return role == UserRole.Admin;
		}
		return true;
	}

	private static TaskDto MapToDto(TaskItem task)
	{
		return new TaskDto
		{
			Id = task.Id,
			Title = task.Title,
			Description = task.Description,
			AssignedToUserId = task.AssignedToUserId,
			CreatedByUserId = task.CreatedByUserId,
			Status = task.Status.ToString(),
			Priority = task.Priority.ToString(),
			DueDate = task.DueDate,
			CreatedAt = task.CreatedAt,
			UpdatedAt = task.UpdatedAt
		};
	}
}
