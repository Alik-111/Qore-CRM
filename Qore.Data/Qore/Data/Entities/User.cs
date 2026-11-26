using System;
using System.Collections.Generic;
using Qore.Shared.Enums;

namespace Qore.Data.Entities;

public class User
{
	public Guid Id { get; set; }

	public string Username { get; set; } = string.Empty;

	public string PasswordHash { get; set; } = string.Empty;

	public string PasswordSalt { get; set; } = string.Empty;

	public string FullName { get; set; } = string.Empty;

	public string Email { get; set; } = string.Empty;

	public UserRole Role { get; set; } = UserRole.Employee;

	public bool IsActive { get; set; } = true;

	public DateTime CreatedAt { get; set; }

	public DateTime? LastLoginAt { get; set; }

	public List<UserSession> Sessions { get; set; } = new List<UserSession>();
}
