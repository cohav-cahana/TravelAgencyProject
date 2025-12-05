using Microsoft.AspNetCore.Mvc;
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
        // GET: /Account/Register *we do it after*
        public IActionResult Register()
        {
            return View();
        }
        
        private readonly AppDbContext _context;

        // Constructor that accepts the database 
        public AccountController(AppDbContext context)
        {
            _context = context;
        }
    }
}
