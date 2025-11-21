using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Models;

namespace WebApplication1
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // ------------------------------------------
            // 1) Add MVC
            // ------------------------------------------
            builder.Services.AddControllersWithViews();

            // ------------------------------------------
            // 2) Add SQL Server via appsettings.json
            // ------------------------------------------
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(
                    builder.Configuration.GetConnectionString("DefaultConnection")
                ));

            // ------------------------------------------
            // 3) Add Authentication (Cookies)
            // ------------------------------------------
            builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
                {
                    options.LoginPath = "/Account/Login";
                    options.AccessDeniedPath = "/Account/AccessDenied";
                });

            // ------------------------------------------
            // 4) Add Session support
            // ------------------------------------------
            builder.Services.AddDistributedMemoryCache();
            builder.Services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromHours(4);
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
            });

            // ------------------------------------------
            // 5) FileProtector (your existing service)
            // ------------------------------------------
            builder.Services.AddSingleton<IFileProtector, FileProtector>();

            var app = builder.Build();

            // ------------------------------------------
            // 6) Ensure DB exists and seed base HR user
            // ------------------------------------------
            using (var scope = app.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                db.Database.EnsureCreated();

                // SEED HR USER IF MISSING
                if (!db.Users.Any(u => u.Role == "HR"))
                {
                    var hr = new User
                    {
                        Name = "System",
                        Surname = "HR",
                        Email = "hr@example.com",
                        Role = "HR",
                        HourlyRate = 0,
                        PasswordHash = Hash("P@ssw0rd")
                    };

                    db.Users.Add(hr);
                    db.SaveChanges();
                }
            }

            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            // MUST be in this order:
            app.UseAuthentication();
            app.UseAuthorization();

            app.UseSession();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.Run();
        }

        // Simple SHA256 hashing helper for seeding HR
        private static string Hash(string password)
        {
            using var sha = System.Security.Cryptography.SHA256.Create();
            return Convert.ToHexString(sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password)));
        }
    }
}