using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using RentACar.Models;
using RentACar.NewFolder;

namespace RentACar.Controllers
{
    /// <summary>
    /// Controller responsible for account related actions:
    /// registration, login, logout and access denied handling.
    /// Uses <see cref="UserManager{User}"/>, <see cref="SignInManager{User}"/> and <see cref="RoleManager{IdentityRole}"/>.
    /// </summary>
    public class AccountController : Controller
    {
        private readonly UserManager<User> _userManager;// The UserManager is a service provided by ASP.NET Core Identity that allows us to manage user accounts, including creating users, validating credentials, and managing roles. By injecting UserManager<ApplicationUser> into the controller, we can perform operations related to user management, such as creating new users during registration and assigning roles to users.
        private readonly SignInManager<User> _signInManager;// The SignInManager is another service provided by ASP.NET Core Identity that handles user sign-in and sign-out operations. By injecting SignInManager<ApplicationUser> into the controller, we can manage user authentication, including signing in users after successful registration or login, and signing out users when they choose to log out of the application.
        private readonly RoleManager<IdentityRole> _roleManager;// The RoleManager is a service provided by ASP.NET Core Identity that allows us to manage roles in the application. By injecting RoleManager<IdentityRole> into the controller, we can create and manage roles, such as "Admin" and "User", which can be assigned to users to control access to different parts of the application based on their roles.


        /// <summary>
        /// Creates a new instance of <see cref="AccountController"/>.
        /// </summary>
        /// <param name="userManager">Injected <see cref="UserManager{User}"/> instance.</param>
        /// <param name="signInManager">Injected <see cref="SignInManager{User}"/> instance.</param>
        /// <param name="roleManager">Injected <see cref="RoleManager{IdentityRole}"/> instance.</param>
        public AccountController(
            UserManager<User> userManager,
            SignInManager<User> signInManager,
            RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
        }

        /// <summary>
        /// GET: /Account/Register
        /// Displays the registration form. Redirects authenticated users to Home.
        /// </summary>
        /// <returns>Registration view or redirection to Home for authenticated users.</returns>
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

        /// <summary>
        /// POST: /Account/Register
        /// Processes registration. Creates the user, ensures roles exist and signs in the new user.
        /// </summary>
        /// <param name="model">A <see cref="User"/> model containing registration data.</param>
        /// <returns>Redirect to Home on success; returns the registration view with validation errors otherwise.</returns>
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

        /// <summary>
        /// GET: /Account/Login
        /// Displays the login form. Redirects authenticated users to Home.
        /// </summary>
        /// <returns>Login view or redirection to Home for authenticated users.</returns>
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

        /// <summary>
        /// POST: /Account/Login
        /// Attempts to sign in the user with the provided credentials.
        /// </summary>
        /// <param name="model">Login data in a <see cref="LoginViewModel"/> instance.</param>
        /// <returns>Redirect to Home on success; returns login view with errors otherwise.</returns>
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

        /// <summary>
        /// POST: /Account/Logout
        /// Signs out the current user.
        /// </summary>
        /// <returns>Redirect to Home after sign out.</returns>
        [Authorize]
        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            // Sign out the currently authenticated user using the SignInManager and then redirect them to the home page. The Authorize attribute ensures that only authenticated users can access this action, and the ValidateAntiForgeryToken attribute helps protect against CSRF attacks by requiring a valid anti-forgery token in the request when logging out.
            await _signInManager.SignOutAsync();

            return RedirectToAction("Index", "Home");
        }

        /// <summary>
        /// GET: /Account/AccessDenied
        /// Returns the AccessDenied view when a user tries to access a resource they are not authorized for.
        /// </summary>
        /// <returns>AccessDenied view.</returns>
        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }

        /// <summary>
        /// Ensures the application roles exist ("Admin" and "User").
        /// Creates missing roles using <see cref="RoleManager{IdentityRole}"/>.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
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
