using System;

namespace Qore.Shared.DTOs;

public class OpportunityDto
{
	public Guid Id { get; set; }

	public string Title { get; set; } = string.Empty;

	public Guid? ClientId { get; set; }

	public decimal Value { get; set; }

	public string Status { get; set; } = string.Empty;

	public Guid AssignedToUserId { get; set; }

	public string Description { get; set; } = string.Empty;

	public DateTime CreatedAt { get; set; }

	public DateTime UpdatedAt { get; set; }
}
