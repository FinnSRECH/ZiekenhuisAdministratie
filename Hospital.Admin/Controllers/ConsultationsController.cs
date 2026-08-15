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
				new { patientId });
		}

		FillCreateData(patient);

		var consultation = new Consultation
		{
			PatientId = patientId,
			TreatmentId = treatments.First().Id,
			StartTime = DateTime.Now
				.AddDays(1)
				.Date
				.AddHours(9),
			Status = AppointmentStatus.Planned
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
			surgeon.Role != UserRole.Surgeon ||
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

		if (!ModelState.IsValid)
		{
			FillCreateData(patient);

			return View(consultation);
		}

		_data.AddConsultation(consultation);

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

	private void FillCreateData(
		Patient patient)
	{
		ViewBag.Patient = patient;

		ViewBag.Treatments =
			_data.GetTreatments(patient.Id);

		ViewBag.Surgeons =
			_data.GetStaffMembersByRole(
				UserRole.Surgeon);
	}
}