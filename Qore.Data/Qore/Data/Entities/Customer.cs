using System;
using Qore.Shared.Enums;

namespace Qore.Data.Entities;

public class Customer
{
	public int Id { get; set; }

	public string Name { get; set; } = string.Empty;

	public string Email { get; set; } = string.Empty;

	public string Phone { get; set; } = string.Empty;

	public string Company { get; set; } = string.Empty;

	public CustomerStatus Status { get; set; } = CustomerStatus.Active;

	public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
