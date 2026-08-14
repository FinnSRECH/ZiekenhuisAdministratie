using Hospital.Admin.Services;

namespace Hospital.Admin
{
	public class Program
	{
		public static void Main(string[] args)
		{
			var builder = WebApplication.CreateBuilder(args);

			// MVC toevoegen
			builder.Services.AddControllersWithViews();

			// Gedeelde ziekenhuisdata beschikbaar maken
			builder.Services.AddSingleton<HospitalDataService>();

			var app = builder.Build();

			if (!app.Environment.IsDevelopment())
			{
				app.UseExceptionHandler("/Home/Error");
				app.UseHsts();
			}

			app.UseHttpsRedirection();
			app.UseRouting();

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