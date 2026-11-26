using System;
using Qore.Shared.Enums;

namespace Qore.Data.Entities;

public class Opportunity
{
	public Guid Id { get; set; }

	public string Title { get; set; } = string.Empty;

	public Guid? ClientId { get; set; }

	public decimal Value { get; set; }

	public OpportunityStatus Status { get; set; }

	public Guid AssignedToUserId { get; set; }

	public string Description { get; set; } = string.Empty;

	public DateTime CreatedAt { get; set; }

	public DateTime UpdatedAt { get; set; }

	public Client? Client { get; set; }

	public User AssignedToUser { get; set; }
}
