using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Qore.Data;
using Qore.Data.Entities;
using Qore.Shared.Enums;

namespace Qore.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DashboardController : ControllerBase
{
	private readonly QoreDbContext _context;

	public DashboardController(QoreDbContext context)
	{
		_context = context;
	}

	[HttpGet("summary")]
	public async Task<IActionResult> GetSummary()
	{
		User currentUser = await GetCurrentUserAsync();
		if (currentUser == null)
		{
			return Unauthorized();
		}
		bool isEmployee = currentUser.Role == UserRole.Employee;
		IQueryable<Client> clientsQuery = _context.Clients.AsQueryable();
		IQueryable<Opportunity> opportunitiesQuery = _context.Opportunities.AsQueryable();
		IQueryable<TaskItem> tasksQuery = _context.TaskItems.AsQueryable();
		IQueryable<Document> documentsQuery = _context.Documents.AsQueryable();
		if (isEmployee)
		{
			clientsQuery = clientsQuery.Where((Client c) => c.AssignedToUserId == null || c.AssignedToUserId == currentUser.Id);
			opportunitiesQuery = opportunitiesQuery.Where((Opportunity o) => o.AssignedToUserId == currentUser.Id);
			tasksQuery = tasksQuery.Where((TaskItem t) => t.AssignedToUserId == currentUser.Id);
			documentsQuery = documentsQuery.Where((Document d) => d.VisibleToRole == (DocumentVisibility?)null || (int?)d.VisibleToRole == (int?)MapRole(currentUser.Role));
		}
		int totalClients = await clientsQuery.CountAsync();
		int totalOpportunities = await opportunitiesQuery.CountAsync();
		int openOpportunities = await opportunitiesQuery.Where((Opportunity o) => (int)o.Status != 4 && (int)o.Status != 5).CountAsync();
		decimal openPipelineValue = (await opportunitiesQuery.Where((Opportunity o) => (int)o.Status != 4 && (int)o.Status != 5).SumAsync((Expression<Func<Opportunity, decimal?>>)((Opportunity o) => o.Value), default(CancellationToken))).GetValueOrDefault();
		decimal wonPipelineValue = (await opportunitiesQuery.Where((Opportunity o) => (int)o.Status == 4).SumAsync((Expression<Func<Opportunity, decimal?>>)((Opportunity o) => o.Value), default(CancellationToken))).GetValueOrDefault();
		var taskStatusBreakdown = await (from t in tasksQuery
			group t by t.Status into g
			select new
			{
				Status = g.Key.ToString(),
				Count = g.Count()
			}).ToListAsync();
		Dictionary<string, int> taskCounts = new Dictionary<string, int>
		{
			["New"] = 0,
			["In Progress"] = 0,
			["Completed"] = 0
		};
		foreach (var item in taskStatusBreakdown)
		{
			if (item.Status == "New")
			{
				taskCounts["New"] = item.Count;
			}
			else if (item.Status == "InProgress")
			{
				taskCounts["In Progress"] = item.Count;
			}
			else if (item.Status == "Completed")
			{
				taskCounts["Completed"] = item.Count;
			}
		}
		List<object> recentItems = new List<object>();
		var recentClients = await (from c in clientsQuery.OrderByDescending((Client c) => c.UpdatedAt).Take(5)
			select new
			{
				type = "Client",
				title = c.Name,
				createdAt = c.UpdatedAt,
				status = c.CompanyName
			}).ToListAsync();
		var recentOpportunities = await (from o in opportunitiesQuery.OrderByDescending((Opportunity o) => o.UpdatedAt).Take(5)
			select new
			{
				type = "Opportunity",
				title = o.Title,
				createdAt = o.UpdatedAt,
				status = o.Status.ToString()
			}).ToListAsync();
		var recentTasks = await (from t in tasksQuery.OrderByDescending((TaskItem t) => t.UpdatedAt).Take(5)
			select new
			{
				type = "Task",
				title = t.Title,
				createdAt = t.UpdatedAt,
				status = t.Status.ToString()
			}).ToListAsync();
		recentItems.AddRange(recentClients);
		recentItems.AddRange(recentOpportunities);
		recentItems.AddRange(recentTasks);
		List<object> recentActivity = recentItems.OrderByDescending((object x) => (DateTime?)(x.GetType().GetProperty("createdAt")?.GetValue(x, null) ?? ((object)DateTime.MinValue))).Take(8).ToList();
		var myTasks = await (from t in (from t in tasksQuery
				where t.AssignedToUserId == currentUser.Id && (int)t.Status != 2
				orderby t.DueDate ?? DateTime.MaxValue
				select t).Take(5)
			select new
			{
				id = t.Id,
				title = t.Title,
				status = t.Status.ToString(),
				dueDate = t.DueDate,
				priority = t.Priority.ToString()
			}).ToListAsync();
		int totalDocuments = await documentsQuery.CountAsync();
		int activeTasks = await tasksQuery.Where((TaskItem t) => (int)t.Status != 2).CountAsync();
		int overdueTasks = await tasksQuery.Where((TaskItem t) => t.AssignedToUserId == currentUser.Id && (int)t.Status != 2 && t.DueDate != null && t.DueDate < DateTime.UtcNow).CountAsync();
		int upcomingTasks = await tasksQuery.Where((TaskItem t) => t.AssignedToUserId == currentUser.Id && (int)t.Status != 2 && t.DueDate != null && t.DueDate >= DateTime.UtcNow && t.DueDate <= DateTime.UtcNow.AddDays(7.0)).CountAsync();
		string role = currentUser.Role.ToString();
		int clients = totalClients;
		int opportunities = totalOpportunities;
		int openOpportunities2 = openOpportunities;
		decimal pipelineValue = openPipelineValue;
		decimal wonPipelineValue2 = wonPipelineValue;
		int documents = totalDocuments;
		return Ok(new
		{
			success = true,
			role = role,
			totals = new
			{
				clients = clients,
				opportunities = opportunities,
				openOpportunities = openOpportunities2,
				pipelineValue = pipelineValue,
				wonPipelineValue = wonPipelineValue2,
				documents = documents,
				tasks = await tasksQuery.CountAsync()
			},
			taskBreakdown = taskCounts,
			recentActivity = recentActivity,
			myTasks = myTasks,
			focus = new { overdueTasks, upcomingTasks, activeTasks }
		});
	}

