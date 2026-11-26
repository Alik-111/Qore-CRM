using System.ComponentModel.DataAnnotations;

namespace Qore.Shared.DTOs;

public class UpdateTaskStatusDto
{
	[Required]
	public string Status { get; set; } = string.Empty;
}
