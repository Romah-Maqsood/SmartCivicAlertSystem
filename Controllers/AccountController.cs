using Microsoft.AspNetCore.Mvc;

namespace SmartCityPulse.Controllers
{
    public class AccountController : Controller
    {
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Login(string email, string password)
        {
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Register(string name, string email, string password)
        {
            return RedirectToAction("Login");
        }

        public IActionResult Logout()
        {
            return RedirectToAction("Index", "Home");
        }
    }
}