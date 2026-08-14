using Hospital.Domain.Enums;

namespace Hospital.Domain.Models;

public class Treatment
{
	public int Id { get; set; }

	public int PatientId { get; set; }

	public string Name { get; set; } = string.Empty;

	public string Description { get; set; } = string.Empty;

	public DateTime StartDate { get; set; }

	public DateTime? EndDate { get; set; }

	public TreatmentStatus Status { get; set; }

	public int? SecretaryId { get; set; }

	public List<int> SurgeonIds { get; set; } = new();

	public List<int> NurseIds { get; set; } = new();
}