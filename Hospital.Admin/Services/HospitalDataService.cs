using Hospital.Domain.Enums;
using Hospital.Domain.Models;

namespace Hospital.Admin.Services;

public class HospitalDataService
{
	private readonly PasswordService _passwordService;

	private readonly List<Patient> _patients = new()
	{
		new Patient
		{
			Id = 1,
			FirstName = "Jan",
			LastName = "de Vries",
			DateOfBirth = new DateOnly(1985, 4, 12),
			Email = "jan.devries@example.nl",
			PhoneNumber = "0612345678",
			Address = "Kerkstraat 12, Amsterdam"
		},

		new Patient
		{
			Id = 2,
			FirstName = "Sophie",
			LastName = "Jansen",
			DateOfBirth = new DateOnly(1992, 9, 23),
			Email = "sophie.jansen@example.nl",
			PhoneNumber = "0687654321",
			Address = "Dorpsstraat 8, Utrecht"
		},

		new Patient
		{
			Id = 3,
			FirstName = "Mohammed",
			LastName = "El Amrani",
			DateOfBirth = new DateOnly(1978, 2, 5),
			Email = "m.elamrani@example.nl",
			PhoneNumber = "0611223344",
			Address = "Stationsweg 44, Rotterdam"
		}
	};

	private readonly List<StaffMember> _staffMembers = new()
	{
		new StaffMember
		{
			Id = 1,
			FirstName = "Emma",
			LastName = "Jansen",
			Email = "emma.jansen@hospital.nl",
			Role = UserRole.Surgeon
		},

		new StaffMember
		{
			Id = 2,
			FirstName = "Lisa",
			LastName = "De Boer",
			Email = "lisa.deboer@hospital.nl",
			Role = UserRole.Nurse
		},

		new StaffMember
		{
			Id = 3,
			FirstName = "Mark",
			LastName = "Smit",
			Email = "mark.smit@hospital.nl",
			Role = UserRole.Secretary
		},

		new StaffMember
		{
			Id = 4,
			FirstName = "Anna",
			LastName = "Beheer",
			Email = "admin@hospital.nl",
			Role = UserRole.Administrator
		}
	};

	private readonly List<Treatment> _treatments = new()
	{
		new Treatment
		{
			Id = 1,
			PatientId = 1,
			Name = "Behandeling knieklachten",
			Description = "Onderzoek en behandeling van aanhoudende knieklachten.",
			StartDate = DateTime.Today.AddDays(-14),
			Status = TreatmentStatus.Active,
			SecretaryId = 3,
			SurgeonIds = new List<int> { 1 },
			NurseIds = new List<int> { 2 }
		}
	};

	private readonly List<Consultation> _consultations = new()
	{
		new Consultation
		{
			Id = 1,
			PatientId = 1,
			TreatmentId = 1,
			StartTime = DateTime.Today.AddDays(5).AddHours(10),
			Reason = "Controle knie",
			SurgeonId = 1,
			Room = "B2.14",
			Status = AppointmentStatus.Planned
		}
	};

	private readonly List<OperatingRoom> _operatingRooms = new()
	{
		new OperatingRoom
		{
			Id = 1,
			Name = "OK 1",
			Location = "Verdieping 2",
			IsAvailable = true
		},

		new OperatingRoom
		{
			Id = 2,
			Name = "OK 2",
			Location = "Verdieping 2",
			IsAvailable = true
		}
	};

	private readonly List<Operation> _operations = new();

	// Evaluaties van patiënten/behandelingen.
	private readonly List<Evaluation> _evaluations = new();

	// Documenten in patiëntendossiers.
	private readonly List<PatientDocument> _patientDocuments = new();

	private readonly List<AuditLog> _auditLogs = new();

	private int _auditLogId = 1;

	// -------------------------
	// CONSTRUCTOR
	// -------------------------

	public HospitalDataService(
		PasswordService passwordService)
	{
		_passwordService = passwordService;

		foreach (var staffMember in _staffMembers)
		{
			staffMember.PasswordHash =
				_passwordService.HashPassword(
					"Welkom123!");
		}
	}

