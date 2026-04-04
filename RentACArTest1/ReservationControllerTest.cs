using System;
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
using System.Collections.Generic;

namespace RentACArTest1
{
    public class ReservationControllerTests
    {
        private DbContextOptions<ApplicationDbContext> CreateNewContextOptions(string dbName)
        {
            return new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;
        }

        private static ControllerContext CreateControllerContext(string? username = null, bool isAuthenticated = false)
        {
            var claims = new List<Claim>();
            if (username != null)
                claims.Add(new Claim(ClaimTypes.Name, username));

            var identity = new ClaimsIdentity(claims, isAuthenticated ? "Test" : null);
            var principal = new ClaimsPrincipal(identity);

            return new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };
        }

        private static User CreateUser(
            string id = "u1",
            string userName = "user1",
            string email = "a@a.com",
            string firstName = "F",
            string lastName = "L",
            string egn = "1234567890")
        {
            return new User
            {
                Id = id,
                UserName = userName,
                Email = email,
                FirstName = firstName,
                LastName = lastName,
                EGN = egn,
                PasswordHash = "pwd",
                EmailConfirmed = true,
                IsDeleted = false
            };
        }

        // ---------------- My ----------------

        [Test]
        public async Task My_Returns_Challenge_When_NotAuthenticated()
        {
            var options = CreateNewContextOptions(nameof(My_Returns_Challenge_When_NotAuthenticated));
            await using var ctx = new ApplicationDbContext(options);
            var controller = new ReservationController(ctx)
            {
                ControllerContext = CreateControllerContext(username: null, isAuthenticated: false)
            };

            var result = await controller.My();

            Assert.That(result, Is.InstanceOf<ChallengeResult>());
        }

        [Test]
        public async Task My_Returns_NotFound_When_UserMissing()
        {
            var options = CreateNewContextOptions(nameof(My_Returns_NotFound_When_UserMissing));
            await using var ctx = new ApplicationDbContext(options);
            var controller = new ReservationController(ctx)
            {
                ControllerContext = CreateControllerContext(username: "missing", isAuthenticated: true)
            };

            var result = await controller.My();

            Assert.That(result, Is.InstanceOf<NotFoundResult>());
        }

        [Test]
        public async Task My_Returns_View_With_UserReservations()
        {
            var options = CreateNewContextOptions(nameof(My_Returns_View_With_UserReservations));

            // seed user and reservations
            await using (var ctx = new ApplicationDbContext(options))
            {
                var user = CreateUser(id: "u10", userName: "u10");
                ctx.Users.Add(user);
                ctx.Cars.Add(new Car { Id = "c1", Brand = "B", Model = "M", Year = 2020, SeatingCapacity = 4, DailyPrice = 10m, IsDeleted = false });
                ctx.Reservations.Add(new Reservation
                {
                    Id = "r1",
                    UserId = user.Id,
                    CarId = "c1",
                    StartDate = DateTime.UtcNow.Date.AddDays(-1),
                    EndDate = DateTime.UtcNow.Date.AddDays(1),
                    IsReserved = true,
                    IsDeleted = false
                });
                await ctx.SaveChangesAsync();
            }

            await using (var ctx = new ApplicationDbContext(options))
            {
                var controller = new ReservationController(ctx)
                {
                    ControllerContext = CreateControllerContext(username: "u10", isAuthenticated: true)
                };

                var result = await controller.My();

                Assert.That(result, Is.InstanceOf<ViewResult>());
                var view = (ViewResult)result;
                Assert.That(view.Model, Is.InstanceOf<System.Collections.Generic.List<Reservation>>());
                var list = (List<Reservation>)view.Model;
                Assert.That(list.Count, Is.EqualTo(1));
                Assert.That(list[0].UserId, Is.EqualTo("u10"));
            }
        }

        // ---------------- Create ----------------

        [Test]
        public void CreateGet_Sets_CarId_And_Returns_View()
        {
            var options = CreateNewContextOptions(nameof(CreateGet_Sets_CarId_And_Returns_View));
            using var ctx = new ApplicationDbContext(options);
            var controller = new ReservationController(ctx);

            var result = controller.Create("carX");

            Assert.That(result, Is.InstanceOf<ViewResult>());
            var view = (ViewResult)result;
            Assert.That(view.Model, Is.InstanceOf<Reservation>());
            var model = (Reservation)view.Model;
            Assert.That(model.CarId, Is.EqualTo("carX"));
        }

        [Test]
        public async Task CreatePost_EndDateBeforeOrEqualStart_Returns_View_With_ModelError()
        {
            var options = CreateNewContextOptions(nameof(CreatePost_EndDateBeforeOrEqualStart_Returns_View_With_ModelError));
            await using var ctx = new ApplicationDbContext(options);
            var controller = new ReservationController(ctx);

            var model = new Reservation
            {
                CarId = "c1",
                StartDate = DateTime.UtcNow.Date.AddDays(2),
                EndDate = DateTime.UtcNow.Date.AddDays(1),
                IsReserved = true
            };

            var result = await controller.Create(model);

            Assert.That(result, Is.InstanceOf<ViewResult>());
            Assert.That(controller.ModelState.ContainsKey("EndDate"), Is.True);
        }

