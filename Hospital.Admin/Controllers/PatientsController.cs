using System.Net.Mail;
using System.Security.Claims;
using Hospital.Admin.Services;
using Hospital.Domain.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hospital.Admin.Controllers;

[Authorize]
public class PatientsController : Controller
{
	private readonly HospitalDataService _data;
	private readonly PatientAccessService _patientAccess;

	public PatientsController(
		HospitalDataService data,
		PatientAccessService patientAccess)
	{
		_data = data;
		_patientAccess = patientAccess;
	}

	// -------------------------
	// PATIENTENOVERZICHT
	// -------------------------

	public IActionResult Index(
		string? search,
		string? treatmentFilter,
		string? sort)
	{
		var userIdText =
			User.FindFirstValue(
				ClaimTypes.NameIdentifier);

		if (int.TryParse(
				userIdText,
				out var userId))
		{
			_data.CloseActiveAuditLogs(userId);
		}

		var patients =
			_data.GetPatients()
				.AsEnumerable();

		// -------------------------
		// ZOEKEN
		// -------------------------

		if (!string.IsNullOrWhiteSpace(search))
		{
			search = search.Trim();

			patients = patients.Where(p =>
				p.FullName.Contains(
					search,
					StringComparison.OrdinalIgnoreCase) ||
				p.Email.Contains(
					search,
					StringComparison.OrdinalIgnoreCase) ||
				p.PhoneNumber.Contains(
					search,
					StringComparison.OrdinalIgnoreCase));
		}

		// -------------------------
		// FILTEREN
		// -------------------------

		if (treatmentFilter == "active")
		{
			patients = patients.Where(p =>
				_data.GetActiveTreatment(p.Id) is not null);
		}
		else if (treatmentFilter == "inactive")
		{
			patients = patients.Where(p =>
				_data.GetActiveTreatment(p.Id) is null);
		}

		// -------------------------
		// SORTEREN
		// -------------------------

		patients = sort switch
		{
			"name_desc" =>
				patients.OrderByDescending(p =>
					p.FullName),

			"birth_asc" =>
				patients.OrderBy(p =>
					p.DateOfBirth),

			"birth_desc" =>
				patients.OrderByDescending(p =>
					p.DateOfBirth),

			_ =>
				patients.OrderBy(p =>
					p.FullName)
		};

		ViewBag.Search = search;
		ViewBag.TreatmentFilter = treatmentFilter;
		ViewBag.Sort = sort;

		return View(
			patients.ToList());
	}

	// -------------------------
	// PATIENTDOSSIER
	// -------------------------

	public IActionResult Details(int id)
	{
		var patient =
			_data.GetPatient(id);

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

		var userIdText =
			User.FindFirstValue(
				ClaimTypes.NameIdentifier);

		if (int.TryParse(
				userIdText,
				out var userId))
		{
			_data.StartAuditLog(
				userId,
				User.Identity?.Name ??
					"Onbekende gebruiker",
				patient.Id,
				patient.FullName,
				"Raadplegen",
				"Patiëntdossier");
		}

		return View(patient);
	}

	// -------------------------
	// PATIENT WIJZIGEN
	// -------------------------

	[Authorize(Roles = "Administrator,Secretary")]
	public IActionResult Edit(int id)
	{
		var patient =
			_data.GetPatient(id);

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

		return View(patient);
	}

	[HttpPost]
	[ValidateAntiForgeryToken]
	[Authorize(Roles = "Administrator,Secretary")]
	public IActionResult Edit(
		Patient patient)
	{
		/*
		 * Eerst de bestaande patiënt ophalen.
		 * Zo weten we zeker dat het ID geldig is.
		 */
		var existingPatient =
			_data.GetPatient(patient.Id);

		if (existingPatient is null)
		{
			return NotFound();
		}

		if (!_patientAccess.CanAccessPatient(
				User,
				existingPatient.Id))
		{
			return RedirectToAction(
				"AccessDenied",
				"Account");
		}

		// -------------------------
		// VALIDATIE
		// -------------------------

		if (string.IsNullOrWhiteSpace(
				patient.FirstName))
		{
			ModelState.AddModelError(
				nameof(patient.FirstName),
				"Vul een voornaam in.");
		}

		if (string.IsNullOrWhiteSpace(
				patient.LastName))
		{
			ModelState.AddModelError(
				nameof(patient.LastName),
				"Vul een achternaam in.");
		}

		if (patient.DateOfBirth >
			DateOnly.FromDateTime(DateTime.Today))
		{
			ModelState.AddModelError(
				nameof(patient.DateOfBirth),
				"De geboortedatum mag niet in de toekomst liggen.");
		}

		if (string.IsNullOrWhiteSpace(
				patient.Email))
		{
			ModelState.AddModelError(
				nameof(patient.Email),
				"Vul een e-mailadres in.");
		}
		else if (!IsValidEmail(
					 patient.Email))
		{
			ModelState.AddModelError(
				nameof(patient.Email),
				"Vul een geldig e-mailadres in.");
		}

		if (string.IsNullOrWhiteSpace(
				patient.PhoneNumber))
		{
			ModelState.AddModelError(
				nameof(patient.PhoneNumber),
				"Vul een telefoonnummer in.");
		}

		if (string.IsNullOrWhiteSpace(
				patient.Address))
		{
			ModelState.AddModelError(
				nameof(patient.Address),
				"Vul een adres in.");
		}

		if (!ModelState.IsValid)
		{
			return View(patient);
		}

		var updated =
			_data.UpdatePatient(patient);

		if (!updated)
		{
			return NotFound();
		}

		AddAuditLog(
			existingPatient,
			"Wijzigen",
			"Patiëntgegevens");

		TempData["Success"] =
			"De patiëntgegevens zijn succesvol gewijzigd.";

		return RedirectToAction(
			nameof(Details),
			new
			{
				id = patient.Id
			});
	}

	// -------------------------
	// HULPMETHODES
	// -------------------------

	private static bool IsValidEmail(
		string email)
	{
		try
		{
			var address =
				new MailAddress(
					email.Trim());

			return address.Address.Equals(
				email.Trim(),
				StringComparison.OrdinalIgnoreCase);
		}
		catch
		{
			return false;
		}
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