	// -------------------------
	// PATIENTEN
	// -------------------------

	public IReadOnlyList<Patient> GetPatients()
	{
		return _patients;
	}

	public Patient? GetPatient(int id)
	{
		return _patients
			.FirstOrDefault(p => p.Id == id);
	}

	// -------------------------
	// PERSONEEL
	// -------------------------

	public IReadOnlyList<StaffMember> GetStaffMembers()
	{
		return _staffMembers
			.OrderBy(s => s.LastName)
			.ThenBy(s => s.FirstName)
			.ToList();
	}

	public StaffMember? GetStaffMember(int id)
	{
		return _staffMembers
			.FirstOrDefault(s => s.Id == id);
	}

	public StaffMember? GetStaffMemberByEmail(
		string email)
	{
		return _staffMembers
			.FirstOrDefault(s =>
				s.Email.Equals(
					email,
					StringComparison.OrdinalIgnoreCase));
	}

	public IReadOnlyList<StaffMember>
		GetStaffMembersByRole(UserRole role)
	{
		return _staffMembers
			.Where(s =>
				s.Role == role &&
				s.IsActive)
			.OrderBy(s => s.LastName)
			.ThenBy(s => s.FirstName)
			.ToList();
	}

	public bool StaffEmailExists(string email)
	{
		return _staffMembers.Any(s =>
			s.Email.Equals(
				email,
				StringComparison.OrdinalIgnoreCase));
	}

	public void AddStaffMember(
		StaffMember staffMember,
		string password)
	{
		staffMember.Id =
			_staffMembers.Count == 0
				? 1
				: _staffMembers.Max(s => s.Id) + 1;

		staffMember.Email =
			staffMember.Email.Trim();

		staffMember.PasswordHash =
			_passwordService.HashPassword(
				password);

		staffMember.IsActive = true;

		_staffMembers.Add(staffMember);
	}

	public bool DeactivateStaffMember(int id)
	{
		var staffMember =
			GetStaffMember(id);

		if (staffMember is null)
		{
			return false;
		}

		staffMember.IsActive = false;

		return true;
	}

	// -------------------------
	// BEHANDELINGEN
	// -------------------------

	public IReadOnlyList<Treatment> GetTreatments(
		int patientId)
	{
		return _treatments
			.Where(t =>
				t.PatientId == patientId)
			.OrderByDescending(t =>
				t.StartDate)
			.ToList();
	}

	public Treatment? GetActiveTreatment(
		int patientId)
	{
		return _treatments
			.FirstOrDefault(t =>
				t.PatientId == patientId &&
				t.Status == TreatmentStatus.Active);
	}

	public Treatment? GetTreatment(
		int treatmentId)
	{
		return _treatments
			.FirstOrDefault(t =>
				t.Id == treatmentId);
	}

	public void AddTreatment(
		Treatment treatment)
	{
		treatment.Id =
			_treatments.Count == 0
				? 1
				: _treatments.Max(t => t.Id) + 1;

		_treatments.Add(treatment);
	}

	public bool CompleteTreatment(
		int treatmentId)
	{
		var treatment =
			GetTreatment(treatmentId);

		if (treatment is null)
		{
			return false;
		}

		if (treatment.Status !=
			TreatmentStatus.Active)
		{
			return false;
		}

		treatment.Status =
			TreatmentStatus.Completed;

		return true;
	}

	// -------------------------
	// CONSULTATIES
	// -------------------------

	public IReadOnlyList<Consultation>
		GetConsultations(int patientId)
	{
		return _consultations
			.Where(c =>
				c.PatientId == patientId)
			.OrderBy(c =>
				c.StartTime)
			.ToList();
	}

	public Consultation? GetConsultation(
		int consultationId)
	{
		return _consultations
			.FirstOrDefault(c =>
				c.Id == consultationId);
	}

	public void AddConsultation(
		Consultation consultation)
	{
		consultation.Id =
			_consultations.Count == 0
				? 1
				: _consultations.Max(c => c.Id) + 1;

		_consultations.Add(consultation);
	}

