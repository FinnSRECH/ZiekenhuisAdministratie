using System.Security.Claims;
using Hospital.Admin.Services;
using Hospital.Domain.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hospital.Admin.Controllers;

[Authorize]
public class DocumentsController : Controller
{
	private readonly HospitalDataService _data;
	private readonly PatientAccessService _patientAccess;

	public DocumentsController(
		HospitalDataService data,
		PatientAccessService patientAccess)
	{
		_data = data;
		_patientAccess = patientAccess;
	}

	// -------------------------
	// DOCUMENTENOVERZICHT
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

		var documents =
			_data.GetPatientDocuments(
				patientId);

		return View(documents);
	}

	// -------------------------
	// DOCUMENT TOEVOEGEN
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

		ViewBag.Patient = patient;

		return View();
	}

	[HttpPost]
	[ValidateAntiForgeryToken]
	[Authorize(Roles = "Administrator,Secretary")]
	public async Task<IActionResult> Create(
		int patientId,
		IFormFile? file)
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

		if (file is null ||
			file.Length == 0)
		{
			ModelState.AddModelError(
				"file",
				"Selecteer een document.");

			ViewBag.Patient = patient;

			return View();
		}

		// Maximaal 5 MB.
		const long maxFileSize =
			5 * 1024 * 1024;

		if (file.Length > maxFileSize)
		{
			ModelState.AddModelError(
				"file",
				"Het document mag maximaal 5 MB groot zijn.");
		}

		var allowedExtensions =
			new[]
			{
				".pdf",
				".doc",
				".docx",
				".jpg",
				".jpeg",
				".png"
			};

		var extension =
			Path.GetExtension(
				file.FileName)
				.ToLowerInvariant();

		if (!allowedExtensions.Contains(
				extension))
		{
			ModelState.AddModelError(
				"file",
				"Alleen PDF-, Word-, JPG- en PNG-bestanden zijn toegestaan.");
		}

		if (!ModelState.IsValid)
		{
			ViewBag.Patient = patient;

			return View();
		}

		byte[] content;

		using (var memoryStream =
			   new MemoryStream())
		{
			await file.CopyToAsync(
				memoryStream);

			content =
				memoryStream.ToArray();
		}

		var userIdText =
			User.FindFirstValue(
				ClaimTypes.NameIdentifier);

		int.TryParse(
			userIdText,
			out var userId);

		var document =
			new PatientDocument
			{
				PatientId =
					patient.Id,

				FileName =
					Path.GetFileName(
						file.FileName),

				ContentType =
					file.ContentType,

				Content =
					content,

				UploadedByUserId =
					userId,

				UploadedByUserName =
					User.Identity?.Name ??
					"Onbekende gebruiker"
			};

		_data.AddPatientDocument(
			document);

		AddAuditLog(
			patient,
			"Toevoegen",
			"Document");

		TempData["Success"] =
			"Het document is succesvol toegevoegd.";

		return RedirectToAction(
			nameof(Index),
			new
			{
				patientId =
					patient.Id
			});
	}

	// -------------------------
	// DOCUMENT OPENEN
	// -------------------------

	public IActionResult Download(int id)
	{
		var document =
			_data.GetPatientDocument(id);

		if (document is null)
		{
			return NotFound();
		}

		var patient =
			_data.GetPatient(
				document.PatientId);

		if (patient is null)
		{
			return NotFound();
		}

		/*
		 * Heel belangrijk:
		 * controleer opnieuw de patiënttoegang.
		 *
		 * Hierdoor kan iemand niet simpelweg
		 * een andere document-ID in de URL zetten.
		 */
		if (!_patientAccess.CanAccessPatient(
				User,
				patient.Id))
		{
			return RedirectToAction(
				"AccessDenied",
				"Account");
		}

		return File(
			document.Content,
			document.ContentType,
			document.FileName);
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