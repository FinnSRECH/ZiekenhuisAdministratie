using System.Security.Claims;
using Hospital.Admin.Services;
using Hospital.Domain.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hospital.Admin.Controllers;

[Authorize]
public class EvaluationsController : Controller
{
	private readonly HospitalDataService _data;
	private readonly PatientAccessService _patientAccess;

	public EvaluationsController(
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

		var evaluations =
			_data.GetEvaluations(patientId);

		return View(evaluations);
	}

	[Authorize(Roles = "Administrator,Nurse")]
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
				"Er is nog geen behandeling beschikbaar voor deze patiënt.";

			return RedirectToAction(
				nameof(Index),
				new
				{
					patientId
				});
		}

		FillCreateData(patient);

		var evaluation = new Evaluation
		{
			PatientId = patientId,
			TreatmentId = treatments.First().Id
		};

		return View(evaluation);
	}

	[HttpPost]
	[ValidateAntiForgeryToken]
	[Authorize(Roles = "Administrator,Nurse")]
	public IActionResult Create(
		Evaluation evaluation)
	{
		var patient =
			_data.GetPatient(
				evaluation.PatientId);

		if (patient is null)
		{
			return NotFound();
		}

		if (!_patientAccess.CanAccessPatient(
				User,
				evaluation.PatientId))
		{
			return RedirectToAction(
				"AccessDenied",
				"Account");
		}

		var treatment =
			_data.GetTreatment(
				evaluation.TreatmentId);

		if (treatment is null ||
			treatment.PatientId != evaluation.PatientId)
		{
			ModelState.AddModelError(
				nameof(evaluation.TreatmentId),
				"Selecteer een geldige behandeling.");
		}

		if (string.IsNullOrWhiteSpace(
				evaluation.Title))
		{
			ModelState.AddModelError(
				nameof(evaluation.Title),
				"Vul een titel voor de evaluatie in.");
		}

		if (string.IsNullOrWhiteSpace(
				evaluation.Description))
		{
			ModelState.AddModelError(
				nameof(evaluation.Description),
				"Vul een omschrijving van de evaluatie in.");
		}

		var userIdText =
			User.FindFirstValue(
				ClaimTypes.NameIdentifier);

		if (!int.TryParse(
				userIdText,
				out var staffMemberId))
		{
			return RedirectToAction(
				"AccessDenied",
				"Account");
		}

		var staffMember =
			_data.GetStaffMember(staffMemberId);

		if (staffMember is null ||
			!staffMember.IsActive)
		{
			return RedirectToAction(
				"AccessDenied",
				"Account");
		}

		if (!ModelState.IsValid)
		{
			FillCreateData(patient);

			return View(evaluation);
		}

		evaluation.StaffMemberId =
			staffMemberId;

		_data.AddEvaluation(evaluation);

		_data.StartAuditLog(
			staffMemberId,
			staffMember.FullName,
			patient.Id,
			patient.FullName,
			"Toevoegen",
			"Evaluatie");

		TempData["Success"] =
			"De evaluatie is succesvol geregistreerd.";

		return RedirectToAction(
			nameof(Index),
			new
			{
				patientId = evaluation.PatientId
			});
	}

	public IActionResult Details(int id)
	{
		var evaluation =
			_data.GetEvaluation(id);

		if (evaluation is null)
		{
			return NotFound();
		}

		var patient =
			_data.GetPatient(
				evaluation.PatientId);

		if (patient is null)
		{
			return NotFound();
		}

		if (!_patientAccess.CanAccessPatient(
				User,
				evaluation.PatientId))
		{
			return RedirectToAction(
				"AccessDenied",
				"Account");
		}

		ViewBag.Patient = patient;

		ViewBag.Treatment =
			_data.GetTreatment(
				evaluation.TreatmentId);

		ViewBag.StaffMember =
			_data.GetStaffMember(
				evaluation.StaffMemberId);

		return View(evaluation);
	}

	private void FillCreateData(
		Patient patient)
	{
		ViewBag.Patient = patient;

		ViewBag.Treatments =
			_data.GetTreatments(patient.Id);
	}
}