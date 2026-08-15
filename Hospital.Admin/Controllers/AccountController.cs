using System.Security.Claims;
using Hospital.Admin.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hospital.Admin.Controllers;

public class AccountController : Controller
{
	private readonly HospitalDataService _data;
	private readonly PasswordService _passwordService;

	public AccountController(
		HospitalDataService data,
		PasswordService passwordService)
	{
		_data = data;
		_passwordService = passwordService;
	}

	[AllowAnonymous]
	[HttpGet]
	public IActionResult Login()
	{
		if (User.Identity?.IsAuthenticated == true)
		{
			return RedirectToAction(
				"Index",
				"Home");
		}

		return View();
	}

	[AllowAnonymous]
	[HttpPost]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> Login(
		string email,
		string password)
	{
		var staffMember =
			_data.GetStaffMemberByEmail(email);

		if (staffMember is null ||
			!staffMember.IsActive ||
			!_passwordService.VerifyPassword(
				password,
				staffMember.PasswordHash))
		{
			ViewBag.Error =
				"Het e-mailadres of wachtwoord is onjuist.";

			ViewBag.Email = email;

			return View();
		}

		var claims = new List<Claim>
		{
			new Claim(
				ClaimTypes.NameIdentifier,
				staffMember.Id.ToString()),

			new Claim(
				ClaimTypes.Name,
				staffMember.FullName),

			new Claim(
				ClaimTypes.Email,
				staffMember.Email),

			new Claim(
				ClaimTypes.Role,
				staffMember.Role.ToString())
		};

		var identity = new ClaimsIdentity(
			claims,
			CookieAuthenticationDefaults.AuthenticationScheme);

		var principal =
			new ClaimsPrincipal(identity);

		await HttpContext.SignInAsync(
			CookieAuthenticationDefaults.AuthenticationScheme,
			principal);

		return RedirectToAction(
			"Index",
			"Home");
	}

	[AllowAnonymous]
	[HttpGet]
	public IActionResult ForgotPassword()
	{
		if (User.Identity?.IsAuthenticated == true)
		{
			return RedirectToAction(
				"Index",
				"Home");
		}

		return View();
	}

	[Authorize]
	public async Task<IActionResult> Logout()
	{
		await HttpContext.SignOutAsync(
			CookieAuthenticationDefaults.AuthenticationScheme);

		return RedirectToAction(
			nameof(Login));
	}

	[AllowAnonymous]
	public IActionResult AccessDenied()
	{
		return View();
	}
}