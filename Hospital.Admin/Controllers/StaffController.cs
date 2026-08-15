using System.Security.Claims;
using Hospital.Admin.Services;
using Hospital.Domain.Enums;
using Hospital.Domain.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hospital.Admin.Controllers;

[Authorize(Roles = "Administrator")]
public class StaffController : Controller
{
	private readonly HospitalDataService _data;

	public StaffController(HospitalDataService data)
	{
		_data = data;
	}

	public IActionResult Index()
	{
		var staffMembers = _data.GetStaffMembers();

		return View(staffMembers);
	}

	[HttpGet]
	public IActionResult Create()
	{
		ViewBag.Roles = Enum.GetValues<UserRole>();

		return View(new StaffMember());
	}

	[HttpPost]
	[ValidateAntiForgeryToken]
	public IActionResult Create(
		StaffMember staffMember,
		string password)
	{
		if (string.IsNullOrWhiteSpace(staffMember.FirstName))
		{
			ModelState.AddModelError(
				nameof(staffMember.FirstName),
				"Vul een voornaam in.");
		}

		if (string.IsNullOrWhiteSpace(staffMember.LastName))
		{
			ModelState.AddModelError(
				nameof(staffMember.LastName),
				"Vul een achternaam in.");
		}

		if (string.IsNullOrWhiteSpace(staffMember.Email))
		{
			ModelState.AddModelError(
				nameof(staffMember.Email),
				"Vul een e-mailadres in.");
		}
		else if (_data.StaffEmailExists(staffMember.Email))
		{
			ModelState.AddModelError(
				nameof(staffMember.Email),
				"Er bestaat al een medewerker met dit e-mailadres.");
		}

		if (string.IsNullOrWhiteSpace(password))
		{
			ModelState.AddModelError(
				"password",
				"Vul een wachtwoord in.");
		}
		else if (password.Length < 8)
		{
			ModelState.AddModelError(
				"password",
				"Het wachtwoord moet minimaal 8 tekens bevatten.");
		}

		if (!ModelState.IsValid)
		{
			ViewBag.Roles = Enum.GetValues<UserRole>();

			return View(staffMember);
		}

		_data.AddStaffMember(
			staffMember,
			password);

		TempData["Success"] =
			$"Medewerker {staffMember.FullName} is succesvol toegevoegd.";

		return RedirectToAction(nameof(Index));
	}

	[HttpPost]
	[ValidateAntiForgeryToken]
	public IActionResult Deactivate(int id)
	{
		var staffMember = _data.GetStaffMember(id);

		if (staffMember is null)
		{
			return NotFound();
		}

		var currentUserId =
			User.FindFirstValue(ClaimTypes.NameIdentifier);

		if (currentUserId == staffMember.Id.ToString())
		{
			TempData["Error"] =
				"Je kunt je eigen account niet deactiveren.";

			return RedirectToAction(nameof(Index));
		}

		if (!staffMember.IsActive)
		{
			TempData["Error"] =
				"Deze medewerker is al gedeactiveerd.";

			return RedirectToAction(nameof(Index));
		}

		_data.DeactivateStaffMember(id);

		TempData["Success"] =
			$"Medewerker {staffMember.FullName} is gedeactiveerd.";

		return RedirectToAction(nameof(Index));
	}
}