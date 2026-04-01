using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using RentACar.Models;
using RentACar.NewFolder;

namespace RentACar.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<User> _userManager;// The UserManager is a service provided by ASP.NET Core Identity that allows us to manage user accounts, including creating users, validating credentials, and managing roles. By injecting UserManager<ApplicationUser> into the controller, we can perform operations related to user management, such as creating new users during registration and assigning roles to users.
        private readonly SignInManager<User> _signInManager;// The SignInManager is another service provided by ASP.NET Core Identity that handles user sign-in and sign-out operations. By injecting SignInManager<ApplicationUser> into the controller, we can manage user authentication, including signing in users after successful registration or login, and signing out users when they choose to log out of the application.
        private readonly RoleManager<IdentityRole> _roleManager;// The RoleManager is a service provided by ASP.NET Core Identity that allows us to manage roles in the application. By injecting RoleManager<IdentityRole> into the controller, we can create and manage roles, such as "Admin" and "User", which can be assigned to users to control access to different parts of the application based on their roles.


        // The constructor of the AccountController class takes UserManager, SignInManager, and RoleManager as parameters and assigns them to private readonly fields. This allows us to use these services throughout the controller to manage user accounts, handle authentication, and manage roles as needed for registration, login, and access control functionalities.
        public AccountController(
            UserManager<User> userManager,
            SignInManager<User> signInManager,
            RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
        }
        // The Register action method with the HttpGet attribute is responsible for displaying the registration form to the user. It checks if the user is already authenticated, and if so, it redirects them to the home page. If the user is not authenticated, it returns the registration view where they can enter their details to create a new account.
        [HttpGet]
        public IActionResult Register()
        {
            // Check if the user is already authenticated. If they are, redirect them to the home page.

            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Home");
            }

            return View();
        }

        // The Register action method with the HttpPost attribute is responsible for processing the registration form submission. It first checks if the model state is valid, and if not, it returns the view with the model to display validation errors. It then checks if a user with the provided email already exists, and if so, it adds a model error and returns the view. If the email is unique, it creates a new ApplicationUser instance with the provided details and attempts to create the user using UserManager. If the creation is successful, it ensures that necessary roles are created, assigns the "User" role to the new user, signs them in, and redirects them to the home page. If there are errors during user creation, it adds those errors to the model state and returns the view to display them.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(User model)
        {
            // Check if the model state is valid. If it is not, return the view with the model to display validation errors.
            if (!ModelState.IsValid)
            {

                return View(model);
            }

            // Check if a user with the provided email already exists. If such a user exists, add a model error and return the view to display the error message.
            var existingUser = await _userManager.FindByEmailAsync(model.Email);
            if (existingUser != null)
            {
                ModelState.AddModelError("Email", "Този email вече е регистриран");
                return View(model);
            }
            // If the email is unique, create a new ApplicationUser instance with the provided details and attempt to create the user using UserManager. If the creation is successful, ensure that necessary roles are created, assign the "User" role to the new user, sign them in, and redirect them to the home page. If there are errors during user creation, add those errors to the model state and return the view to display them.
            var user = new User
            {
                FirstName = model.FirstName,
                LastName = model.LastName,
                EGN = model.EGN,
                UserName = model.UserName,
                Email = model.Email,
                PasswordHash = model.PasswordHash,
                EmailConfirmed = true
            };

            // Attempt to create the user with the provided password. If the creation is successful, ensure that necessary roles are created, assign the "User" role to the new user, sign them in, and redirect them to the home page. If there are errors during user creation, add those errors to the model state and return the view to display them.
            var result = await _userManager.CreateAsync(user, model.PasswordHash);

            if (result.Succeeded)
            {

                await EnsureRolesCreated();

                await _userManager.AddToRoleAsync(user, "User");

                await _signInManager.SignInAsync(user, isPersistent: false);

                return RedirectToAction("Index", "Home");
            }
            // If there are errors during user creation, add those errors to the model state and return the view to display them.
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        // The Login action method with the HttpGet attribute is responsible for displaying the login form to the user. It checks if the user is already authenticated, and if so, it redirects them to the home page. If the user is not authenticated, it returns the login view where they can enter their credentials to log in to their account.
        [HttpGet]
        public IActionResult Login()
        {
            // Check if the user is already authenticated. If they are, redirect them to the home page.
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Home");
            }

            return View();
        }
        // The Login action method with the HttpPost attribute is responsible for processing the login form submission. It first checks if the model state is valid, and if not, it returns the view with the model to display validation errors. It then attempts to sign in the user using SignInManager with the provided username and password. If the sign-in is successful, it redirects the user to the home page. If the account is locked out due to multiple failed login attempts, it adds a model error indicating that the account is temporarily locked and returns the view. If the login attempt fails for any other reason, it adds a model error indicating invalid credentials and returns the view to display the error message.

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            // Check if the model state is valid. If it is not, return the view with the model to display validation errors.

            if (!ModelState.IsValid)
            {
                return View(model);
            }
            // Attempt to sign in the user using SignInManager with the provided username and password. If the sign-in is successful, redirect the user to the home page. If the account is locked out due to multiple failed login attempts, add a model error indicating that the account is temporarily locked and return the view. If the login attempt fails for any other reason, add a model error indicating invalid credentials and return the view to display the error message.
            var result = await _signInManager.PasswordSignInAsync(
                userName: model.Username,
                password: model.Password,
                isPersistent: model.RememberMe,  // Persistent cookie?
                lockoutOnFailure: true           // Lockout after errors?
            );

            if (result.Succeeded)
            {
                return RedirectToAction("Index", "Home");
            }
            // If the account is locked out due to multiple failed login attempts, add a model error indicating that the account is temporarily locked and return the view. If the login attempt fails for any other reason, add a model error indicating invalid credentials and return the view to display the error message.
            if (result.IsLockedOut)
            {
                ModelState.AddModelError(string.Empty,
                    "Акаунтът ви е временно заключен поради много грешни опити. " +
                    "Моля опитайте отново след 5 минути.");
                return View(model);
            }
            // If the login attempt fails for any other reason, add a model error indicating invalid credentials and return the view to display the error message.
            ModelState.AddModelError(string.Empty, "Невалидно потребителско име или парола.");
            return View(model);
        }
        // The Logout action method is responsible for signing out the currently authenticated user. It uses the SignInManager to sign out the user and then redirects them to the home page. The method is decorated with the Authorize attribute, which means that only authenticated users can access this action, and the ValidateAntiForgeryToken attribute, which helps protect against cross-site request forgery (CSRF) attacks by ensuring that a valid anti-forgery token is included in the request when logging out.
        [Authorize]
        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            // Sign out the currently authenticated user using the SignInManager and then redirect them to the home page. The Authorize attribute ensures that only authenticated users can access this action, and the ValidateAntiForgeryToken attribute helps protect against CSRF attacks by requiring a valid anti-forgery token in the request when logging out.
            await _signInManager.SignOutAsync();

            return RedirectToAction("Index", "Home");
        }
        // The AccessDenied action method is responsible for displaying an access denied view to users who attempt to access resources or perform actions that they are not authorized to access. This method is typically invoked when a user tries to access a restricted area of the application without the necessary permissions or roles. The method simply returns the AccessDenied view, which can be customized to inform the user that they do not have permission to access the requested resource or perform the desired action.

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }
        // The EnsureRolesCreated method is a private asynchronous method that checks if the necessary roles ("Admin" and "User") exist in the system, and if not, it creates them using the RoleManager. This method is called during user registration to ensure that the required roles are available before assigning a role to the newly registered user. By ensuring that the roles are created, we can maintain proper role-based access control in the application and avoid issues when trying to assign roles to users.
        private async Task EnsureRolesCreated()
        {
            // Check if the "Admin" role exists, and if not, create it using the RoleManager. This ensures that the "Admin" role is available in the system for assigning to users who need administrative privileges.

            if (!await _roleManager.RoleExistsAsync("Admin"))
            {
                await _roleManager.CreateAsync(new IdentityRole("Admin"));
            }
            // Check if the "User" role exists, and if not, create it using the RoleManager. This ensures that the "User" role is available in the system for assigning to regular users who do not require administrative privileges.

            if (!await _roleManager.RoleExistsAsync("User"))
            {
                await _roleManager.CreateAsync(new IdentityRole("User"));
            }

        }
    }
}
