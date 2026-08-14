using Hospital.Domain.Enums;
using Hospital.Domain.Models;

namespace Hospital.Admin.Services;

public class HospitalDataService
{
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

	public IReadOnlyList<Patient> GetPatients()
	{
		return _patients;
	}

	public Patient? GetPatient(int id)
	{
		return _patients.FirstOrDefault(p => p.Id == id);
	}

	public IReadOnlyList<StaffMember> GetStaffMembers()
	{
		return _staffMembers;
	}

	public IReadOnlyList<Treatment> GetTreatments(int patientId)
	{
		return _treatments
			.Where(t => t.PatientId == patientId)
			.ToList();
	}

	public IReadOnlyList<Consultation> GetConsultations(int patientId)
	{
		return _consultations
			.Where(c => c.PatientId == patientId)
			.ToList();
	}

	public IReadOnlyList<OperatingRoom> GetOperatingRooms()
	{
		return _operatingRooms;
	}

	public IReadOnlyList<Operation> GetOperations(int patientId)
	{
		return _operations
			.Where(o => o.PatientId == patientId)
			.ToList();
	}
}