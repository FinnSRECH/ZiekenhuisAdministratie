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

	public IActionResult Index()
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
			_data.GetPatients();

		return View(patients);
	}

	public IActionResult Details(int id)
	{
		var patient = _data.GetPatient(id);

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
				User.Identity?.Name ?? "Onbekende gebruiker",
				patient.Id,
				patient.FullName,
				"Raadplegen",
				"Patiëntdossier");
		}

		return View(patient);
	}
}