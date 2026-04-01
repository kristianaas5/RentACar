using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RentACar.Data;
using RentACar.Models;

namespace RentACar
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(connectionString));
            builder.Services.AddDatabaseDeveloperPageExceptionFilter();
            builder.Services.AddDefaultIdentity<User>(options =>
            {
                // Standard password settings   
                options.Password.RequireDigit = true;       //Enquires at least one digit (0-9) in the password
                options.Password.RequireLowercase = true;       //Enquires at least one lowercase letter (a-z) in the password
                options.Password.RequireUppercase = true;       //Enquires at least one uppercase letter (A-Z) in the password
                options.Password.RequireNonAlphanumeric = true; //Enquires at least one non-alphanumeric character (e.g., !, @, #, etc.) in the password
                options.Password.RequiredLength = 3;            // Minimum length of the password (in this case, 3 characters)

                // Standard user settings
                options.User.RequireUniqueEmail = true;  // Email must be unique for each user

                // Standard lockout settings
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5); // Lockout duration of 5 minutes
                options.Lockout.MaxFailedAccessAttempts = 5;  // After 5 failed attempts, the user will be locked out
                options.Lockout.AllowedForNewUsers = true; // Lockout is enabled for new users

                // SignIn settings
                options.SignIn.RequireConfirmedEmail = false;  // Does not require email confirmation for sign-in
            })
                .AddRoles<IdentityRole>()  // Add support for roles in Identity
                .AddEntityFrameworkStores<ApplicationDbContext>() // Use Entity Framework Core for storing Identity data
                .AddDefaultTokenProviders();// Add default token providers for password reset, email confirmation, etc.

            builder.Services.AddControllersWithViews(); // Add support for MVC controllers and views

            // Set up cookie settings for authentication
            builder.Services.ConfigureApplicationCookie(options =>
            {
                // Path for Login
                options.LoginPath = "/Account/Login";

                // Path за Logout
                options.LogoutPath = "/Account/Logout";

                // Path for AccessDenied (when user tries to access a resource they don't have permission for)
                options.AccessDeniedPath = "/Account/AccessDenied";

                // Cookie settings for security
                options.Cookie.HttpOnly = true;  // Secure cookie, not accessible via JavaScript
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;  // Only sent over HTTPS
                options.Cookie.SameSite = SameSiteMode.Strict;  // CSRF security

                // Time settings for cookie expiration and sliding expiration
                options.ExpireTimeSpan = TimeSpan.FromHours(2);  // Cookie is valid for 2 hours
                options.SlidingExpiration = true;  // If the user is active, the expiration time will be reset with each request
            });


            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseMigrationsEndPoint();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }
            app.UseStaticFiles();
            app.UseHttpsRedirection();
            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapStaticAssets();
            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");
                //.WithStaticAssets();
            app.MapRazorPages()
               .WithStaticAssets();

            using (var scope = app.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                try
                {
                    var userManager = services.GetRequiredService<UserManager<User>>();// Get the UserManager service to manage user accounts
                    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();// Get the RoleManager service to manage roles

                    await SeedData.Initialize(services, userManager, roleManager);// Seed initial data for roles and an admin user
                }
                catch (Exception ex)
                {
                    var logger = services.GetRequiredService<ILogger<Program>>();
                    logger.LogError(ex, "Грешка при seed на данните");// Log any exceptions that occur during the seeding process
                }
            }
            using (var scope = app.Services.CreateScope())
            {
                var services = scope.ServiceProvider;// Get the ApplicationDbContext service to interact with the database
                var context = services.GetRequiredService<ApplicationDbContext>();// Seed initial data for authors and genres in the database

                await SeedData.SeedCars(context);
            }

            app.Run();
        }
    }
}
