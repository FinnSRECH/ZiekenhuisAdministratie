namespace Hospital.Domain.Models;

public class Patient
{
	public int Id { get; set; }

	public string FirstName { get; set; } = string.Empty;

	public string LastName { get; set; } = string.Empty;

	public DateOnly DateOfBirth { get; set; }

	public string Email { get; set; } = string.Empty;

	public string PhoneNumber { get; set; } = string.Empty;

	public string Address { get; set; } = string.Empty;

	public string FullName => $"{FirstName} {LastName}";
}