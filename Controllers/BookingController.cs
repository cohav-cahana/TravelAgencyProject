using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TravelAgencyProject.Data;
using TravelAgencyProject.Models;

namespace TravelAgencyProject.Controllers
{
    public class BookingController : Controller
    {
        private readonly AppDbContext _context;

        public BookingController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var userIdString = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdString))
            {
                return RedirectToAction("Login", "Account");
            }

            int userId = int.Parse(userIdString);

            var allBookings = await _context.Bookings
                .Include(b => b.Trip)
                .Where(b => b.UserId == userId)
                .OrderByDescending(b => b.Trip.StartDate)
                .ToListAsync();

            var upcomingBookings = allBookings.Where(b => b.Trip.StartDate >= DateTime.Today).ToList();
            var pastBookings = allBookings.Where(b => b.Trip.StartDate < DateTime.Today).ToList();

            ViewBag.PastBookings = pastBookings;

            return View(upcomingBookings);
        }
        public async Task<IActionResult> AdminWaitingList()
        {
            if (HttpContext.Session.GetString("IsAdmin") != "true")
            {
                return RedirectToAction("Login", "Account");
            }

            var list = await _context.WaitingLists
                .Include(w => w.User)
                .Include(w => w.Trip)
                .OrderBy(w => w.RequestDate) // FIFO
                .ToListAsync();

            return View("~/Views/Admin/WaitingList.cshtml", list);
        }
        [HttpPost]
        public async Task<IActionResult> NotifyUser(int id)
        {
            var entry = await _context.WaitingLists.FindAsync(id);
            if (entry != null)
            {
                entry.HasBeenNotified = true;
                await _context.SaveChangesAsync();
                TempData["AdminMessage"] = "The customer has been marked as notified.";
            }
            return RedirectToAction(nameof(AdminWaitingList));
        }

        [HttpPost]
        public async Task<IActionResult> RemoveFromWaitingList(int id)
        {
            var entry = await _context.WaitingLists.FindAsync(id);
            if (entry != null)
            {
                _context.WaitingLists.Remove(entry);
                await _context.SaveChangesAsync();
                TempData["AdminMessage"] = "The user was removed from the waiting list.";
            }
            return RedirectToAction(nameof(AdminWaitingList));
        }
    }
}