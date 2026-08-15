using Hospital.Domain.Enums;

namespace Hospital.Domain.Models;

public class StaffMember
{
	public int Id { get; set; }

	public string FirstName { get; set; } = string.Empty;

	public string LastName { get; set; } = string.Empty;

	public string Email { get; set; } = string.Empty;

	public string PasswordHash { get; set; } = string.Empty;

	public UserRole Role { get; set; }

	public bool IsActive { get; set; } = true;

	public string FullName => $"{FirstName} {LastName}";
}