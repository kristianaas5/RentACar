using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
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
    public class CarsControllerTests
    {
        private DbContextOptions<ApplicationDbContext> CreateNewContextOptions(string dbName)
        {
            return new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;
        }

        // A small derived context used to simulate SaveChangesAsync throwing.
        private class ThrowingSaveChangesContext : ApplicationDbContext
        {
            public ThrowingSaveChangesContext(DbContextOptions<ApplicationDbContext> options)
                : base(options)
            {
            }

            public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
            {
                throw new Exception("Simulated failure during SaveChangesAsync");
            }
        }

        // ---------------- All ----------------

        [Test]
        public async Task All_Returns_View_With_Cars_Ordered()
        {
            var options = CreateNewContextOptions(nameof(All_Returns_View_With_Cars_Ordered));

            await using (var ctx = new ApplicationDbContext(options))
            {
                ctx.Cars.AddRange(
                    new Car { Id = "c1", Brand = "Zeta", Model = "M2", Year = 2000, SeatingCapacity = 4, DailyPrice = 10m, IsDeleted = false },
                    new Car { Id = "c2", Brand = "Alpha", Model = "A1", Year = 2010, SeatingCapacity = 4, DailyPrice = 20m, IsDeleted = false }
                );
                await ctx.SaveChangesAsync();
            }

            await using (var ctx = new ApplicationDbContext(options))
            {
                var controller = new CarsController(ctx);

                var result = await controller.All();

                Assert.That(result, Is.InstanceOf<ViewResult>());
                var view = (ViewResult)result;
                Assert.That(view.Model, Is.InstanceOf<List<Car>>());
                var list = (List<Car>)view.Model;
                Assert.That(list.Count, Is.EqualTo(2));
                // Ordered by Brand then Model -> "Alpha" first
                Assert.That(list[0].Brand, Is.EqualTo("Alpha"));
            }
        }

        // ---------------- Details ----------------

        [Test]
        public async Task Details_NullId_Returns_NotFound()
        {
            var options = CreateNewContextOptions(nameof(Details_NullId_Returns_NotFound));
            await using var ctx = new ApplicationDbContext(options);
            var controller = new CarsController(ctx);

            var result = await controller.Details(null);

            Assert.That(result, Is.InstanceOf<NotFoundResult>());
        }

        [Test]
        public async Task Details_Missing_Returns_NotFound()
        {
            var options = CreateNewContextOptions(nameof(Details_Missing_Returns_NotFound));
            await using var ctx = new ApplicationDbContext(options);
            var controller = new CarsController(ctx);

            var result = await controller.Details("missing");

            Assert.That(result, Is.InstanceOf<NotFoundResult>());
        }

        [Test]
        public async Task Details_Returns_View_When_Found()
        {
            var options = CreateNewContextOptions(nameof(Details_Returns_View_When_Found));
            await using (var ctx = new ApplicationDbContext(options))
            {
                ctx.Cars.Add(new Car { Id = "c10", Brand = "B", Model = "M", Year = 2005, SeatingCapacity = 4, DailyPrice = 15m, IsDeleted = false });
                await ctx.SaveChangesAsync();
            }

            await using (var ctx = new ApplicationDbContext(options))
            {
                var controller = new CarsController(ctx);

                var result = await controller.Details("c10");

                Assert.That(result, Is.InstanceOf<ViewResult>());
                var view = (ViewResult)result;
                Assert.That(view.Model, Is.InstanceOf<Car>());
                var car = (Car)view.Model;
                Assert.That(car.Id, Is.EqualTo("c10"));
            }
        }

        // ---------------- Create ----------------

        [Test]
        public void CreateGet_Returns_View()
        {
            var options = CreateNewContextOptions(nameof(CreateGet_Returns_View));
            using var ctx = new ApplicationDbContext(options);
            var controller = new CarsController(ctx);

            var result = controller.Create();

            Assert.That(result, Is.InstanceOf<ViewResult>());
        }

        [Test]
        public async Task CreatePost_Returns_View_When_ModelState_Invalid()
        {
            var options = CreateNewContextOptions(nameof(CreatePost_Returns_View_When_ModelState_Invalid));
            await using var ctx = new ApplicationDbContext(options);
            var controller = new CarsController(ctx);
            controller.ModelState.AddModelError("x", "err");

            var car = new Car { Brand = "B", Model = "M", Year = 2000, SeatingCapacity = 4, DailyPrice = 10m };

            var result = await controller.Create(car);

            Assert.That(result, Is.InstanceOf<ViewResult>());
            var view = (ViewResult)result;
            Assert.That(view.Model, Is.InstanceOf<Car>());
        }

        [Test]
        public async Task CreatePost_Success_Adds_Car_And_Redirects()
        {
            var options = CreateNewContextOptions(nameof(CreatePost_Success_Adds_Car_And_Redirects));
            await using var ctx = new ApplicationDbContext(options);
            var controller = new CarsController(ctx);

            var car = new Car { Id = "new1", Brand = "New", Model = "X", Year = 2020, SeatingCapacity = 4, DailyPrice = 30m, IsDeleted = false };

            var result = await controller.Create(car);

            Assert.That(result, Is.InstanceOf<RedirectToActionResult>());
            var redirect = (RedirectToActionResult)result;
            Assert.That(redirect.ActionName, Is.EqualTo("All"));

            var exists = await ctx.Cars.FindAsync("new1");
            Assert.That(exists, Is.Not.Null);
            Assert.That(exists!.Brand, Is.EqualTo("New"));
        }

        // ---------------- Edit ----------------

        [Test]
        public async Task EditGet_NullId_Returns_NotFound()
        {
            var options = CreateNewContextOptions(nameof(EditGet_NullId_Returns_NotFound));
            await using var ctx = new ApplicationDbContext(options);
            var controller = new CarsController(ctx);

            var result = await controller.Edit(null);

            Assert.That(result, Is.InstanceOf<NotFoundResult>());
        }

        [Test]
        public async Task EditGet_Missing_Returns_NotFound()
        {
            var options = CreateNewContextOptions(nameof(EditGet_Missing_Returns_NotFound));
            await using var ctx = new ApplicationDbContext(options);
            var controller = new CarsController(ctx);

            var result = await controller.Edit("missing");

            Assert.That(result, Is.InstanceOf<NotFoundResult>());
        }

        [Test]
        public async Task EditGet_Returns_View_When_Found()
        {
            var options = CreateNewContextOptions(nameof(EditGet_Returns_View_When_Found));
            await using (var ctx = new ApplicationDbContext(options))
            {
                ctx.Cars.Add(new Car { Id = "e1", Brand = "B", Model = "M", Year = 2010, SeatingCapacity = 4, DailyPrice = 12m, IsDeleted = false });
                await ctx.SaveChangesAsync();
            }

            await using (var ctx = new ApplicationDbContext(options))
            {
                var controller = new CarsController(ctx);

                var result = await controller.Edit("e1");

                Assert.That(result, Is.InstanceOf<ViewResult>());
                var view = (ViewResult)result;
                Assert.That(view.Model, Is.InstanceOf<Car>());
                var car = (Car)view.Model;
                Assert.That(car.Id, Is.EqualTo("e1"));
            }
        }

        [Test]
        public async Task EditPost_IdMismatch_Returns_NotFound()
        {
            var options = CreateNewContextOptions(nameof(EditPost_IdMismatch_Returns_NotFound));
            await using var ctx = new ApplicationDbContext(options);
            var controller = new CarsController(ctx);

            var car = new Car { Id = "x1", Brand = "B", Model = "M", Year = 2010, SeatingCapacity = 4, DailyPrice = 12m };

            var result = await controller.Edit("other", car);

            Assert.That(result, Is.InstanceOf<NotFoundResult>());
        }

        [Test]
        public async Task EditPost_ModelStateInvalid_Returns_View()
        {
            var options = CreateNewContextOptions(nameof(EditPost_ModelStateInvalid_Returns_View));
            await using var ctx = new ApplicationDbContext(options);
            var controller = new CarsController(ctx);
            controller.ModelState.AddModelError("x", "err");

            var car = new Car { Id = "x2", Brand = "B", Model = "M", Year = 2010, SeatingCapacity = 4, DailyPrice = 12m };

            var result = await controller.Edit("x2", car);

            Assert.That(result, Is.InstanceOf<ViewResult>());
            var view = (ViewResult)result;
            Assert.That(view.Model, Is.InstanceOf<Car>());
        }

        [Test]
        public async Task EditPost_Success_Updates_And_Redirects()
        {
            var options = CreateNewContextOptions(nameof(EditPost_Success_Updates_And_Redirects));
            await using (var ctx = new ApplicationDbContext(options))
            {
                ctx.Cars.Add(new Car { Id = "upd1", Brand = "Old", Model = "M", Year = 2010, SeatingCapacity = 4, DailyPrice = 12m, IsDeleted = false });
                await ctx.SaveChangesAsync();
            }

            await using (var ctx = new ApplicationDbContext(options))
            {
                var controller = new CarsController(ctx);

                var updated = new Car { Id = "upd1", Brand = "NewBrand", Model = "M", Year = 2010, SeatingCapacity = 4, DailyPrice = 15m, IsDeleted = false };

                var result = await controller.Edit("upd1", updated);

                Assert.That(result, Is.InstanceOf<RedirectToActionResult>());
                var redirect = (RedirectToActionResult)result;
                Assert.That(redirect.ActionName, Is.EqualTo("All"));

                var persisted = await ctx.Cars.FindAsync("upd1");
                Assert.That(persisted, Is.Not.Null);
                Assert.That(persisted!.Brand, Is.EqualTo("NewBrand"));
                Assert.That(persisted.DailyPrice, Is.EqualTo(15m));
            }
        }

        // NEW: simulate SaveChangesAsync throwing to hit catch branch in Edit POST
        [Test]
        public async Task EditPost_SaveChangesThrows_Returns_NotFound()
        {
            var options = CreateNewContextOptions(nameof(EditPost_SaveChangesThrows_Returns_NotFound));

            // seed data using a normal context
            await using (var seed = new ApplicationDbContext(options))
            {
                seed.Cars.Add(new Car { Id = "t1", Brand = "Old", Model = "M", Year = 2010, SeatingCapacity = 4, DailyPrice = 10m, IsDeleted = false });
                await seed.SaveChangesAsync();
            }

            // use a context that throws on SaveChangesAsync
            await using (var throwing = new ThrowingSaveChangesContext(options))
            {
                var controller = new CarsController(throwing);

                var updated = new Car { Id = "t1", Brand = "BrokenUpdate", Model = "M", Year = 2010, SeatingCapacity = 4, DailyPrice = 11m, IsDeleted = false };

                var result = await controller.Edit("t1", updated);

                Assert.That(result, Is.InstanceOf<NotFoundResult>());
            }
        }

        // ---------------- Delete ----------------

        [Test]
        public async Task DeleteGet_NullId_Returns_NotFound()
        {
            var options = CreateNewContextOptions(nameof(DeleteGet_NullId_Returns_NotFound));
            await using var ctx = new ApplicationDbContext(options);
            var controller = new CarsController(ctx);

            var result = await controller.Delete(null);

            Assert.That(result, Is.InstanceOf<NotFoundResult>());
        }

        [Test]
        public async Task DeleteGet_Missing_Returns_NotFound()
        {
            var options = CreateNewContextOptions(nameof(DeleteGet_Missing_Returns_NotFound));
            await using var ctx = new ApplicationDbContext(options);
            var controller = new CarsController(ctx);

            var result = await controller.Delete("missing");

            Assert.That(result, Is.InstanceOf<NotFoundResult>());
        }

        [Test]
        public async Task DeleteGet_Returns_View_When_Found()
        {
            var options = CreateNewContextOptions(nameof(DeleteGet_Returns_View_When_Found));
            await using (var ctx = new ApplicationDbContext(options))
            {
                ctx.Cars.Add(new Car { Id = "d1", Brand = "B", Model = "M", Year = 2010, SeatingCapacity = 4, DailyPrice = 12m, IsDeleted = false });
                await ctx.SaveChangesAsync();
            }

            await using (var ctx = new ApplicationDbContext(options))
            {
                var controller = new CarsController(ctx);

                var result = await controller.Delete("d1");

                Assert.That(result, Is.InstanceOf<ViewResult>());
                var view = (ViewResult)result;
                Assert.That(view.Model, Is.InstanceOf<Car>());
                var car = (Car)view.Model;
                Assert.That(car.Id, Is.EqualTo("d1"));
            }
        }

        [Test]
        public async Task DeleteConfirmed_Missing_Returns_NotFound()
        {
            var options = CreateNewContextOptions(nameof(DeleteConfirmed_Missing_Returns_NotFound));
            await using var ctx = new ApplicationDbContext(options);
            var controller = new CarsController(ctx);

            var result = await controller.DeleteConfirmed("missing");

            Assert.That(result, Is.InstanceOf<NotFoundResult>());
        }

        [Test]
        public async Task DeleteConfirmed_Sets_IsDeleted_And_Redirects()
        {
            var options = CreateNewContextOptions(nameof(DeleteConfirmed_Sets_IsDeleted_And_Redirects));
            await using (var ctx = new ApplicationDbContext(options))
            {
                ctx.Cars.Add(new Car { Id = "del1", Brand = "B", Model = "M", Year = 2010, SeatingCapacity = 4, DailyPrice = 12m, IsDeleted = false });
                await ctx.SaveChangesAsync();
            }

            await using (var ctx = new ApplicationDbContext(options))
            {
                var controller = new CarsController(ctx);

                var result = await controller.DeleteConfirmed("del1");

                Assert.That(result, Is.InstanceOf<RedirectToActionResult>());
                var redirect = (RedirectToActionResult)result;
                Assert.That(redirect.ActionName, Is.EqualTo("All"));

                var persisted = await ctx.Cars.FindAsync("del1");
                // Query filter excludes deleted; FindAsync returns null after IsDeleted set and SaveChanges,
                // so check underlying entry by querying ignoring query filters.
                var raw = await ctx.Cars.IgnoreQueryFilters().FirstOrDefaultAsync(c => c.Id == "del1");
                Assert.That(raw, Is.Not.Null);
                Assert.That(raw!.IsDeleted, Is.True);
            }
        }
    }
}
