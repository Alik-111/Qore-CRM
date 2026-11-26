using System;
using System.ComponentModel.DataAnnotations;

namespace Qore.Shared.DTOs;

public class CreateTaskDto
{
	[Required]
	[StringLength(200)]
	public string Title { get; set; } = string.Empty;

	[StringLength(5000)]
	public string Description { get; set; } = string.Empty;

	[Required]
	public Guid AssignedToUserId { get; set; }

	public string Status { get; set; } = "New";

	public string Priority { get; set; } = "Medium";

	public DateTime? DueDate { get; set; }
}
