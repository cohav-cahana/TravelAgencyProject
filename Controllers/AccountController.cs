using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TravelAgencyProject.Data;
using TravelAgencyProject.Models;

namespace TravelAgencyProject.Controllers
{
    public class AccountController : Controller
    {

        // GET: /Account/Login
        public IActionResult Login()
        {
            return View();
        }
        //POST: /Account/Login
        [HttpPost]
        public IActionResult Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            { //Looking for user in the database
                var user = _context.Users.FirstOrDefault(u => u.Email == model.Email && u.Password == model.Password);
                if (user != null) // User found, so we save his info in session
                {
                    HttpContext.Session.SetString("Email", user.Email);
                    HttpContext.Session.SetString("FirstName", user.FirstName);
                    if (user.IsAdmin) // If the user is admin, we save that info in session too
                    {
                        HttpContext.Session.SetString("IsAdmin", "true");
                    }
                    return RedirectToAction("Index", "Home");
                }
                ModelState.AddModelError("", "Invalid email or password");
            }
            return View(model);
        }
        public IActionResult Logout()
        {
            HttpContext.Session.Clear(); // Delete all session data
            return RedirectToAction("Index", "Home");
        }
        // GET: /Account/Register 
        public IActionResult Register()
        {
            if (HttpContext.Session.GetString("Email") != null)
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }
        // POST: /Account/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register([Bind ("FirstName,LastName,Email,Password")] User user)
            {
            if (ModelState.IsValid)
            {
                // Check if email already exists
                var existingUser = _context.Users.FirstOrDefault(u => u.Email == user.Email);
                if (existingUser != null)
                {
                    ModelState.AddModelError("Email", "Email is already registered.");
                    return View(user);
                }
                //defult values for sefaety
                user.IsAdmin = false;
                user.IsActive = true;

                _context.Add(user);
                await _context.SaveChangesAsync();

                //Rembemer me 
                HttpContext.Session.SetString("Email", user.Email);
                HttpContext.Session.SetString("FirstName", user.FirstName);
                return RedirectToAction("Index", "Home");
            }
            return View(user);
        }
        // GET: Trips/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            // בדיקת הרשאת אדמין
            if (HttpContext.Session.GetString("IsAdmin") != "true")
            {
                return RedirectToAction("Login", "Account");
            }

            var trip = await _context.Trips.FindAsync(id);
            if (trip == null) return NotFound();

            return View(trip);
        }


        private readonly AppDbContext _context;

        // Constructor that accepts the database 
        public AccountController(AppDbContext context)
        {
            _context = context;
        }
    }
}

