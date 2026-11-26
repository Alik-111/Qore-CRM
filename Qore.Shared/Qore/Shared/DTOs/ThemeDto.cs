using System;

namespace Qore.Shared.DTOs;

public class ThemeDto
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

	public DateTime CreatedAt { get; set; }
}
