namespace Hospital.Domain.Models;

public class AuditLog
{
	public int Id { get; set; }

	public int UserId { get; set; }

	public string UserName { get; set; } = string.Empty;

	public int PatientId { get; set; }

	public string PatientName { get; set; } = string.Empty;

	public string Action { get; set; } = string.Empty;

	public string Resource { get; set; } = string.Empty;

	public DateTime OpenedAt { get; set; }

	public DateTime? ClosedAt { get; set; }

	public TimeSpan? Duration
	{
		get
		{
			if (ClosedAt is null)
			{
				return null;
			}

			return ClosedAt.Value - OpenedAt;
		}
	}
}