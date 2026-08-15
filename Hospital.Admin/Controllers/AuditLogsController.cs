using Hospital.Admin.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hospital.Admin.Controllers;

[Authorize(Roles = "Administrator")]
public class AuditLogsController : Controller
{
	private readonly HospitalDataService _data;

	public AuditLogsController(HospitalDataService data)
	{
		_data = data;
	}

	public IActionResult Index()
	{
		var auditLogs =
			_data.GetAuditLogs();

		return View(auditLogs);
	}
}