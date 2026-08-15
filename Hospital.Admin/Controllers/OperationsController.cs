using Hospital.Admin.Services;
using Hospital.Domain.Enums;
using Hospital.Domain.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hospital.Admin.Controllers;

[Authorize]
public class OperationsController : Controller
{
	private readonly HospitalDataService _data;
	private readonly PatientAccessService _patientAccess;

	public OperationsController(
		HospitalDataService data,
		PatientAccessService patientAccess)
	{
		_data = data;
		_patientAccess = patientAccess;
	}

	public IActionResult Index(int patientId)
	{
		var patient =
			_data.GetPatient(patientId);

		if (patient is null)
		{
			return NotFound();
		}

		if (!_patientAccess.CanAccessPatient(
				User,
				patientId))
		{
			return RedirectToAction(
				"AccessDenied",
				"Account");
		}

		ViewBag.Patient = patient;

		var operations =
			_data.GetOperations(patientId);

		return View(operations);
	}

	[Authorize(Roles = "Administrator,Secretary")]
	public IActionResult Create(int patientId)
	{
		var patient =
			_data.GetPatient(patientId);

		if (patient is null)
		{
			return NotFound();
		}

		if (!_patientAccess.CanAccessPatient(
				User,
				patientId))
		{
			return RedirectToAction(
				"AccessDenied",
				"Account");
		}

		var treatments =
			_data.GetTreatments(patientId);

		if (treatments.Count == 0)
		{
			TempData["Error"] =
				"Er moet eerst een behandeling worden gestart voordat een operatie kan worden gepland.";

			return RedirectToAction(
				nameof(Index),
				new { patientId });
		}

		FillCreateData(patient);

		var operation = new Operation
		{
			PatientId = patientId,
			TreatmentId = treatments.First().Id,

			StartTime = DateTime.Now
				.AddDays(7)
				.Date
				.AddHours(8),

			DurationMinutes = 60,

			Status = AppointmentStatus.Planned
		};

		return View(operation);
	}

	[HttpPost]
	[ValidateAntiForgeryToken]
	[Authorize(Roles = "Administrator,Secretary")]
	public IActionResult Create(
		Operation operation)
	{
		var patient =
			_data.GetPatient(
				operation.PatientId);

		if (patient is null)
		{
			return NotFound();
		}

		if (!_patientAccess.CanAccessPatient(
				User,
				operation.PatientId))
		{
			return RedirectToAction(
				"AccessDenied",
				"Account");
		}

		var treatment =
			_data.GetTreatment(
				operation.TreatmentId);

		if (treatment is null ||
			treatment.PatientId !=
				operation.PatientId)
		{
			ModelState.AddModelError(
				nameof(operation.TreatmentId),
				"Selecteer een geldige behandeling.");
		}

		var operatingRoom =
			_data.GetOperatingRoom(
				operation.OperatingRoomId);

		if (operatingRoom is null)
		{
			ModelState.AddModelError(
				nameof(operation.OperatingRoomId),
				"Selecteer een geldige operatiekamer.");
		}
		else if (!operatingRoom.IsAvailable)
		{
			ModelState.AddModelError(
				nameof(operation.OperatingRoomId),
				"Deze operatiekamer is momenteel niet beschikbaar.");
		}

		if (string.IsNullOrWhiteSpace(
				operation.Name))
		{
			ModelState.AddModelError(
				nameof(operation.Name),
				"Vul een naam voor de operatie in.");
		}

		if (string.IsNullOrWhiteSpace(
				operation.Description))
		{
			ModelState.AddModelError(
				nameof(operation.Description),
				"Vul een omschrijving in.");
		}

		if (operation.StartTime <= DateTime.Now)
		{
			ModelState.AddModelError(
				nameof(operation.StartTime),
				"De operatie moet in de toekomst worden gepland.");
		}

		if (operation.DurationMinutes <= 0)
		{
			ModelState.AddModelError(
				nameof(operation.DurationMinutes),
				"De duur van de operatie moet groter zijn dan 0 minuten.");
		}

		if (operation.SurgeonIds.Count == 0)
		{
			ModelState.AddModelError(
				nameof(operation.SurgeonIds),
				"Selecteer minimaal één chirurg.");
		}
		else
		{
			foreach (var surgeonId
					 in operation.SurgeonIds)
			{
				var surgeon =
					_data.GetStaffMember(
						surgeonId);

				if (surgeon is null ||
					surgeon.Role !=
						UserRole.Surgeon ||
					!surgeon.IsActive)
				{
					ModelState.AddModelError(
						nameof(operation.SurgeonIds),
						"Een van de geselecteerde chirurgen is ongeldig of niet actief.");

					break;
				}
			}
		}

		// Controle op dubbele planning.
		if (operation.DurationMinutes > 0 &&
			operation.StartTime > DateTime.Now &&
			operatingRoom is not null &&
			operatingRoom.IsAvailable)
		{
			if (_data.HasOperatingRoomConflict(
					operation))
			{
				ModelState.AddModelError(
					nameof(operation.OperatingRoomId),
					"Deze operatiekamer is tijdens dit tijdstip al in gebruik.");
			}

			if (operation.SurgeonIds.Count > 0 &&
				_data.HasSurgeonConflict(
					operation))
			{
				ModelState.AddModelError(
					nameof(operation.SurgeonIds),
					"Een van de geselecteerde chirurgen is tijdens dit tijdstip al ingepland.");
			}
		}

		if (!ModelState.IsValid)
		{
			FillCreateData(patient);

			return View(operation);
		}

		_data.AddOperation(operation);

		TempData["Success"] =
			"De operatie is succesvol gepland.";

		return RedirectToAction(
			nameof(Index),
			new
			{
				patientId =
					operation.PatientId
			});
	}

	public IActionResult Details(int id)
	{
		var operation =
			_data.GetOperation(id);

		if (operation is null)
		{
			return NotFound();
		}

		var patient =
			_data.GetPatient(
				operation.PatientId);

		if (patient is null)
		{
			return NotFound();
		}

		if (!_patientAccess.CanAccessPatient(
				User,
				operation.PatientId))
		{
			return RedirectToAction(
				"AccessDenied",
				"Account");
		}

		var treatment =
			_data.GetTreatment(
				operation.TreatmentId);

		var operatingRoom =
			_data.GetOperatingRoom(
				operation.OperatingRoomId);

		ViewBag.Patient = patient;
		ViewBag.Treatment = treatment;
		ViewBag.OperatingRoom = operatingRoom;

		ViewBag.Surgeons =
			operation.SurgeonIds
				.Select(id =>
					_data.GetStaffMember(id))
				.Where(s => s is not null)
				.ToList();

		return View(operation);
	}

	private void FillCreateData(
		Patient patient)
	{
		ViewBag.Patient = patient;

		ViewBag.Treatments =
			_data.GetTreatments(patient.Id);

		ViewBag.OperatingRooms =
			_data.GetAvailableOperatingRooms();

		ViewBag.Surgeons =
			_data.GetStaffMembersByRole(
				UserRole.Surgeon);
	}
}