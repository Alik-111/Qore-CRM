using System;
using Qore.Shared.Enums;

namespace Qore.Data.Entities;

public class Document
{
	public Guid Id { get; set; }

	public string Name { get; set; } = string.Empty;

	public string Description { get; set; } = string.Empty;

	public byte[] FileData { get; set; } = Array.Empty<byte>();

	public string FileName { get; set; } = string.Empty;

	public string ContentType { get; set; } = string.Empty;

	public Guid UploadedByUserId { get; set; }

	public DocumentVisibility? VisibleToRole { get; set; }

	public DateTime CreatedAt { get; set; }

	public User UploadedByUser { get; set; }
}
