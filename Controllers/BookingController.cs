using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TravelAgencyProject.Data;
using TravelAgencyProject.Models;
using TravelAgencyProject.Services;

namespace TravelAgencyProject.Controllers
{
    public class BookingController : Controller
    {
        private readonly AppDbContext _context;
        private readonly EmailService _emailService; // 1. Add the email service

        public BookingController(AppDbContext context, EmailService emailService)
        {
            _context = context;
            _emailService = emailService; // 2. Initialize the email service
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
            // Find the entry and include the User and Trip details to get the email and destination
            var entry = await _context.WaitingLists
                .Include(w => w.User)
                .Include(w => w.Trip)
                .FirstOrDefaultAsync(w => w.Id == id);

            if (entry != null)
            {
                // 3. Send the email manually now because the Admin clicked the button
                await _emailService.SendEmailAsync(entry.User.Email, "Room Available!",
                    $"Hi {entry.User.FirstName}, a room is now available for {entry.Trip.Destination}. You can now proceed with your booking.");

                entry.HasBeenNotified = true;
                await _context.SaveChangesAsync();
                TempData["AdminMessage"] = "The customer has been notified via email.";
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