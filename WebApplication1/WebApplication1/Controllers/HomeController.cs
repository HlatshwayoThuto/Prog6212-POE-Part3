// Import necessary namespaces
using Microsoft.AspNetCore.Mvc;       // Provides classes and interfaces for building ASP.NET Core MVC web apps
using WebApplication1.Models;         // Includes application-specific models and services like DataService

namespace WebApplication1.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }
    }
}