	public bool UpdateConsultation(
		Consultation consultation)
	{
		var existingConsultation =
			GetConsultation(consultation.Id);

		if (existingConsultation is null)
		{
			return false;
		}

		existingConsultation.TreatmentId =
			consultation.TreatmentId;

		existingConsultation.StartTime =
			consultation.StartTime;

		existingConsultation.Reason =
			consultation.Reason.Trim();

		existingConsultation.SurgeonId =
			consultation.SurgeonId;

		existingConsultation.Room =
			consultation.Room.Trim();

		existingConsultation.Status =
			consultation.Status;

		return true;
	}

	// -------------------------
	// OPERATIEKAMERS
	// -------------------------

	public IReadOnlyList<OperatingRoom>
		GetOperatingRooms()
	{
		return _operatingRooms
			.OrderBy(r => r.Name)
			.ToList();
	}

	public IReadOnlyList<OperatingRoom>
		GetAvailableOperatingRooms()
	{
		return _operatingRooms
			.Where(r => r.IsAvailable)
			.OrderBy(r => r.Name)
			.ToList();
	}

	public OperatingRoom? GetOperatingRoom(
		int id)
	{
		return _operatingRooms
			.FirstOrDefault(r =>
				r.Id == id);
	}

	public bool OperatingRoomNameExists(
		string name)
	{
		return _operatingRooms.Any(r =>
			r.Name.Equals(
				name.Trim(),
				StringComparison.OrdinalIgnoreCase));
	}

	public void AddOperatingRoom(
		OperatingRoom operatingRoom)
	{
		operatingRoom.Id =
			_operatingRooms.Count == 0
				? 1
				: _operatingRooms.Max(r => r.Id) + 1;

		operatingRoom.Name =
			operatingRoom.Name.Trim();

		operatingRoom.Location =
			operatingRoom.Location.Trim();

		operatingRoom.IsAvailable = true;

		_operatingRooms.Add(
			operatingRoom);
	}

	public bool SetOperatingRoomAvailability(
		int id,
		bool isAvailable)
	{
		var operatingRoom =
			GetOperatingRoom(id);

		if (operatingRoom is null)
		{
			return false;
		}

		operatingRoom.IsAvailable =
			isAvailable;

		return true;
	}

	// -------------------------
	// OPERATIES
	// -------------------------

	public IReadOnlyList<Operation> GetOperations(
		int patientId)
	{
		return _operations
			.Where(o =>
				o.PatientId == patientId)
			.OrderBy(o =>
				o.StartTime)
			.ToList();
	}

	public Operation? GetOperation(
		int operationId)
	{
		return _operations
			.FirstOrDefault(o =>
				o.Id == operationId);
	}

	public bool HasOperatingRoomConflict(
		Operation operation)
	{
		var newStart =
			operation.StartTime;

		var newEnd =
			operation.EndTime;

		return _operations.Any(
			existingOperation =>

				existingOperation.Id !=
					operation.Id &&

				existingOperation.OperatingRoomId ==
					operation.OperatingRoomId &&

				existingOperation.Status !=
					AppointmentStatus.Cancelled &&

				newStart <
					existingOperation.EndTime &&

				newEnd >
					existingOperation.StartTime);
	}

	public bool HasSurgeonConflict(
		Operation operation)
	{
		var newStart =
			operation.StartTime;

		var newEnd =
			operation.EndTime;

		return _operations.Any(
			existingOperation =>

				existingOperation.Id !=
					operation.Id &&

				existingOperation.Status !=
					AppointmentStatus.Cancelled &&

				existingOperation.SurgeonIds.Any(
					surgeonId =>
						operation.SurgeonIds.Contains(
							surgeonId)) &&

				newStart <
					existingOperation.EndTime &&

				newEnd >
					existingOperation.StartTime);
	}

	public void AddOperation(
		Operation operation)
	{
		operation.Id =
			_operations.Count == 0
				? 1
				: _operations.Max(o => o.Id) + 1;

		_operations.Add(operation);
	}

