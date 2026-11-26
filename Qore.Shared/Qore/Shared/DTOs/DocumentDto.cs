using System;

namespace Qore.Shared.DTOs;

public class DocumentDto
{
	public Guid Id { get; set; }

	public string Name { get; set; } = string.Empty;

	public string Description { get; set; } = string.Empty;

	public string FileName { get; set; } = string.Empty;

	public string ContentType { get; set; } = string.Empty;

	public Guid UploadedByUserId { get; set; }

	public string? VisibleToRole { get; set; }

	public DateTime CreatedAt { get; set; }
}
