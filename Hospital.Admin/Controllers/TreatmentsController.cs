using Hospital.Admin.Services;
using Hospital.Domain.Enums;
using Hospital.Domain.Models;
using Microsoft.AspNetCore.Mvc;

namespace Hospital.Admin.Controllers;

public class TreatmentsController : Controller
{
	private readonly HospitalDataService _data;

	public TreatmentsController(HospitalDataService data)
	{
		_data = data;
	}

	public IActionResult Index(int patientId)
	{
		var patient = _data.GetPatient(patientId);

		if (patient is null)
		{
			return NotFound();
		}

		ViewBag.Patient = patient;

		var treatments = _data.GetTreatments(patientId);

		return View(treatments);
	}

	public IActionResult Create(int patientId)
	{
		var patient = _data.GetPatient(patientId);

		if (patient is null)
		{
			return NotFound();
		}

		ViewBag.Patient = patient;

		ViewBag.Secretaries =
			_data.GetStaffMembersByRole(UserRole.Secretary);

		ViewBag.Surgeons =
			_data.GetStaffMembersByRole(UserRole.Surgeon);

		ViewBag.Nurses =
			_data.GetStaffMembersByRole(UserRole.Nurse);

		var treatment = new Treatment
		{
			PatientId = patientId,
			StartDate = DateTime.Today,
			Status = TreatmentStatus.Active
		};

		return View(treatment);
	}

	[HttpPost]
	[ValidateAntiForgeryToken]
	public IActionResult Create(Treatment treatment)
	{
		var patient = _data.GetPatient(treatment.PatientId);

		if (patient is null)
		{
			return NotFound();
		}

		if (string.IsNullOrWhiteSpace(treatment.Name))
		{
			ModelState.AddModelError(
				nameof(treatment.Name),
				"Vul een naam voor de behandeling in.");
		}

		if (string.IsNullOrWhiteSpace(treatment.Description))
		{
			ModelState.AddModelError(
				nameof(treatment.Description),
				"Vul een omschrijving in.");
		}

		if (treatment.SecretaryId is null)
		{
			ModelState.AddModelError(
				nameof(treatment.SecretaryId),
				"Selecteer een verantwoordelijke secretaresse.");
		}

		if (treatment.SurgeonIds.Count == 0)
		{
			ModelState.AddModelError(
				nameof(treatment.SurgeonIds),
				"Selecteer minimaal één chirurg.");
		}

		if (treatment.NurseIds.Count == 0)
		{
			ModelState.AddModelError(
				nameof(treatment.NurseIds),
				"Selecteer minimaal één verpleegkundige.");
		}

		if (!ModelState.IsValid)
		{
			ViewBag.Patient = patient;

			ViewBag.Secretaries =
				_data.GetStaffMembersByRole(UserRole.Secretary);

			ViewBag.Surgeons =
				_data.GetStaffMembersByRole(UserRole.Surgeon);

			ViewBag.Nurses =
				_data.GetStaffMembersByRole(UserRole.Nurse);

			return View(treatment);
		}

		_data.AddTreatment(treatment);

		TempData["Success"] =
			"De behandeling is succesvol gestart.";

		return RedirectToAction(
			nameof(Index),
			new { patientId = treatment.PatientId });
	}

	public IActionResult Details(int id)
	{
		var treatment = _data.GetTreatment(id);

		if (treatment is null)
		{
			return NotFound();
		}

		var patient = _data.GetPatient(treatment.PatientId);

		if (patient is null)
		{
			return NotFound();
		}

		ViewBag.Patient = patient;

		return View(treatment);
	}
}