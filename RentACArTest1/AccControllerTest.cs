using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;
using RentACar.Controllers;
using RentACar.Models;
using RentACar.NewFolder;

namespace RentACArTest1
{
    public class AccountControllerTests
    {
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

        private static Mock<UserManager<User>> CreateUserManagerMock()
        {
            var store = new Mock<IUserStore<User>>();
            return new Mock<UserManager<User>>(
                store.Object,
                Mock.Of<IOptions<IdentityOptions>>(),
                Mock.Of<IPasswordHasher<User>>(),
                new IUserValidator<User>[0],
                new IPasswordValidator<User>[0],
                Mock.Of<ILookupNormalizer>(),
                Mock.Of<IdentityErrorDescriber>(),
                Mock.Of<IServiceProvider>(),
                Mock.Of<ILogger<UserManager<User>>>()
            );
        }

        private static Mock<SignInManager<User>> CreateSignInManagerMock(Mock<UserManager<User>> userManagerMock)
        {
            return new Mock<SignInManager<User>>(
                userManagerMock.Object,
                Mock.Of<IHttpContextAccessor>(),
                Mock.Of<IUserClaimsPrincipalFactory<User>>(),
                Mock.Of<IOptions<IdentityOptions>>(),
                Mock.Of<ILogger<SignInManager<User>>>(),
                Mock.Of<IAuthenticationSchemeProvider>(),
                Mock.Of<IUserConfirmation<User>>()
            );
        }

        private static Mock<RoleManager<IdentityRole>> CreateRoleManagerMock()
        {
            var store = new Mock<IRoleStore<IdentityRole>>();
            return new Mock<RoleManager<IdentityRole>>(
                store.Object,
                new IRoleValidator<IdentityRole>[0],
                Mock.Of<ILookupNormalizer>(),
                Mock.Of<IdentityErrorDescriber>(),
                Mock.Of<ILogger<RoleManager<IdentityRole>>>()
            );
        }

        // helper to create a minimal valid User (satisfies required properties)
        private static User CreateUser(
            string id = "1",
            string userName = "user",
            string email = "a@a.com",
            string firstName = "F",
            string lastName = "L",
            string egn = "1234567890",
            string passwordHash = "pwd",
            bool isDeleted = false)
        {
            return new User
            {
                Id = id,
                UserName = userName,
                Email = email,
                FirstName = firstName,
                LastName = lastName,
                EGN = egn,
                PasswordHash = passwordHash,
                IsDeleted = isDeleted,
                EmailConfirmed = true
            };
        }

        // -------- Register GET --------

        [Test]
        public void Register_Get_Returns_View_When_NotAuthenticated()
        {
            var um = CreateUserManagerMock();
            var sm = CreateSignInManagerMock(um);
            var rm = CreateRoleManagerMock();

            var controller = new AccountController(um.Object, sm.Object, rm.Object)
            {
                ControllerContext = CreateControllerContext(username: null, isAuthenticated: false)
            };

            var result = controller.Register();

            Assert.That(result, Is.InstanceOf<ViewResult>());
        }

        [Test]
        public void Register_Get_Redirects_When_Authenticated()
        {
            var um = CreateUserManagerMock();
            var sm = CreateSignInManagerMock(um);
            var rm = CreateRoleManagerMock();

            var controller = new AccountController(um.Object, sm.Object, rm.Object)
            {
                ControllerContext = CreateControllerContext(username: "u", isAuthenticated: true)
            };

            var result = controller.Register();

            Assert.That(result, Is.InstanceOf<RedirectToActionResult>());
            var redirect = (RedirectToActionResult)result;
            Assert.That(redirect.ActionName, Is.EqualTo("Index"));
            Assert.That(redirect.ControllerName, Is.EqualTo("Home"));
        }

        // -------- Register POST --------

        [Test]
        public async Task Register_Post_Returns_View_When_ModelState_Invalid()
        {
            var um = CreateUserManagerMock();
            var sm = CreateSignInManagerMock(um);
            var rm = CreateRoleManagerMock();

            var controller = new AccountController(um.Object, sm.Object, rm.Object);
            controller.ModelState.AddModelError("x", "err");

            var model = CreateUser();

            var result = await controller.Register(model);

            Assert.That(result, Is.InstanceOf<ViewResult>());
        }

