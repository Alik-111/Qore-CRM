using System;
using Qore.Shared.Enums;

namespace Qore.Data.Entities;

public class TaskItem
{
	public Guid Id { get; set; }

	public string Title { get; set; } = string.Empty;

	public string Description { get; set; } = string.Empty;

	public Guid AssignedToUserId { get; set; }

	public Guid CreatedByUserId { get; set; }

	public TaskItemStatus Status { get; set; }

	public TaskPriority Priority { get; set; } = TaskPriority.Medium;

	public DateTime? DueDate { get; set; }

	public DateTime CreatedAt { get; set; }

	public DateTime UpdatedAt { get; set; }

	public User AssignedToUser { get; set; }

	public User CreatedByUser { get; set; }
}
