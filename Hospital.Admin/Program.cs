using Hospital.Admin.Services;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace Hospital.Admin
{
	public class Program
	{
		public static void Main(string[] args)
		{
			var builder = WebApplication.CreateBuilder(args);

			builder.Services.AddControllersWithViews();

			// Services
			builder.Services.AddSingleton<HospitalDataService>();
			builder.Services.AddSingleton<PasswordService>();
			builder.Services.AddSingleton<PatientAccessService>();

			// Authenticatie
			builder.Services
				.AddAuthentication(
					CookieAuthenticationDefaults.AuthenticationScheme)
				.AddCookie(options =>
				{
					options.LoginPath = "/Account/Login";
					options.AccessDeniedPath = "/Account/AccessDenied";

					options.Cookie.Name =
						"Hospital.Admin.Auth";

					options.Cookie.HttpOnly = true;

					options.Cookie.SameSite =
						SameSiteMode.Lax;
				});

			builder.Services.AddAuthorization();

			var app = builder.Build();

			if (!app.Environment.IsDevelopment())
			{
				app.UseExceptionHandler("/Home/Error");
				app.UseHsts();
			}

			app.UseHttpsRedirection();
			app.UseRouting();

			app.UseAuthentication();
			app.UseAuthorization();

			app.MapStaticAssets();

			app.MapControllerRoute(
				name: "default",
				pattern: "{controller=Home}/{action=Index}/{id?}")
				.WithStaticAssets();

			app.Run();
		}
	}
}