        [Test]
        public async Task Register_Post_Returns_View_When_Email_Exists()
        {
            var um = CreateUserManagerMock();
            var sm = CreateSignInManagerMock(um);
            var rm = CreateRoleManagerMock();

            var existing = CreateUser(id: "2", userName: "existing", email: "a@a.com");
            um.Setup(u => u.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync(existing);

            var controller = new AccountController(um.Object, sm.Object, rm.Object);

            var model = CreateUser(userName: "u", email: "a@a.com");

            var result = await controller.Register(model);

            Assert.That(result, Is.InstanceOf<ViewResult>());
            Assert.That(controller.ModelState.ContainsKey("Email"), Is.True);
        }

        [Test]
        public async Task Register_Post_Redirects_On_Success()
        {
            var um = CreateUserManagerMock();
            var sm = CreateSignInManagerMock(um);
            var rm = CreateRoleManagerMock();

            um.Setup(u => u.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync((User?)null);
            um.Setup(u => u.CreateAsync(It.IsAny<User>(), It.IsAny<string>())).ReturnsAsync(IdentityResult.Success);
            um.Setup(u => u.AddToRoleAsync(It.IsAny<User>(), "User")).ReturnsAsync(IdentityResult.Success);

            rm.Setup(r => r.RoleExistsAsync(It.IsAny<string>())).ReturnsAsync(true);

            sm.Setup(s => s.SignInAsync(It.IsAny<User>(), It.IsAny<bool>(), null)).Returns(Task.CompletedTask);

            var controller = new AccountController(um.Object, sm.Object, rm.Object)
            {
                ControllerContext = CreateControllerContext(username: null, isAuthenticated: false)
            };

            var model = CreateUser(userName: "user", email: "b@b.com");

            var result = await controller.Register(model);

            Assert.That(result, Is.InstanceOf<RedirectToActionResult>());
            var redirect = (RedirectToActionResult)result;
            Assert.That(redirect.ActionName, Is.EqualTo("Index"));
            Assert.That(redirect.ControllerName, Is.EqualTo("Home"));

            um.Verify(u => u.CreateAsync(It.IsAny<User>(), It.IsAny<string>()), Times.Once);
            um.Verify(u => u.AddToRoleAsync(It.IsAny<User>(), "User"), Times.Once);
            sm.Verify(s => s.SignInAsync(It.IsAny<User>(), It.IsAny<bool>(), null), Times.Once);
        }

        [Test]
        public async Task Register_Post_Returns_View_When_Create_Fails()
        {
            var um = CreateUserManagerMock();
            var sm = CreateSignInManagerMock(um);
            var rm = CreateRoleManagerMock();

            um.Setup(u => u.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync((User?)null);
            var error = new IdentityError { Description = "err" };
            um.Setup(u => u.CreateAsync(It.IsAny<User>(), It.IsAny<string>()))
                .ReturnsAsync(IdentityResult.Failed(error));

            var controller = new AccountController(um.Object, sm.Object, rm.Object);

            var model = CreateUser(userName: "user", email: "c@c.com");

            var result = await controller.Register(model);

            Assert.That(result, Is.InstanceOf<ViewResult>());
            Assert.That(controller.ModelState[string.Empty].Errors.Count, Is.GreaterThan(0));
        }

        // -------- Login GET --------

        [Test]
        public void Login_Get_Returns_View_When_NotAuthenticated()
        {
            var um = CreateUserManagerMock();
            var sm = CreateSignInManagerMock(um);
            var rm = CreateRoleManagerMock();

            var controller = new AccountController(um.Object, sm.Object, rm.Object)
            {
                ControllerContext = CreateControllerContext(username: null, isAuthenticated: false)
            };

            var result = controller.Login();

            Assert.That(result, Is.InstanceOf<ViewResult>());
        }

        [Test]
        public void Login_Get_Redirects_When_Authenticated()
        {
            var um = CreateUserManagerMock();
            var sm = CreateSignInManagerMock(um);
            var rm = CreateRoleManagerMock();

            var controller = new AccountController(um.Object, sm.Object, rm.Object)
            {
                ControllerContext = CreateControllerContext(username: "u", isAuthenticated: true)
            };

            var result = controller.Login();

            Assert.That(result, Is.InstanceOf<RedirectToActionResult>());
            var redirect = (RedirectToActionResult)result;
            Assert.That(redirect.ActionName, Is.EqualTo("Index"));
            Assert.That(redirect.ControllerName, Is.EqualTo("Home"));
        }

        // -------- Login POST --------

        [Test]
        public async Task Login_Post_Redirects_On_Success()
        {
            var um = CreateUserManagerMock();
            var sm = CreateSignInManagerMock(um);
            var rm = CreateRoleManagerMock();

            sm.Setup(s => s.PasswordSignInAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>()))
              .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Success);

            var controller = new AccountController(um.Object, sm.Object, rm.Object);

            var model = new LoginViewModel { Username = "u", Password = "p", RememberMe = false };

            var result = await controller.Login(model);

            Assert.That(result, Is.InstanceOf<RedirectToActionResult>());
            var redirect = (RedirectToActionResult)result;
            Assert.That(redirect.ActionName, Is.EqualTo("Index"));
            Assert.That(redirect.ControllerName, Is.EqualTo("Home"));
        }

        [Test]
        public async Task Login_Post_Shows_LockedOut_Message()
        {
            var um = CreateUserManagerMock();
            var sm = CreateSignInManagerMock(um);
            var rm = CreateRoleManagerMock();

            sm.Setup(s => s.PasswordSignInAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>()))
              .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.LockedOut);

