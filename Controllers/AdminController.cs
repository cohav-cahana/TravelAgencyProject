using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TravelAgencyProject.Data;
using TravelAgencyProject.Models;

namespace TravelAgencyProject.Controllers
{
    public class AdminController : Controller
    {
        private readonly AppDbContext _context;
        public AdminController(AppDbContext context)
        {
            _context = context;
        }
        public IActionResult Index()
        {
            if (HttpContext.Session.GetString("IsAdmin") != "true")
            {
                return RedirectToAction("Login", "Account");
            }
            return View();
        }

        // GET: Admin/Users
        public async Task<IActionResult> Users()
        {
            var users = await _context.Users.ToListAsync();
            return View(users);
        }
        // GET: Admin/UserBookings/5 (view bookings for a specific user)
        public async Task<IActionResult> UserBookings(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            var bookings = await _context.Bookings
                .Include(b => b.Trip)
                .Where(b => b.UserId == id)
                .ToListAsync();

            ViewBag.UserName = $"{user.FirstName} {user.LastName}";
            return View(bookings);
        }
        // POST: Admin/DeleteUser/5
        [HttpPost]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user != null && !user.IsAdmin) // Prevent deletion of admin users
            {
                _context.Users.Remove(user);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Users));
        }
        public async Task<IActionResult> WaitingList()
        {
            //Check if admin is logged in
            if (HttpContext.Session.GetString("IsAdmin") != "true")
            {
                return RedirectToAction("Login", "Account");
            }

            // Retrieve waiting list entries with related Trip and User data
            var waitingEntries = await _context.WaitingLists
                .Include(w => w.Trip)
                .Include(w => w.User)
                .OrderBy(w => w.RequestDate) 
                .ToListAsync();

            return View(waitingEntries);
        }

    }
}
