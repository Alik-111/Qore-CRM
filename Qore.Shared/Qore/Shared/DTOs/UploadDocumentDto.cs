using System.ComponentModel.DataAnnotations;

namespace Qore.Shared.DTOs;

public class UploadDocumentDto
{
	[Required]
	[StringLength(200)]
	public string Name { get; set; } = string.Empty;

	public string Description { get; set; } = string.Empty;

	[Required]
	public string FileName { get; set; } = string.Empty;

	public string ContentType { get; set; } = string.Empty;

	public string? VisibleToRole { get; set; }
}