	[HttpGet("notifications")]
	public async Task<IActionResult> GetNotifications()
	{
		User currentUser = await GetCurrentUserAsync();
		if (currentUser == null)
		{
			return Unauthorized();
		}
		bool isEmployee = currentUser.Role == UserRole.Employee;
		IQueryable<TaskItem> tasksQuery = _context.TaskItems.AsQueryable();
		IQueryable<Opportunity> opportunitiesQuery = _context.Opportunities.AsQueryable();
		IQueryable<Document> documentsQuery = _context.Documents.AsQueryable();
		if (isEmployee)
		{
			tasksQuery = tasksQuery.Where((TaskItem t) => t.AssignedToUserId == currentUser.Id);
			opportunitiesQuery = opportunitiesQuery.Where((Opportunity o) => o.AssignedToUserId == currentUser.Id);
			documentsQuery = documentsQuery.Where((Document d) => d.VisibleToRole == (DocumentVisibility?)null || (int?)d.VisibleToRole == (int?)MapRole(currentUser.Role));
		}
		List<object> notifications = new List<object>();
		DateTime now = DateTime.UtcNow;
		int overdueAssignments = await tasksQuery.Where((TaskItem t) => (int)t.Status != 2 && t.DueDate != null && t.DueDate < now).CountAsync();
		if (overdueAssignments > 0)
		{
			notifications.Add(new
			{
				id = Guid.NewGuid().ToString(),
				title = "Overdue tasks",
				message = $"You have {overdueAssignments} overdue task(s) that need attention.",
				kind = "task",
				priority = "high",
				createdAt = now
			});
		}
		int dueSoonAssignments = await tasksQuery.Where((TaskItem t) => (int)t.Status != 2 && t.DueDate != null && t.DueDate >= now && t.DueDate <= ((DateTime)now).AddDays(7.0)).CountAsync();
		if (dueSoonAssignments > 0)
		{
			notifications.Add(new
			{
				id = Guid.NewGuid().ToString(),
				title = "Upcoming deadlines",
				message = $"{dueSoonAssignments} task(s) are due in the next 7 days.",
				kind = "task",
				priority = "medium",
				createdAt = now.AddMinutes(-10.0)
			});
		}
		int recentAssignedTasks = await _context.TaskItems.Where((TaskItem t) => t.AssignedToUserId == currentUser.Id && (int)t.Status != 2 && t.CreatedAt >= ((DateTime)now).AddDays(-7.0)).CountAsync();
		if (recentAssignedTasks > 0)
		{
			notifications.Add(new
			{
				id = Guid.NewGuid().ToString(),
				title = "New tasks assigned",
				message = $"{recentAssignedTasks} new task(s) were assigned to you recently.",
				kind = "task",
				priority = "medium",
				createdAt = now.AddMinutes(-5.0)
			});
		}
		int openOpportunities = await opportunitiesQuery.Where((Opportunity o) => (int)o.Status != 4 && (int)o.Status != 5).CountAsync();
		if (openOpportunities > 0)
		{
			notifications.Add(new
			{
				id = Guid.NewGuid().ToString(),
				title = "Open pipeline",
				message = $"{openOpportunities} active opportunity(ies) are still progressing.",
				kind = "pipeline",
				priority = "medium",
				createdAt = now.AddMinutes(-20.0)
			});
		}
		int availableDocuments = await documentsQuery.CountAsync();
		if (availableDocuments > 0)
		{
			notifications.Add(new
			{
				id = Guid.NewGuid().ToString(),
				title = "Shared documents",
				message = $"{availableDocuments} document(s) are available to you right now.",
				kind = "document",
				priority = "low",
				createdAt = now.AddHours(-1.0)
			});
		}
		if (notifications.Count == 0)
		{
			notifications.Add(new
			{
				id = Guid.NewGuid().ToString(),
				title = "You are all caught up",
				message = "No urgent follow-ups are waiting for you right now.",
				kind = "info",
				priority = "low",
				createdAt = now
			});
		}
		return Ok(new
		{
			success = true,
			notifications = notifications.OrderByDescending((object n) => (DateTime)((dynamic)n).createdAt).Take(6).ToList()
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

	private static DocumentVisibility MapRole(UserRole role)
	{
		return role switch
		{
			UserRole.Admin => DocumentVisibility.Admin, 
			UserRole.Manager => DocumentVisibility.Manager, 
			_ => DocumentVisibility.Employee, 
		};
	}
}
