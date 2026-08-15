using System.Security.Claims;
using Hospital.Admin.Services;
using Hospital.Domain.Enums;
using Hospital.Domain.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hospital.Admin.Controllers;

[Authorize]
public class TreatmentsController : Controller
{
	private readonly HospitalDataService _data;
	private readonly PatientAccessService _patientAccess;

	public TreatmentsController(
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

		var treatments =
			_data.GetTreatments(patientId);

		return View(treatments);
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

		var activeTreatment =
			_data.GetActiveTreatment(patientId);

		if (activeTreatment is not null)
		{
			TempData["Error"] =
				"Deze patiënt heeft al een actieve behandeling. " +
				"Sluit de huidige behandeling eerst af voordat een nieuwe behandeling wordt gestart.";

			return RedirectToAction(
				nameof(Index),
				new
				{
					patientId
				});
		}

		FillCreateData(patient);

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
	[Authorize(Roles = "Administrator,Secretary")]
	public IActionResult Create(Treatment treatment)
	{
		var patient =
			_data.GetPatient(treatment.PatientId);

		if (patient is null)
		{
			return NotFound();
		}

		var activeTreatment =
			_data.GetActiveTreatment(
				treatment.PatientId);

		if (activeTreatment is not null)
		{
			TempData["Error"] =
				"Deze patiënt heeft al een actieve behandeling. " +
				"Sluit de huidige behandeling eerst af voordat een nieuwe behandeling wordt gestart.";

			return RedirectToAction(
				nameof(Index),
				new
				{
					patientId = treatment.PatientId
				});
		}

		if (string.IsNullOrWhiteSpace(
				treatment.Name))
		{
			ModelState.AddModelError(
				nameof(treatment.Name),
				"Vul een naam voor de behandeling in.");
		}

		if (string.IsNullOrWhiteSpace(
				treatment.Description))
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
		else
		{
			var secretary =
				_data.GetStaffMember(
					treatment.SecretaryId.Value);

			if (secretary is null ||
				secretary.Role != UserRole.Secretary ||
				!secretary.IsActive)
			{
				ModelState.AddModelError(
					nameof(treatment.SecretaryId),
					"Selecteer een geldige actieve secretaresse.");
			}
		}

		if (treatment.SurgeonIds.Count == 0)
		{
			ModelState.AddModelError(
				nameof(treatment.SurgeonIds),
				"Selecteer minimaal één chirurg.");
		}
		else
		{
			foreach (var surgeonId
					 in treatment.SurgeonIds)
			{
				var surgeon =
					_data.GetStaffMember(surgeonId);

				if (surgeon is null ||
					surgeon.Role != UserRole.Surgeon ||
					!surgeon.IsActive)
				{
					ModelState.AddModelError(
						nameof(treatment.SurgeonIds),
						"Een van de geselecteerde chirurgen is ongeldig of niet actief.");

					break;
				}
			}
		}

		if (treatment.NurseIds.Count == 0)
		{
			ModelState.AddModelError(
				nameof(treatment.NurseIds),
				"Selecteer minimaal één verpleegkundige.");
		}
		else
		{
			foreach (var nurseId
					 in treatment.NurseIds)
			{
				var nurse =
					_data.GetStaffMember(nurseId);

				if (nurse is null ||
					nurse.Role != UserRole.Nurse ||
					!nurse.IsActive)
				{
					ModelState.AddModelError(
						nameof(treatment.NurseIds),
						"Een van de geselecteerde verpleegkundigen is ongeldig of niet actief.");

					break;
				}
			}
		}

		if (!ModelState.IsValid)
		{
			FillCreateData(patient);

			return View(treatment);
		}

		_data.AddTreatment(treatment);

		AddAuditLog(
			patient,
			"Toevoegen",
			"Behandeling");

		TempData["Success"] =
			"De behandeling is succesvol gestart.";

		return RedirectToAction(
			nameof(Index),
			new
			{
				patientId = treatment.PatientId
			});
	}

	public IActionResult Details(int id)
	{
		var treatment =
			_data.GetTreatment(id);

		if (treatment is null)
		{
			return NotFound();
		}

		var patient =
			_data.GetPatient(
				treatment.PatientId);

		if (patient is null)
		{
			return NotFound();
		}

		if (!_patientAccess.CanAccessPatient(
				User,
				treatment.PatientId))
		{
			return RedirectToAction(
				"AccessDenied",
				"Account");
		}

		ViewBag.Patient = patient;

		return View(treatment);
	}

	[HttpPost]
	[ValidateAntiForgeryToken]
	[Authorize(Roles = "Administrator,Secretary")]
	public IActionResult Complete(int id)
	{
		var treatment =
			_data.GetTreatment(id);

		if (treatment is null)
		{
			return NotFound();
		}

		var patient =
			_data.GetPatient(
				treatment.PatientId);

		if (patient is null)
		{
			return NotFound();
		}

		if (treatment.Status != TreatmentStatus.Active)
		{
			TempData["Error"] =
				"Alleen een actieve behandeling kan worden afgesloten.";

			return RedirectToAction(
				nameof(Details),
				new
				{
					id = treatment.Id
				});
		}

		var completed =
			_data.CompleteTreatment(
				treatment.Id);

		if (!completed)
		{
			TempData["Error"] =
				"De behandeling kon niet worden afgesloten.";

			return RedirectToAction(
				nameof(Details),
				new
				{
					id = treatment.Id
				});
		}

		AddAuditLog(
			patient,
			"Wijzigen",
			"Behandeling afgesloten");

		TempData["Success"] =
			"De behandeling is succesvol afgesloten.";

		return RedirectToAction(
			nameof(Index),
			new
			{
				patientId = treatment.PatientId
			});
	}

	private void FillCreateData(
		Patient patient)
	{
		ViewBag.Patient = patient;

		ViewBag.Secretaries =
			_data.GetStaffMembersByRole(
				UserRole.Secretary);

		ViewBag.Surgeons =
			_data.GetStaffMembersByRole(
				UserRole.Surgeon);

		ViewBag.Nurses =
			_data.GetStaffMembersByRole(
				UserRole.Nurse);
	}

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