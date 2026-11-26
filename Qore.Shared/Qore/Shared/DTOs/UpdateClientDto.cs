using System;
using System.ComponentModel.DataAnnotations;

namespace Qore.Shared.DTOs;

public class UpdateClientDto
{
	[Required]
	[StringLength(200)]
	public string Name { get; set; } = string.Empty;

	[StringLength(200)]
	public string CompanyName { get; set; } = string.Empty;

	[EmailAddress]
	public string Email { get; set; } = string.Empty;

	[StringLength(50)]
	public string Phone { get; set; } = string.Empty;

	[StringLength(500)]
	public string Address { get; set; } = string.Empty;

	[StringLength(5000)]
	public string Notes { get; set; } = string.Empty;

	public Guid? AssignedToUserId { get; set; }
}