            var controller = new AccountController(um.Object, sm.Object, rm.Object);

            var model = new LoginViewModel { Username = "u", Password = "p", RememberMe = false };

            var result = await controller.Login(model);

            Assert.That(result, Is.InstanceOf<ViewResult>());
            Assert.That(controller.ModelState[string.Empty].Errors.Count, Is.GreaterThan(0));
        }

        [Test]
        public async Task Login_Post_Shows_InvalidCredentials()
        {
            var um = CreateUserManagerMock();
            var sm = CreateSignInManagerMock(um);
            var rm = CreateRoleManagerMock();

            sm.Setup(s => s.PasswordSignInAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>()))
              .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Failed);

            var controller = new AccountController(um.Object, sm.Object, rm.Object);

            var model = new LoginViewModel { Username = "u", Password = "p", RememberMe = false };

            var result = await controller.Login(model);

            Assert.That(result, Is.InstanceOf<ViewResult>());
            Assert.That(controller.ModelState[string.Empty].Errors.Count, Is.GreaterThan(0));
        }

        // -------- Logout --------

        [Test]
        public async Task Logout_Post_SignsOut_And_Redirects()
        {
            var um = CreateUserManagerMock();
            var sm = CreateSignInManagerMock(um);
            var rm = CreateRoleManagerMock();

            sm.Setup(s => s.SignOutAsync()).Returns(Task.CompletedTask);

            var controller = new AccountController(um.Object, sm.Object, rm.Object)
            {
                ControllerContext = CreateControllerContext(username: "u", isAuthenticated: true)
            };

            var result = await controller.Logout();

            Assert.That(result, Is.InstanceOf<RedirectToActionResult>());
            var redirect = (RedirectToActionResult)result;
            Assert.That(redirect.ActionName, Is.EqualTo("Index"));
            Assert.That(redirect.ControllerName, Is.EqualTo("Home"));

            sm.Verify(s => s.SignOutAsync(), Times.Once);
        }

        // -------- AccessDenied --------

        [Test]
        public void AccessDenied_Returns_View()
        {
            var um = CreateUserManagerMock();
            var sm = CreateSignInManagerMock(um);
            var rm = CreateRoleManagerMock();

            var controller = new AccountController(um.Object, sm.Object, rm.Object);

            var result = controller.AccessDenied();

            Assert.That(result, Is.InstanceOf<ViewResult>());
        }
    }
}
