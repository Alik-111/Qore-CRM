using System;

namespace Qore.Data.Entities;

public class Theme
{
	public Guid Id { get; set; }

	public string Name { get; set; } = string.Empty;

	public string PrimaryColor { get; set; } = string.Empty;

	public string SecondaryColor { get; set; } = string.Empty;

	public string AccentColor { get; set; } = string.Empty;

	public string BackgroundColor { get; set; } = string.Empty;

	public string TextColor { get; set; } = string.Empty;

	public bool IsActive { get; set; }

	public bool IsDefault { get; set; }

	public Guid CreatedByUserId { get; set; }

	public DateTime CreatedAt { get; set; }

	public User CreatedByUser { get; set; }
}
