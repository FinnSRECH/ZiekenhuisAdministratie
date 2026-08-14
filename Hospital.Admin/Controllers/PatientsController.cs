using Hospital.Admin.Services;
using Microsoft.AspNetCore.Mvc;

namespace Hospital.Admin.Controllers;

public class PatientsController : Controller
{
	private readonly HospitalDataService _data;

	public PatientsController(HospitalDataService data)
	{
		_data = data;
	}

	public IActionResult Index()
	{
		var patients = _data.GetPatients();

		return View(patients);
	}

	public IActionResult Details(int id)
	{
		var patient = _data.GetPatient(id);

		if (patient is null)
		{
			return NotFound();
		}

		return View(patient);
	}
}