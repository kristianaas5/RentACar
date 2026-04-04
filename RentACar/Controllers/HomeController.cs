using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using RentACar.Models;

namespace RentACar.Controllers
{
    /// <summary>
    /// Handles requests for the site's home pages such as the index, privacy and error pages.
    /// Provides simple, non-authorized views used by the public site.
    /// </summary>
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        /// <summary>
        /// Initializes a new instance of <see cref="HomeController"/>.
        /// </summary>
        /// <param name="logger">Logger instance used for diagnostics and tracing.</param>
        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// GET: /
        /// Returns the main landing page view.
        /// </summary>
        /// <returns>The Index view result.</returns>
        public IActionResult Index()
        {
            return View();
        }

        /// <summary>
        /// GET: /Home/Privacy
        /// Returns the privacy policy page.
        /// </summary>
        /// <returns>The Privacy view result.</returns>
        public IActionResult Privacy()
        {
            return View();
        }

        /// <summary>
        /// GET: /Home/Error
        /// Returns the Error view with an <see cref="ErrorViewModel"/> containing the current request id for troubleshooting.
        /// The response is not cached.
        /// </summary>
        /// <returns>The Error view populated with request correlation information.</returns>
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