	public bool UpdateOperation(
		Operation operation)
	{
		var existingOperation =
			GetOperation(operation.Id);

		if (existingOperation is null)
		{
			return false;
		}

		existingOperation.TreatmentId =
			operation.TreatmentId;

		existingOperation.Name =
			operation.Name.Trim();

		existingOperation.Description =
			operation.Description.Trim();

		existingOperation.StartTime =
			operation.StartTime;

		existingOperation.DurationMinutes =
			operation.DurationMinutes;

		existingOperation.OperatingRoomId =
			operation.OperatingRoomId;

		existingOperation.SurgeonIds =
			operation.SurgeonIds.ToList();

		existingOperation.Status =
			operation.Status;

		return true;
	}

	// -------------------------
	// EVALUATIES
	// -------------------------

	public IReadOnlyList<Evaluation> GetEvaluations(
		int patientId)
	{
		return _evaluations
			.Where(e =>
				e.PatientId == patientId)
			.OrderByDescending(e =>
				e.CreatedAt)
			.ToList();
	}

	public Evaluation? GetEvaluation(
		int evaluationId)
	{
		return _evaluations
			.FirstOrDefault(e =>
				e.Id == evaluationId);
	}

	public void AddEvaluation(
		Evaluation evaluation)
	{
		evaluation.Id =
			_evaluations.Count == 0
				? 1
				: _evaluations.Max(e => e.Id) + 1;

		evaluation.CreatedAt =
			DateTime.Now;

		_evaluations.Add(evaluation);
	}

	// -------------------------
	// DOCUMENTEN
	// -------------------------

	public IReadOnlyList<PatientDocument>
		GetPatientDocuments(int patientId)
	{
		return _patientDocuments
			.Where(d =>
				d.PatientId == patientId)
			.OrderByDescending(d =>
				d.UploadedAt)
			.ToList();
	}

	public PatientDocument? GetPatientDocument(
		int documentId)
	{
		return _patientDocuments
			.FirstOrDefault(d =>
				d.Id == documentId);
	}

	public void AddPatientDocument(
		PatientDocument document)
	{
		document.Id =
			_patientDocuments.Count == 0
				? 1
				: _patientDocuments.Max(d => d.Id) + 1;

		document.UploadedAt =
			DateTime.Now;

		_patientDocuments.Add(document);
	}

	// -------------------------
	// AUDITLOG
	// -------------------------

	public int StartAuditLog(
		int userId,
		string userName,
		int patientId,
		string patientName,
		string action,
		string resource)
	{
		CloseActiveAuditLogs(userId);

		var auditLog = new AuditLog
		{
			Id = _auditLogId++,
			UserId = userId,
			UserName = userName,
			PatientId = patientId,
			PatientName = patientName,
			Action = action,
			Resource = resource,
			OpenedAt = DateTime.Now
		};

		if (!action.Equals(
				"Raadplegen",
				StringComparison.OrdinalIgnoreCase))
		{
			auditLog.ClosedAt =
				DateTime.Now;
		}

		_auditLogs.Add(auditLog);

		return auditLog.Id;
	}

	public void CloseAuditLog(
		int auditLogId)
	{
		var auditLog =
			_auditLogs.FirstOrDefault(a =>
				a.Id == auditLogId);

		if (auditLog is null)
		{
			return;
		}

		if (auditLog.ClosedAt is null)
		{
			auditLog.ClosedAt =
				DateTime.Now;
		}
	}

	public void CloseActiveAuditLogs(
		int userId)
	{
		var activeLogs =
			_auditLogs
				.Where(a =>
					a.UserId == userId &&
					a.ClosedAt == null)
				.ToList();

		foreach (var auditLog
				 in activeLogs)
		{
			auditLog.ClosedAt =
				DateTime.Now;
		}
	}

	public IReadOnlyList<AuditLog>
		GetAuditLogs()
	{
		return _auditLogs
			.OrderByDescending(a =>
				a.OpenedAt)
			.ToList();
	}
}