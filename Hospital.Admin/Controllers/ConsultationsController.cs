using System.Security.Claims;
using Hospital.Admin.Services;
using Hospital.Domain.Enums;
using Hospital.Domain.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hospital.Admin.Controllers;

[Authorize]
public class ConsultationsController : Controller
{
	private readonly HospitalDataService _data;
	private readonly PatientAccessService _patientAccess;

	public ConsultationsController(
		HospitalDataService data,
		PatientAccessService patientAccess)
	{
		_data = data;
		_patientAccess = patientAccess;
	}

	// -------------------------
	// CENTRALE PLANNING
	// -------------------------

	public IActionResult Planning()
	{
		var consultations =
			_data.GetAllConsultations()
				.Where(c =>
					_patientAccess.CanAccessPatient(
						User,
						c.PatientId))
				.OrderBy(c =>
					c.StartTime)
				.ToList();

		ViewBag.Patients =
			_data.GetPatients()
				.ToDictionary(
					p => p.Id);

		ViewBag.Surgeons =
			_data.GetStaffMembers()
				.Where(s =>
					s.Role == UserRole.Surgeon)
				.ToDictionary(
					s => s.Id);

		return View(consultations);
	}

	// -------------------------
	// CONSULTATIES PER PATIENT
	// -------------------------

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

		var consultations =
			_data.GetConsultations(patientId);

		return View(consultations);
	}

	// -------------------------
	// CONSULTATIE AANMAKEN
	// -------------------------

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
				"Er moet eerst een behandeling worden gestart voordat een consultatie kan worden gepland.";

			return RedirectToAction(
				nameof(Index),
				new
				{
					patientId
				});
		}

		FillCreateData(patient);

		var consultation =
			new Consultation
			{
				PatientId =
					patientId,

				TreatmentId =
					treatments.First().Id,

				StartTime =
					DateTime.Now
						.AddDays(1)
						.Date
						.AddHours(9),

				Status =
					AppointmentStatus.Planned
			};

		return View(consultation);
	}

	[HttpPost]
	[ValidateAntiForgeryToken]
	[Authorize(Roles = "Administrator,Secretary")]
	public IActionResult Create(
		Consultation consultation)
	{
		var patient =
			_data.GetPatient(
				consultation.PatientId);

		if (patient is null)
		{
			return NotFound();
		}

		if (!_patientAccess.CanAccessPatient(
				User,
				consultation.PatientId))
		{
			return RedirectToAction(
				"AccessDenied",
				"Account");
		}

		ValidateConsultation(
			consultation);

		if (!ModelState.IsValid)
		{
			FillCreateData(patient);

			return View(consultation);
		}

		_data.AddConsultation(
			consultation);

		AddAuditLog(
			patient,
			"Toevoegen",
			"Consultatie");

		TempData["Success"] =
			"De consultatie is succesvol gepland.";

		return RedirectToAction(
			nameof(Index),
			new
			{
				patientId =
					consultation.PatientId
			});
	}

	// -------------------------
	// CONSULTATIE WIJZIGEN
	// -------------------------

	[Authorize(Roles = "Administrator,Secretary")]
	public IActionResult Edit(int id)
	{
		var consultation =
			_data.GetConsultation(id);

		if (consultation is null)
		{
			return NotFound();
		}

		var patient =
			_data.GetPatient(
				consultation.PatientId);

		if (patient is null)
		{
			return NotFound();
		}

		if (!_patientAccess.CanAccessPatient(
				User,
				patient.Id))
		{
			return RedirectToAction(
				"AccessDenied",
				"Account");
		}

		FillCreateData(patient);

		return View(consultation);
	}

	[HttpPost]
	[ValidateAntiForgeryToken]
	[Authorize(Roles = "Administrator,Secretary")]
	public IActionResult Edit(
		Consultation consultation)
	{
		var existingConsultation =
			_data.GetConsultation(
				consultation.Id);

		if (existingConsultation is null)
		{
			return NotFound();
		}

		var patient =
			_data.GetPatient(
				existingConsultation.PatientId);

		if (patient is null)
		{
			return NotFound();
		}

		if (!_patientAccess.CanAccessPatient(
				User,
				patient.Id))
		{
			return RedirectToAction(
				"AccessDenied",
				"Account");
		}

		consultation.PatientId =
			existingConsultation.PatientId;

		ValidateConsultation(
			consultation);

		if (!ModelState.IsValid)
		{
			FillCreateData(patient);

			return View(consultation);
		}

		var updated =
			_data.UpdateConsultation(
				consultation);

		if (!updated)
		{
			return NotFound();
		}

		AddAuditLog(
			patient,
			"Wijzigen",
			"Consultatie");

		TempData["Success"] =
			"De consultatie is succesvol gewijzigd.";

		return RedirectToAction(
			nameof(Index),
			new
			{
				patientId =
					patient.Id
			});
	}

	// -------------------------
	// VALIDATIE
	// -------------------------

	private void ValidateConsultation(
		Consultation consultation)
	{
		var treatment =
			_data.GetTreatment(
				consultation.TreatmentId);

		if (treatment is null ||
			treatment.PatientId !=
				consultation.PatientId)
		{
			ModelState.AddModelError(
				nameof(consultation.TreatmentId),
				"Selecteer een geldige behandeling.");
		}

		var surgeon =
			_data.GetStaffMember(
				consultation.SurgeonId);

		if (surgeon is null ||
			surgeon.Role !=
				UserRole.Surgeon ||
			!surgeon.IsActive)
		{
			ModelState.AddModelError(
				nameof(consultation.SurgeonId),
				"Selecteer een geldige actieve chirurg.");
		}

		if (string.IsNullOrWhiteSpace(
				consultation.Reason))
		{
			ModelState.AddModelError(
				nameof(consultation.Reason),
				"Vul de reden van de consultatie in.");
		}

		if (string.IsNullOrWhiteSpace(
				consultation.Room))
		{
			ModelState.AddModelError(
				nameof(consultation.Room),
				"Vul een ruimte in.");
		}

		if (consultation.StartTime <=
			DateTime.Now)
		{
			ModelState.AddModelError(
				nameof(consultation.StartTime),
				"De consultatie moet in de toekomst worden gepland.");
		}
	}

	// -------------------------
	// VIEW DATA
	// -------------------------

	private void FillCreateData(
		Patient patient)
	{
		ViewBag.Patient =
			patient;

		ViewBag.Treatments =
			_data.GetTreatments(
				patient.Id);

		ViewBag.Surgeons =
			_data.GetStaffMembersByRole(
				UserRole.Surgeon);
	}

	// -------------------------
	// AUDITLOG
	// -------------------------

	private void AddAuditLog(
		Patient patient,
		string action,
		string resource)
	{
		var userIdText =
			User.FindFirstValue(
				ClaimTypes.NameIdentifier);

		if (!int.TryParse(
				userIdText,
				out var userId))
		{
			return;
		}

		_data.StartAuditLog(
			userId,
			User.Identity?.Name ??
				"Onbekende gebruiker",
			patient.Id,
			patient.FullName,
			action,
			resource);
	}
}