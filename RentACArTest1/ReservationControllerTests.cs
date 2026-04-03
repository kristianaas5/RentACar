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
        private DbContextOptions<ApplicationDbContext> CreateNewContextOptions(string dbName)
        {
            return new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;
        }

        private static ControllerContext CreateControllerContext(string? username = null, bool isAdmin = false)
        {
            var claims = new List<Claim>();
            if (username != null)
            {
                claims.Add(new Claim(ClaimTypes.Name, username));
            }
            if (isAdmin)
            {
                claims.Add(new Claim(ClaimTypes.Role, "Admin"));
            }

            var identity = new ClaimsIdentity(claims, username == null ? "" : "Test");
            var principal = new ClaimsPrincipal(identity);

            return new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };
        }

        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public async Task My_Returns_Challenge_When_User_Not_Authenticated()
        {
            var options = CreateNewContextOptions(nameof(My_Returns_Challenge_When_User_Not_Authenticated));
            await using var ctx = new ApplicationDbContext(options);
            var controller = new ReservationController(ctx)
            {
                ControllerContext = CreateControllerContext(username: null)
            };

            var result = await controller.My();

            Assert.That(result, Is.InstanceOf<ChallengeResult>());
        }

        [Test]
        public async Task My_Returns_NotFound_When_User_Not_In_Db()
        {
            var options = CreateNewContextOptions(nameof(My_Returns_NotFound_When_User_Not_In_Db));
            await using var ctx = new ApplicationDbContext(options);
            var controller = new ReservationController(ctx)
            {
                ControllerContext = CreateControllerContext(username: "missing")
            };

            var result = await controller.My();

            Assert.That(result, Is.InstanceOf<NotFoundResult>());
        }

        [Test]
        public async Task My_Returns_Reservations_For_User_Ordered()
        {
            var options = CreateNewContextOptions(nameof(My_Returns_Reservations_For_User_Ordered));
            await using (var ctx = new ApplicationDbContext(options))
            {
                var user = new User { Id = 1, UserName = "u1", PasswordHash = "p", FirstName = "F", LastName = "L", EGN = "1234567890", Email = "a@b.com" };
                var car = new Car { Id = 1, Brand = "B", Model = "M", Year = 2000, SeatingCapacity = 4, DailyPrice = 10m };
                ctx.Users.Add(user);
                ctx.Cars.Add(car);
                ctx.Reservations.AddRange(
                    new Reservation { Id = 1, UserId = user.Id, CarId = car.Id, StartDate = DateTime.UtcNow.AddDays(-1), EndDate = DateTime.UtcNow, IsReserved = true },
                    new Reservation { Id = 2, UserId = user.Id, CarId = car.Id, StartDate = DateTime.UtcNow.AddDays(-2), EndDate = DateTime.UtcNow.AddDays(-1), IsReserved = true }
                );
                await ctx.SaveChangesAsync();
            }

            await using (var ctx = new ApplicationDbContext(options))
            {
                var controller = new ReservationController(ctx)
                {
                    ControllerContext = CreateControllerContext(username: "u1")
                };

                var result = await controller.My();

                Assert.That(result, Is.InstanceOf<ViewResult>());
                var view = (ViewResult)result;
                Assert.That(view.Model, Is.InstanceOf<List<Reservation>>());
                var model = (List<Reservation>)view.Model;
                Assert.That(model.Count, Is.EqualTo(2));
                Assert.That(model[0].StartDate, Is.GreaterThanOrEqualTo(model[1].StartDate));
                Assert.That(model.All(r => r.UserId == 1), Is.True);
            }
        }

        [Test]
        public async Task All_Returns_All_Reservations_For_Admin()
        {
            var options = CreateNewContextOptions(nameof(All_Returns_All_Reservations_For_Admin));
            await using (var ctx = new ApplicationDbContext(options))
            {
                var user1 = new User { Id = 1, Username = "u1", PasswordHash = "p", FirstName = "F", LastName = "L", EGN = "1234567890", Email = "a@b.com" };
                var user2 = new User { Id = 2, Username = "u2", PasswordHash = "p", FirstName = "F", LastName = "L", EGN = "1111111111", Email = "c@d.com" };
                var car = new Car { Id = 1, Brand = "B", Model = "M", Year = 2000, SeatingCapacity = 4, DailyPrice = 10m };
                ctx.Users.AddRange(user1, user2);
                ctx.Cars.Add(car);
                ctx.Reservations.AddRange(
                    new Reservation { Id = 1, UserId = user1.Id, CarId = car.Id, StartDate = DateTime.UtcNow.AddDays(-1), EndDate = DateTime.UtcNow, IsReserved = true },
                    new Reservation { Id = 2, UserId = user2.Id, CarId = car.Id, StartDate = DateTime.UtcNow.AddDays(-2), EndDate = DateTime.UtcNow.AddDays(-1), IsReserved = true }
                );
                await ctx.SaveChangesAsync();
            }

            await using (var ctx = new ApplicationDbContext(options))
            {
                var controller = new ReservationController(ctx)
                {
                    ControllerContext = CreateControllerContext(username: "admin", isAdmin: true)
                };

                var result = await controller.All();

                Assert.That(result, Is.InstanceOf<ViewResult>());
                var view = (ViewResult)result;
                Assert.That(view.Model, Is.InstanceOf<List<Reservation>>());
                var model = (List<Reservation>)view.Model;
                Assert.That(model.Count, Is.EqualTo(2));
                Assert.That(model[0].StartDate, Is.GreaterThanOrEqualTo(model[1].StartDate));
            }
        }

        [Test]
        public async Task CreateGet_Returns_NotFound_When_CarId_Null_Or_Missing()
        {
            var options = CreateNewContextOptions(nameof(CreateGet_Returns_NotFound_When_CarId_Null_Or_Missing));
            await using var ctx = new ApplicationDbContext(options);
            var controller = new ReservationController(ctx);

            var nullResult = await controller.Create((int?)null);
            Assert.That(nullResult, Is.InstanceOf<NotFoundResult>());

            var missingResult = await controller.Create((int?)999);
            Assert.That(missingResult, Is.InstanceOf<NotFoundResult>());
        }

        [Test]
        public async Task CreateGet_Returns_View_With_Default_Model_When_Car_Exists()
        {
            var options = CreateNewContextOptions(nameof(CreateGet_Returns_View_With_Default_Model_When_Car_Exists));
            await using (var ctx = new ApplicationDbContext(options))
            {
                ctx.Cars.Add(new Car { Id = 10, Brand = "B", Model = "M", Year = 2005, SeatingCapacity = 4, DailyPrice = 20m });
                await ctx.SaveChangesAsync();
            }

            await using (var ctx = new ApplicationDbContext(options))
            {
                var controller = new ReservationController(ctx);
                var result = await controller.Create((int?)10);

                Assert.That(result, Is.InstanceOf<ViewResult>());
                var view = (ViewResult)result;
                Assert.That(view.Model, Is.InstanceOf<Reservation>());
                var model = (Reservation)view.Model;
                Assert.That(model.CarId, Is.EqualTo(10));
                Assert.That(model.StartDate, Is.LessThan(model.EndDate));
            }
        }

        [Test]
        public async Task CreatePost_Returns_View_When_ModelState_Invalid()
        {
            var options = CreateNewContextOptions(nameof(CreatePost_Returns_View_When_ModelState_Invalid));
            await using var ctx = new ApplicationDbContext(options);
            var controller = new ReservationController(ctx);
            controller.ModelState.AddModelError("X", "err");

            var model = new Reservation { CarId = 1, StartDate = DateTime.UtcNow, EndDate = DateTime.UtcNow.AddDays(1) };
            var result = await controller.Create(model);

            Assert.That(result, Is.InstanceOf<ViewResult>());
            var view = (ViewResult)result;
            Assert.That(view.Model, Is.SameAs(model));
        }

        [Test]
        public async Task CreatePost_Returns_Challenge_When_User_Not_Authenticated()
        {
            var options = CreateNewContextOptions(nameof(CreatePost_Returns_Challenge_When_User_Not_Authenticated));
            await using var ctx = new ApplicationDbContext(options);
            var controller = new ReservationController(ctx)
            {
                ControllerContext = CreateControllerContext(username: null)
            };

            var model = new Reservation { CarId = 1, StartDate = DateTime.UtcNow, EndDate = DateTime.UtcNow.AddDays(1) };
            var result = await controller.Create(model);

            Assert.That(result, Is.InstanceOf<ChallengeResult>());
        }

        [Test]
        public async Task CreatePost_Returns_NotFound_When_User_Not_In_Db()
        {
            var options = CreateNewContextOptions(nameof(CreatePost_Returns_NotFound_When_User_Not_In_Db));
            await using var ctx = new ApplicationDbContext(options);
            var controller = new ReservationController(ctx)
            {
                ControllerContext = CreateControllerContext(username: "nouser")
            };

            var model = new Reservation { CarId = 1, StartDate = DateTime.UtcNow, EndDate = DateTime.UtcNow.AddDays(1) };
            var result = await controller.Create(model);

            Assert.That(result, Is.InstanceOf<NotFoundResult>());
        }

        [Test]
        public async Task CreatePost_Returns_View_When_Car_Is_Busy()
        {
            var options = CreateNewContextOptions(nameof(CreatePost_Returns_View_When_Car_Is_Busy));
            await using (var ctx = new ApplicationDbContext(options))
            {
                var user = new User { Id = 42, Username = "u", PasswordHash = "p", FirstName = "F", LastName = "L", EGN = "1234567890", Email = "a@b.com" };
                var car = new Car { Id = 5, Brand = "B", Model = "M", Year = 2010, SeatingCapacity = 4, DailyPrice = 30m };
                ctx.Users.Add(user);
                ctx.Cars.Add(car);
                // Existing reservation overlaps with requested period
                ctx.Reservations.Add(new Reservation
                {
                    Id = 100,
                    UserId = 42,
                    CarId = 5,
                    StartDate = DateTime.UtcNow,
                    EndDate = DateTime.UtcNow.AddDays(3),
                    IsReserved = true
                });
                await ctx.SaveChangesAsync();
            }

            await using (var ctx = new ApplicationDbContext(options))
            {
                var controller = new ReservationController(ctx)
                {
                    ControllerContext = CreateControllerContext(username: "u")
                };

                var model = new Reservation
                {
                    CarId = 5,
                    StartDate = DateTime.UtcNow.AddDays(1),
                    EndDate = DateTime.UtcNow.AddDays(2)
                };

                var result = await controller.Create(model);

                Assert.That(result, Is.InstanceOf<ViewResult>());
                var view = (ViewResult)result;
                Assert.That(controller.ModelState.IsValid, Is.False);
                // There is a global model error added with empty key in controller when busy
                Assert.That(controller.ModelState.ContainsKey(string.Empty), Is.True);
                Assert.That(controller.ModelState[string.Empty]!.Errors.Count, Is.GreaterThan(0));
            }
        }

        [Test]
        public async Task CreatePost_Success_Creates_Reservation_And_Redirects()
        {
            var options = CreateNewContextOptions(nameof(CreatePost_Success_Creates_Reservation_And_Redirects));
            await using (var ctx = new ApplicationDbContext(options))
            {
                var user = new User { Id = 7, Username = "success", PasswordHash = "p", FirstName = "F", LastName = "L", EGN = "1234567890", Email = "a@b.com" };
                var car = new Car { Id = 8, Brand = "B", Model = "M", Year = 2015, SeatingCapacity = 4, DailyPrice = 50m };
                ctx.Users.Add(user);
                ctx.Cars.Add(car);
                await ctx.SaveChangesAsync();
            }

            await using (var ctx = new ApplicationDbContext(options))
            {
                var controller = new ReservationController(ctx)
                {
                    ControllerContext = CreateControllerContext(username: "success")
                };

                var model = new Reservation
                {
                    CarId = 8,
                    StartDate = DateTime.UtcNow.AddDays(1),
                    EndDate = DateTime.UtcNow.AddDays(2)
                };

                var result = await controller.Create(model);

                Assert.That(result, Is.InstanceOf<RedirectToActionResult>());
                var redirect = (RedirectToActionResult)result;
                Assert.That(redirect.ActionName, Is.EqualTo("My"));

                var created = await ctx.Reservations.FirstOrDefaultAsync(r => r.CarId == 8 && r.UserId == 7);
                Assert.That(created, Is.Not.Null);
                Assert.That(created!.IsReserved, Is.True);
                Assert.That(created.StartDate, Is.EqualTo(model.StartDate));
            }
        }

        [Test]
        public async Task Delete_Returns_NotFound_When_Missing()
        {
            var options = CreateNewContextOptions(nameof(Delete_Returns_NotFound_When_Missing));
            await using var ctx = new ApplicationDbContext(options);
            var controller = new ReservationController(ctx)
            {
                ControllerContext = CreateControllerContext(username: "admin", isAdmin: true)
            };

            var result = await controller.Delete(999);
            Assert.That(result, Is.InstanceOf<NotFoundResult>());
        }

        [Test]
        public async Task Delete_Removes_Reservation_And_Redirects_When_Found()
        {
            var options = CreateNewContextOptions(nameof(Delete_Removes_Reservation_And_Redirects_When_Found));
            await using (var ctx = new ApplicationDbContext(options))
            {
                ctx.Reservations.Add(new Reservation { Id = 500, UserId = 1, CarId = 1, StartDate = DateTime.UtcNow, EndDate = DateTime.UtcNow.AddDays(1), IsReserved = true });
                await ctx.SaveChangesAsync();
            }

            await using (var ctx = new ApplicationDbContext(options))
            {
                var controller = new ReservationController(ctx)
                {
                    ControllerContext = CreateControllerContext(username: "admin", isAdmin: true)
                };

                var result = await controller.Delete(500);

                Assert.That(result, Is.InstanceOf<RedirectToActionResult>());
                var redirect = (RedirectToActionResult)result;
                Assert.That(redirect.ActionName, Is.EqualTo("All"));

                var exists = await ctx.Reservations.FindAsync(500);
                Assert.That(exists, Is.Null);
            }
        }
    }
}