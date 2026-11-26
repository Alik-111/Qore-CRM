using System.ComponentModel.DataAnnotations;

namespace Qore.Shared.DTOs;

public class UpdateThemeDto
{
	[Required]
	[StringLength(200)]
	public string Name { get; set; } = string.Empty;

	[Required]
	[StringLength(50)]
	public string PrimaryColor { get; set; } = string.Empty;

	[Required]
	[StringLength(50)]
	public string SecondaryColor { get; set; } = string.Empty;

	[Required]
	[StringLength(50)]
	public string AccentColor { get; set; } = string.Empty;

	[Required]
	[StringLength(50)]
	public string BackgroundColor { get; set; } = string.Empty;

	[Required]
	[StringLength(50)]
	public string TextColor { get; set; } = string.Empty;
}
