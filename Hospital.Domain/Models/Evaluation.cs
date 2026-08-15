namespace Hospital.Domain.Models;

public class Evaluation
{
	public int Id { get; set; }

	public int PatientId { get; set; }

	public int TreatmentId { get; set; }

	public int StaffMemberId { get; set; }

	public DateTime CreatedAt { get; set; }

	public string Title { get; set; } = string.Empty;

	public string Description { get; set; } = string.Empty;
}