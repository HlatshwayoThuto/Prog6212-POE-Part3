// Import necessary namespaces
using Microsoft.AspNetCore.Mvc;       // Provides classes and interfaces for building ASP.NET Core MVC web apps
using WebApplication1.Models;         // Includes application-specific models and services like DataService

namespace WebApplication1.Controllers
{
    // Defines a controller named HomeController, which inherits from the base Controller class
    public class HomeController : Controller
    {
        // Private field to hold the injected DataService instance
        private readonly DataService _dataService;

        // Constructor that uses dependency injection to receive an instance of DataService
        public HomeController(DataService dataService)
        {
            _dataService = dataService; // Assigns the injected service to the private field
        }

        // Action method for the default landing page (typically the home page)
        public IActionResult Index()
        {
            // Returns the default view associated with this action (Views/Home/Index.cshtml)
            return View();
        }

        // Action method for the "About" page
        public IActionResult About()
        {
            // Returns the view for the About page (Views/Home/About.cshtml)
            return View();
        }

        // Action method for handling unexpected errors
        public IActionResult Error()
        {
            // Sets an error message in ViewData to be displayed in the Error view
            ViewData["ErrorMessage"] = "An unexpected error occurred. Please try again later.";

            // Returns the view for the Error page (Views/Home/Error.cshtml)
            return View();
        }
    }
}