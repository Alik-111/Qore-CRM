using System.ComponentModel.DataAnnotations;

namespace Qore.Shared.DTOs;

public class UpdateUserDto
{
	[Required]
	[StringLength(100)]
	[RegularExpression("^[a-zA-Z0-9._-]+$", ErrorMessage = "Username must contain only letters, numbers, dots, underscores, or hyphens.")]
	public string Username { get; set; } = string.Empty;

	[Required]
	[StringLength(200)]
	public string FullName { get; set; } = string.Empty;

	[Required]
	[EmailAddress]
	public string Email { get; set; } = string.Empty;

	[Required]
	public string Role { get; set; } = "Employee";
}
