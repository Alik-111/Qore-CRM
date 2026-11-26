using System;
using System.ComponentModel.DataAnnotations;

namespace Qore.Shared.DTOs;

public class CreateOpportunityDto
{
	[Required]
	[StringLength(200)]
	public string Title { get; set; } = string.Empty;

	public Guid? ClientId { get; set; }

	[Range(typeof(decimal), "1", "79228162514264337593543950335")]
	public decimal Value { get; set; }

	public string Status { get; set; } = "New";

	[Required]
	public Guid AssignedToUserId { get; set; }

	[StringLength(5000)]
	public string Description { get; set; } = string.Empty;
}
