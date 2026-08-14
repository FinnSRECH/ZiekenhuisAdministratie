using Hospital.Domain.Enums;

namespace Hospital.Domain.Models;

public class Consultation
{
	public int Id { get; set; }

	public int PatientId { get; set; }

	public int TreatmentId { get; set; }

	public DateTime StartTime { get; set; }

	public string Reason { get; set; } = string.Empty;

	public int SurgeonId { get; set; }

	public string Room { get; set; } = string.Empty;

	public AppointmentStatus Status { get; set; }

	public string MedicalReport { get; set; } = string.Empty;
}