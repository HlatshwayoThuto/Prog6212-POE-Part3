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

            // Add services to the container.
            builder.Services.AddControllersWithViews();

            // Add EF Core (SQLite) - database file stored in App_Data/app.db
            var conn = $"Data Source={Path.Combine(builder.Environment.ContentRootPath, "App_Data", "app.db")}";
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlite(conn));

            // Add Authentication (Cookies)
            builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
                {
                    options.LoginPath = "/Account/Login";
                    options.AccessDeniedPath = "/Account/AccessDenied";
                });

            // Add Session (required by the spec)
            builder.Services.AddDistributedMemoryCache();
            builder.Services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromHours(4);
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
            });

            // Register FileProtector (keep your implementation)
            builder.Services.AddSingleton<IFileProtector, FileProtector>();

            var app = builder.Build();

            // Ensure EF database exists (no migrations required)
            using (var scope = app.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                db.Database.EnsureCreated();

                // -----------------------------
                // SEED HR USER IF NOT PRESENT
                // -----------------------------
                if (!db.Users.Any(u => u.Role == "HR"))
                {
                    var hr = new User
                    {
                        Name = "System",
                        Surname = "HR",
                        Email = "hr@example.com",
                        Role = "HR",
                        HourlyRate = 0,
                        PasswordHash = Convert.ToHexString(
                            System.Security.Cryptography.SHA256.Create()
                            .ComputeHash(System.Text.Encoding.UTF8.GetBytes("P@ssw0rd"))
                        )
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

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseSession(); // session middleware

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.Run();
        }
    }
}