using System;

namespace Qore.Data.Entities;

public class UserSession
{
	public Guid Id { get; set; }

	public Guid UserId { get; set; }

	public string Jti { get; set; } = string.Empty;

	public DateTime CreatedAt { get; set; }

	public DateTime ExpiresAt { get; set; }

	public bool IsRevoked { get; set; }

	public User? User { get; set; }
}
