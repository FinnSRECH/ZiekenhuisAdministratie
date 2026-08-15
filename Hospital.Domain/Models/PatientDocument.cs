namespace Hospital.Domain.Models;

public class PatientDocument
{
	public int Id { get; set; }

	public int PatientId { get; set; }

	public string FileName { get; set; } = string.Empty;

	public string ContentType { get; set; } = string.Empty;

	public byte[] Content { get; set; } = Array.Empty<byte>();

	public DateTime UploadedAt { get; set; }

	public int UploadedByUserId { get; set; }

	public string UploadedByUserName { get; set; } = string.Empty;
}