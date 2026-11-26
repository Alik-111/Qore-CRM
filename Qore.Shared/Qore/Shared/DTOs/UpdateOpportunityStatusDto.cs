using System.ComponentModel.DataAnnotations;

namespace Qore.Shared.DTOs;

public class UpdateOpportunityStatusDto
{
	[Required]
	public string Status { get; set; } = string.Empty;
}
