using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using RentACar.Controllers;
using RentACar.Models;

namespace RentACArTest1
{
    public class HomeControllerTests
    {
        [Test]
        public void Index_Returns_View()
        {
            var logger = Mock.Of<ILogger<HomeController>>();
            var controller = new HomeController(logger);

            var result = controller.Index();

            Assert.That(result, Is.InstanceOf<ViewResult>());
        }

        [Test]
        public void Privacy_Returns_View()
        {
            var logger = Mock.Of<ILogger<HomeController>>();
            var controller = new HomeController(logger);

            var result = controller.Privacy();

            Assert.That(result, Is.InstanceOf<ViewResult>());
        }

        [Test]
        public void Error_Returns_View_With_RequestId_From_HttpContext_When_No_Activity()
        {
            var logger = Mock.Of<ILogger<HomeController>>();
            var controller = new HomeController(logger)
            {
                ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext
                {
                    HttpContext = new DefaultHttpContext { TraceIdentifier = "trace-123" }
                }
            };

            var result = controller.Error();

            Assert.That(result, Is.InstanceOf<ViewResult>());
            var view = (ViewResult)result;
            Assert.That(view.Model, Is.InstanceOf<ErrorViewModel>());
            var model = (ErrorViewModel)view.Model;
            Assert.That(model.RequestId, Is.EqualTo("trace-123"));
        }

        [Test]
        public void Error_Uses_ActivityId_When_Activity_Current_Is_Present()
        {
            var logger = Mock.Of<ILogger<HomeController>>();
            var controller = new HomeController(logger)
            {
                ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext
                {
                    HttpContext = new DefaultHttpContext { TraceIdentifier = "trace-456" }
                }
            };

            var activity = new Activity("unittest-activity");
            activity.Start();
            try
            {
                var result = controller.Error();

                Assert.That(result, Is.InstanceOf<ViewResult>());
                var view = (ViewResult)result;
                Assert.That(view.Model, Is.InstanceOf<ErrorViewModel>());
                var model = (ErrorViewModel)view.Model;
                Assert.That(model.RequestId, Is.EqualTo(activity.Id));
            }
            finally
            {
                activity.Stop();
            }
        }

        [Test]
        public void Error_Has_ResponseCacheAttribute()
        {
            var method = typeof(RentACar.Controllers.HomeController).GetMethod("Error");
            Assert.That(method, Is.Not.Null);

            var attr = method!.GetCustomAttributes(typeof(ResponseCacheAttribute), inherit: false)
                             .Cast<ResponseCacheAttribute>()
                             .FirstOrDefault();

            Assert.That(attr, Is.Not.Null);
            Assert.That(attr.NoStore, Is.True);
            Assert.That(attr.Duration, Is.EqualTo(0));
            Assert.That(attr.Location, Is.EqualTo(ResponseCacheLocation.None));
        }
    }
}