        [Test]
        public async Task CreatePost_When_CarBusy_Returns_View_With_ModelError()
        {
            var options = CreateNewContextOptions(nameof(CreatePost_When_CarBusy_Returns_View_With_ModelError));

            // seed user and an existing overlapping reservation
            await using (var ctx = new ApplicationDbContext(options))
            {
                var user = CreateUser(id: "u20", userName: "busyuser");
                ctx.Users.Add(user);
                ctx.Reservations.Add(new Reservation
                {
                    Id = "exist",
                    UserId = user.Id,
                    CarId = "busycar",
                    StartDate = DateTime.UtcNow.Date,
                    EndDate = DateTime.UtcNow.Date.AddDays(5),
                    IsReserved = true,
                    IsDeleted = false
                });
                await ctx.SaveChangesAsync();
            }

            await using (var ctx = new ApplicationDbContext(options))
            {
                var controller = new ReservationController(ctx)
                {
                    ControllerContext = CreateControllerContext(username: "busyuser", isAuthenticated: true)
                };

                var model = new Reservation
                {
                    CarId = "busycar",
                    StartDate = DateTime.UtcNow.Date.AddDays(1),
                    EndDate = DateTime.UtcNow.Date.AddDays(2),
                    IsReserved = true
                };

                var result = await controller.Create(model);

                Assert.That(result, Is.InstanceOf<ViewResult>());
                Assert.That(controller.ModelState[string.Empty].Errors.Count, Is.GreaterThan(0));
            }
        }

        [Test]
        public async Task CreatePost_Success_Adds_Reservation_And_Redirects()
        {
            var options = CreateNewContextOptions(nameof(CreatePost_Success_Adds_Reservation_And_Redirects));

            await using (var ctx = new ApplicationDbContext(options))
            {
                var user = CreateUser(id: "u30", userName: "u30");
                ctx.Users.Add(user);
                ctx.Cars.Add(new Car { Id = "car-ok", Brand = "OK", Model = "M", Year = 2021, SeatingCapacity = 4, DailyPrice = 20m, IsDeleted = false });
                await ctx.SaveChangesAsync();
            }

            await using (var ctx = new ApplicationDbContext(options))
            {
                var controller = new ReservationController(ctx)
                {
                    ControllerContext = CreateControllerContext(username: "u30", isAuthenticated: true)
                };

                var model = new Reservation
                {
                    CarId = "car-ok",
                    StartDate = DateTime.UtcNow.Date.AddDays(1),
                    EndDate = DateTime.UtcNow.Date.AddDays(3),
                    IsReserved = true
                };

                var result = await controller.Create(model);

                Assert.That(result, Is.InstanceOf<RedirectToActionResult>());
                var redirect = (RedirectToActionResult)result;
                Assert.That(redirect.ActionName, Is.EqualTo("My"));

                // verify persisted
                var exists = await ctx.Reservations.IgnoreQueryFilters().FirstOrDefaultAsync(r => r.UserId == "u30" && r.CarId == "car-ok");
                Assert.That(exists, Is.Not.Null);
                Assert.That(exists!.IsReserved, Is.True);
            }
        }

        // ---------------- Delete ----------------

        [Test]
        public async Task Delete_Returns_NotFound_When_Missing()
        {
            var options = CreateNewContextOptions(nameof(Delete_Returns_NotFound_When_Missing));
            await using var ctx = new ApplicationDbContext(options);
            var controller = new ReservationController(ctx);

            var result = await controller.Delete("missing");

            Assert.That(result, Is.InstanceOf<NotFoundResult>());
        }

        [Test]
        public async Task Delete_Sets_IsDeleted_And_Redirects()
        {
            var options = CreateNewContextOptions(nameof(Delete_Sets_IsDeleted_And_Redirects));
            await using (var ctx = new ApplicationDbContext(options))
            {
                ctx.Reservations.Add(new Reservation
                {
                    Id = "del1",
                    UserId = "uX",
                    CarId = "cX",
                    StartDate = DateTime.UtcNow.Date,
                    EndDate = DateTime.UtcNow.Date.AddDays(1),
                    IsReserved = true,
                    IsDeleted = false
                });
                await ctx.SaveChangesAsync();
            }

            await using (var ctx = new ApplicationDbContext(options))
            {
                var controller = new ReservationController(ctx);

                var result = await controller.Delete("del1");

                Assert.That(result, Is.InstanceOf<RedirectToActionResult>());
                var redirect = (RedirectToActionResult)result;
                Assert.That(redirect.ActionName, Is.EqualTo("All"));

                var raw = await ctx.Reservations.IgnoreQueryFilters().FirstOrDefaultAsync(r => r.Id == "del1");
                Assert.That(raw, Is.Not.Null);
                Assert.That(raw!.IsDeleted, Is.True);
            }
        }
    }
}
