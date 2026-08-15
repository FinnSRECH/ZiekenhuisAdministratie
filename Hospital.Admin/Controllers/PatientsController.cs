using System.Security.Claims;
using Hospital.Admin.Services;
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

		// Huidige keuzes bewaren in de view.
		ViewBag.Search = search;
		ViewBag.TreatmentFilter = treatmentFilter;
		ViewBag.Sort = sort;

		return View(
			patients.ToList());
	}

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
}