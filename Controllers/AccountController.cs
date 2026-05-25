using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using MongoDB.Driver;
using SmartCityPulse.Models;
using SmartCityPulse.Data;

namespace SmartCityPulse.Controllers
{
    public class AccountController : Controller
    {
        private readonly MongoDbContext _context;

        public AccountController(MongoDbContext context)
        {
            _context = context;
        }

        // ==================== LOGIN ====================
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string email, string password)
        {
            // Check if fields are empty
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                ViewBag.Error = "Email and password are required!";
                return View();
            }

            // ✅ CHECK OPERATORS COLLECTION FIRST
            var operatorUser = await _context.Operators.Find(o => o.Email == email).FirstOrDefaultAsync();

            if (operatorUser != null && operatorUser.PasswordHash == password)
            {
                HttpContext.Session.SetString("UserId", operatorUser.Id);
                HttpContext.Session.SetString("UserName", operatorUser.Name);
                HttpContext.Session.SetString("UserEmail", operatorUser.Email);
                HttpContext.Session.SetString("UserRole", operatorUser.Role);
                HttpContext.Session.SetString("UserDepartment", operatorUser.Department ?? "");

                TempData["SuccessMessage"] = $"Welcome back, {operatorUser.Name}!";
                return RedirectToAction("Dashboard", "Operator");
            }

            // ✅ CHECK USERS COLLECTION (Admin/Citizen)
            var user = await _context.Users.Find(u => u.Email == email).FirstOrDefaultAsync();

            if (user == null)
            {
                ViewBag.Error = "Invalid email or password!";
                return View();
            }

            if (user.PasswordHash != password)
            {
                ViewBag.Error = "Invalid email or password!";
                return View();
            }

            HttpContext.Session.SetString("UserId", user.Id);
            HttpContext.Session.SetString("UserName", user.Name);
            HttpContext.Session.SetString("UserEmail", user.Email);
            HttpContext.Session.SetString("UserRole", user.Role);

            TempData["SuccessMessage"] = $"Welcome back, {user.Name}!";

            if (user.Role == "Admin")
            {
                return RedirectToAction("Index", "Admin");
            }
            else if (user.Role == "Citizen")
            {
                return RedirectToAction("Index", "Citizen");
            }
            else
            {
                return RedirectToAction("Index", "Home");
            }
        }

        // ==================== REGISTER ====================
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(string name, string email, string password, string confirmPassword, string phone)
        {
            if (string.IsNullOrEmpty(name))
            {
                ViewBag.Error = "Name is required!";
                return View();
            }

            if (string.IsNullOrEmpty(email))
            {
                ViewBag.Error = "Email is required!";
                return View();
            }

            if (password != confirmPassword)
            {
                ViewBag.Error = "Passwords do not match!";
                return View();
            }

            if (string.IsNullOrEmpty(password) || password.Length < 6)
            {
                ViewBag.Error = "Password must be at least 6 characters!";
                return View();
            }

            var existingUser = await _context.Users.Find(u => u.Email == email).FirstOrDefaultAsync();
            if (existingUser != null)
            {
                ViewBag.Error = "Email already registered!";
                return View();
            }

            var newUser = new AppUser
            {
                Name = name,
                Email = email,
                PasswordHash = password,
                Phone = phone ?? "",
                Role = "Citizen",
                CreatedAt = DateTime.UtcNow
            };

            await _context.Users.InsertOneAsync(newUser);

            TempData["SuccessMessage"] = "Registration successful! Please login.";
            return RedirectToAction("Login");
        }

        // ==================== LOGOUT ====================
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            TempData["SuccessMessage"] = "Logged out successfully!";
            return RedirectToAction("Index", "Home");
        }
    }
}