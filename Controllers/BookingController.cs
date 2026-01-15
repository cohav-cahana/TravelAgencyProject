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
            // --- Authentication Check ---
            var userIdString = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdString))
            {
                return RedirectToAction("Login", "Account");
            }

            int userId = int.Parse(userIdString);

            // Fetch all bookings for this user, including the trip details
            var allBookings = await _context.Bookings
                .Include(b => b.Trip)
                .Where(b => b.UserId == userId)
                .OrderByDescending(b => b.Trip.StartDate)
                .ToListAsync();

            // Separate Cancelled Bookings first ---
            // We take all bookings where status is Cancelled
            var cancelledBookings = allBookings.Where(b => b.bookingStatus == TripStatus.Cancelled).ToList();

            // Now filter the REMAINING bookings (not cancelled) by date
            var activeBookings = allBookings.Where(b => b.bookingStatus != TripStatus.Cancelled);

            var upcomingBookings = activeBookings.Where(b => b.Trip.StartDate >= DateTime.Today).ToList();
            var pastBookings = activeBookings.Where(b => b.Trip.StartDate < DateTime.Today).ToList();

            // --- Passing data to the View ---
            ViewBag.PastBookings = pastBookings;
            ViewBag.CancelledBookings = cancelledBookings; // Adding the cancelled trips to ViewBag

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
        [HttpPost]
        public async Task<IActionResult> SendReminders()
        {
            // 1. Calculate the target date (exactly 5 days from today)
            var targetDate = DateTime.Today.AddDays(5);

            // 2. Fetch all bookings from the database that start on the target date.
            // We use 'Include' to load the User details (for the email) and Trip details (for the destination).
            // We only filter for 'Upcoming' bookings to avoid sending reminders for cancelled ones.
            var bookingsToRemind = await _context.Bookings
                .Include(b => b.User)
                .Include(b => b.Trip)
                .Where(b => b.Trip.StartDate.Date == targetDate.Date && b.bookingStatus == TripStatus.Upcoming)
                .ToListAsync();

            int count = 0;

            // Loop through each booking and send a personalized reminder email
            foreach (var booking in bookingsToRemind)
            {
                if (booking.User != null && !string.IsNullOrEmpty(booking.User.Email))
                {
                    await _emailService.SendEmailAsync(booking.User.Email,
                        "Your Trip is Coming Up!",
                        $"Hi {booking.User.FirstName}, this is a reminder that your trip to {booking.Trip.Destination} departs in 5 days! Get your bags ready.");
                    count++;
                }
            }

            // 3. Set a feedback message for the Admin to see how many emails were sent
            TempData["AdminMessage"] = $"Success! {count} reminders were sent...";
            // Redirect back to the Admin dashboard
            return RedirectToAction("Index", "Home");
        }
    }
}