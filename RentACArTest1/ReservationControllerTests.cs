using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using RentACar.Controllers;
using RentACar.Data;
using RentACar.Models;

namespace RentACArTest1
{
    public class Tests
    {
        private DbContextOptions<ApplicationDbContext> CreateOptions(string name)
        {
            return new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(name)
                .Options;
        }

        private static ControllerContext CreateContext(string? username = null, bool isAdmin = false)
        {
            var claims = new List<Claim>();

            if (username != null)
                claims.Add(new Claim(ClaimTypes.Name, username));

            if (isAdmin)
                claims.Add(new Claim(ClaimTypes.Role, "Admin"));

            return new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"))
                }
            };
        }

        private static readonly DateTime now = new DateTime(2024, 1, 1);

        // ================= MY =================

        [Test]
        public async Task My_Returns_Challenge_When_Not_Logged()
        {
            var ctx = new ApplicationDbContext(CreateOptions(nameof(My_Returns_Challenge_When_Not_Logged)));

            var controller = new ReservationController(ctx)
            {
                ControllerContext = CreateContext()
            };

            var result = await controller.My();

            Assert.That(result, Is.InstanceOf<ChallengeResult>());
        }

        [Test]
        public async Task My_Returns_NotFound_When_User_Not_Exists()
        {
            var ctx = new ApplicationDbContext(CreateOptions(nameof(My_Returns_NotFound_When_User_Not_Exists)));

            var controller = new ReservationController(ctx)
            {
                ControllerContext = CreateContext("missing")
            };

            var result = await controller.My();

            Assert.That(result, Is.InstanceOf<NotFoundResult>());
        }

        [Test]
        public async Task My_Returns_User_Reservations()
        {
            var options = CreateOptions(nameof(My_Returns_User_Reservations));

            await using (var ctx = new ApplicationDbContext(options))
            {
                ctx.Users.Add(new User
                {
                    Id = "1",
                    UserName = "u1",
                    PasswordHash = "p",
                    FirstName = "F",
                    LastName = "L",
                    EGN = "123",
                    Email = "a@a.com"
                });

                ctx.Cars.Add(new Car
                {
                    Id = "1",
                    Brand = "BMW",
                    Model = "X5",
                    Year = 2020,
                    SeatingCapacity = 5,
                    DailyPrice = 50
                });

                ctx.Reservations.Add(new Reservation
                {
                    Id = "1",
                    UserId = "1",
                    CarId = "1",
                    StartDate = now,
                    EndDate = now.AddDays(1),
                    IsReserved = true
                });

                await ctx.SaveChangesAsync();
            }

            await using (var ctx = new ApplicationDbContext(options))
            {
                var controller = new ReservationController(ctx)
                {
                    ControllerContext = CreateContext("u1")
                };

                var result = await controller.My();

                Assert.That(result, Is.InstanceOf<ViewResult>());
            }
        }

        // ================= ALL =================

        [Test]
        public async Task All_Returns_All_For_Admin()
        {
            var options = CreateOptions(nameof(All_Returns_All_For_Admin));

            await using (var ctx = new ApplicationDbContext(options))
            {
                ctx.Reservations.Add(new Reservation { Id = "1", StartDate = now });
                ctx.Reservations.Add(new Reservation { Id ="2", StartDate = now.AddDays(-1) });
                await ctx.SaveChangesAsync();
            }

            await using (var ctx = new ApplicationDbContext(options))
            {
                var controller = new ReservationController(ctx)
                {
                    ControllerContext = CreateContext("admin", true)
                };

                var result = await controller.All();

                Assert.That(result, Is.InstanceOf<ViewResult>());
            }
        }

        // ================= CREATE GET =================

        [Test]
        public async Task Create_Get_Returns_NotFound_When_Invalid()
        {
            var ctx = new ApplicationDbContext(CreateOptions(nameof(Create_Get_Returns_NotFound_When_Invalid)));

            var controller = new ReservationController(ctx);

            var result1 = await controller.Create((int?)null);
            var result2 = await controller.Create((int?)999);

            Assert.That(result1, Is.InstanceOf<NotFoundResult>());
            Assert.That(result2, Is.InstanceOf<NotFoundResult>());
        }

        [Test]
        public async Task Create_Get_Returns_View_When_Car_Exists()
        {
            var options = CreateOptions(nameof(Create_Get_Returns_View_When_Car_Exists));

            await using (var ctx = new ApplicationDbContext(options))
            {
                ctx.Cars.Add(new Car
                {
                    Id = "1",
                    Brand = "Audi",
                    Model = "A4",
                    Year = 2020,
                    SeatingCapacity = 5,
                    DailyPrice = 60
                });
                await ctx.SaveChangesAsync();
            }

            await using (var ctx = new ApplicationDbContext(options))
            {
                var controller = new ReservationController(ctx);

                var result = await controller.Create((int?)1);

                Assert.That(result, Is.InstanceOf<ViewResult>());
            }
        }

        // ================= CREATE POST =================

        [Test]
        public async Task Create_Post_Returns_Challenge_When_Not_Logged()
        {
            var ctx = new ApplicationDbContext(CreateOptions(nameof(Create_Post_Returns_Challenge_When_Not_Logged)));

            var controller = new ReservationController(ctx)
            {
                ControllerContext = CreateContext()
            };

            var result = await controller.Create(new Reservation());

            Assert.That(result, Is.InstanceOf<ChallengeResult>());
        }

        [Test]
        public async Task Create_Post_Success()
        {
            var options = CreateOptions(nameof(Create_Post_Success));

            await using (var ctx = new ApplicationDbContext(options))
            {
                ctx.Users.Add(new User
                {
                    Id = "1",
                    UserName = "user",
                    PasswordHash = "p",
                    FirstName = "F",
                    LastName = "L",
                    EGN = "123",
                    Email = "a@a.com"
                });

                ctx.Cars.Add(new Car
                {
                    Id = "1",
                    Brand = "BMW",
                    Model = "X5",
                    Year = 2020,
                    SeatingCapacity = 5,
                    DailyPrice = 50
                });

                await ctx.SaveChangesAsync();
            }

            await using (var ctx = new ApplicationDbContext(options))
            {
                var controller = new ReservationController(ctx)
                {
                    ControllerContext = CreateContext("user")
                };

                var model = new Reservation
                {
                    CarId = "1",
                    StartDate = now,
                    EndDate = now.AddDays(1)
                };

                var result = await controller.Create(model);

                Assert.That(result, Is.InstanceOf<RedirectToActionResult>());
            }
        }

        // ================= DELETE =================

        [Test]
        public async Task Delete_Returns_NotFound_When_Missing()
        {
            var ctx = new ApplicationDbContext(CreateOptions(nameof(Delete_Returns_NotFound_When_Missing)));

            var controller = new ReservationController(ctx)
            {
                ControllerContext = CreateContext("admin", true)
            };

            var result = await controller.Delete(999);

            Assert.That(result, Is.InstanceOf<NotFoundResult>());
        }

        [Test]
        public async Task Delete_Removes_Reservation()
        {
            var options = CreateOptions(nameof(Delete_Removes_Reservation));

            await using (var ctx = new ApplicationDbContext(options))
            {
                ctx.Reservations.Add(new Reservation
                {
                    Id = "1",
                    UserId = "1",
                    CarId = "1",
                    StartDate = now,
                    EndDate = now.AddDays(1),
                    IsReserved = true
                });

                await ctx.SaveChangesAsync();
            }

            await using (var ctx = new ApplicationDbContext(options))
            {
                var controller = new ReservationController(ctx)
                {
                    ControllerContext = CreateContext("admin", true)
                };

                var result = await controller.Delete(1);

                Assert.That(result, Is.InstanceOf<RedirectToActionResult>());

                var exists = await ctx.Reservations.FindAsync(1);
                Assert.That(exists, Is.Null);
            }
        }
    }
}