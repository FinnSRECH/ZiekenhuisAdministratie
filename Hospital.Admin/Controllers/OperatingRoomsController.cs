using Hospital.Admin.Services;
using Hospital.Domain.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hospital.Admin.Controllers;

[Authorize(Roles = "Administrator")]
public class OperatingRoomsController : Controller
{
	private readonly HospitalDataService _data;

	public OperatingRoomsController(HospitalDataService data)
	{
		_data = data;
	}

	public IActionResult Index()
	{
		var operatingRooms =
			_data.GetOperatingRooms();

		return View(operatingRooms);
	}

	[HttpGet]
	public IActionResult Create()
	{
		return View(new OperatingRoom());
	}

	[HttpPost]
	[ValidateAntiForgeryToken]
	public IActionResult Create(
		OperatingRoom operatingRoom)
	{
		if (string.IsNullOrWhiteSpace(operatingRoom.Name))
		{
			ModelState.AddModelError(
				nameof(operatingRoom.Name),
				"Vul een naam voor de operatiekamer in.");
		}
		else if (_data.OperatingRoomNameExists(
					 operatingRoom.Name))
		{
			ModelState.AddModelError(
				nameof(operatingRoom.Name),
				"Er bestaat al een operatiekamer met deze naam.");
		}

		if (string.IsNullOrWhiteSpace(
				operatingRoom.Location))
		{
			ModelState.AddModelError(
				nameof(operatingRoom.Location),
				"Vul een locatie in.");
		}

		if (!ModelState.IsValid)
		{
			return View(operatingRoom);
		}

		_data.AddOperatingRoom(operatingRoom);

		TempData["Success"] =
			$"Operatiekamer {operatingRoom.Name} is toegevoegd.";

		return RedirectToAction(nameof(Index));
	}

	[HttpPost]
	[ValidateAntiForgeryToken]
	public IActionResult SetAvailability(
		int id,
		bool isAvailable)
	{
		var operatingRoom =
			_data.GetOperatingRoom(id);

		if (operatingRoom is null)
		{
			return NotFound();
		}

		_data.SetOperatingRoomAvailability(
			id,
			isAvailable);

		TempData["Success"] = isAvailable
			? $"{operatingRoom.Name} is beschikbaar gemaakt."
			: $"{operatingRoom.Name} is onbeschikbaar gemaakt.";

		return RedirectToAction(nameof(Index));
	}
}