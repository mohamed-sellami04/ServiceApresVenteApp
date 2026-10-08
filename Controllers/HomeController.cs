using Microsoft.AspNetCore.Mvc;

namespace ServiceApresVenteApp.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Error(string message = "An error occurred. Please try again later.")
        {
            ViewBag.ErrorMessage = message;
            return View();
        }
    }
}