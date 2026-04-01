using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RentACar.Models;
using System.Data;

namespace RentACar.Data
{
    public class SeedData
    {

        public static async Task Initialize(
                IServiceProvider serviceProvider,
                UserManager<User> userManager,
                RoleManager<IdentityRole> roleManager)
        {
            string[] roleNames = { "Admin", "User" };
            // Create roles if they do not exist
            foreach (var roleName in roleNames)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                    Console.WriteLine($"✓ Роля '{roleName}' създадена");
                }
            }

            //Create admin user
            var adminEmail = "admin@eventures.com";
            var adminUser = await userManager.FindByEmailAsync(adminEmail);

            if (adminUser == null)
            {
                // Set required properties FirstName, LastName, and EGN in the User object initializer
                var admin = new User
                {
                    UserName = "Admin",
                    Email = adminEmail,
                    EmailConfirmed = true,
                    FirstName = "Admin",
                    LastName = "User",
                    EGN = "0000000000" // Replace with a valid EGN if needed
                };

                var result = await userManager.CreateAsync(admin, "Admin123.");

                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(admin, "Admin");

                    Console.WriteLine(new string('=', 24));
                    Console.WriteLine(" ADMIN ПОТРЕБИТЕЛ СЪЗДАДЕН!");
                    Console.WriteLine($"  Email:    {adminEmail}");
                    Console.WriteLine("  Username: Admin");
                    Console.WriteLine("  Password: Admin123.");
                    Console.WriteLine(new string('=', 24));
                }
            }
        }
        public static async Task SeedCars(ApplicationDbContext context)
        {
            if (await context.Cars.AnyAsync())// If there are already authors in the database, skip seeding.
                return;
            var cars = new List<Car>
            {
                new Car
                {
                    Brand = "BMW",
                    Model = "X5",
                    Year = 2020,
                    SeatingCapacity = 5,
                    Description = "Luxury SUV with AWD and automatic transmission",
                    DailyPrice = 120.00m,
                    IsDeleted = false
                },
            new Car
            {
                Brand = "Audi",
                Model = "A4",
                Year = 2019,
                SeatingCapacity = 5,
                Description = "Comfortable sedan with great fuel efficiency",
                DailyPrice = 90.00m,
                IsDeleted = false
            },
            new Car
            {

                Brand = "Mercedes",
                Model = "C-Class",
                Year = 2021,
                SeatingCapacity = 5,
                Description = "Premium sedan with modern features",
                DailyPrice = 130.00m,
                IsDeleted = false
            },
            new Car
            {
                Brand = "Toyota",
                Model = "Corolla",
                Year = 2018,
                SeatingCapacity = 5,
                Description = "Reliable and economical car",
                DailyPrice = 70.00m,
                IsDeleted = false
            }
            };
            context.Cars.AddRange(cars);
            await context.SaveChangesAsync();
            Console.WriteLine("✓ Автомобили добавени в базата данни");
        }
    }
}
