using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TravelAgencyProject.Data;
using TravelAgencyProject.Models;
using TravelAgencyProject.Services;
using Microsoft.AspNetCore.Http;
using TravelAgencyProject.Extensions;

namespace TravelAgencyProject.Controllers
{
    public class BookingController : Controller
    {
        private readonly AppDbContext _context;
        private readonly EmailService _emailService;

        public BookingController(AppDbContext context, EmailService emailService)
        {
            _context = context;
            _emailService = emailService;
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

            // Filter Cancelled Bookings
            var cancelledBookings = allBookings.Where(b => b.bookingStatus == TripStatus.Cancelled).ToList();

            // Filter Active Bookings (Not Cancelled)
            var activeBookings = allBookings.Where(b => b.bookingStatus != TripStatus.Cancelled);

            // Split into Upcoming and Past based on the current date
            var upcomingBookings = activeBookings.Where(b => b.Trip.StartDate >= DateTime.Today).ToList();
            var pastBookings = activeBookings.Where(b => b.Trip.StartDate < DateTime.Today).ToList();

            // Pass data to the View using ViewBag
            ViewBag.PastBookings = pastBookings;
            ViewBag.CancelledBookings = cancelledBookings;

            return View(upcomingBookings);
        }

        /// <summary>
        /// Direct Purchase Logic: Triggered when "BOOK NOW" is clicked.
        /// Bypasses the cart by setting a special flag in the Session.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DirectPurchase(int tripId)
        {
            // Set a flag and store the Trip ID to indicate this is a single-item purchase
            HttpContext.Session.SetString("IsDirectPurchase", "true");
            HttpContext.Session.SetInt32("DirectTripId", tripId);

            // Redirect directly to the checkout page
            return RedirectToAction(nameof(CartCheckout));
        }

        /// <summary>
        /// Displays the Checkout page. 
        /// Decides whether to show the whole cart or just a single "Book Now" trip.
        /// </summary>
        public IActionResult CartCheckout()
        {
            // Authentication Check
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("UserId")))
            {
                return RedirectToAction("Login", "Account");
            }

            // Check if the user arrived via the "BOOK NOW" direct path
            var isDirect = HttpContext.Session.GetString("IsDirectPurchase");
            if (isDirect == "true")
            {
                int? tripId = HttpContext.Session.GetInt32("DirectTripId");
                var trip = _context.Trips.FirstOrDefault(t => t.TripId == tripId);

                if (trip != null)
                {
                    // Return a list containing ONLY the selected trip, ignoring the existing cart
                    return View(new List<Trip> { trip });
                }
            }

            // Regular Cart Logic: Load items stored in the cart session
            var cart = HttpContext.Session.GetComplexObject<List<Trip>>("Cart") ?? new List<Trip>();

            if (!cart.Any())
            {
                return RedirectToAction("Index", "Trips");
            }

            return View(cart);
        }

        /// <summary>
        /// Admin View: Displays the global waiting list for all trips.
        /// </summary>
        public async Task<IActionResult> AdminWaitingList()
        {
            if (HttpContext.Session.GetString("IsAdmin") != "true")
            {
                return RedirectToAction("Login", "Account");
            }

            var list = await _context.WaitingLists
                .Include(w => w.User)
                .Include(w => w.Trip)
                .OrderBy(w => w.RequestDate) // First-In-First-Out (FIFO)
                .ToListAsync();

            return View("~/Views/Admin/WaitingList.cshtml", list);
        }

        /// <summary>
        /// Admin Action: Manually notifies a user via email that a spot has opened up.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> NotifyUser(int id)
        {
            var entry = await _context.WaitingLists
                .Include(w => w.User)
                .Include(w => w.Trip)
                .FirstOrDefaultAsync(w => w.Id == id);

            if (entry != null)
            {
                // Send notification email
                await _emailService.SendEmailAsync(entry.User.Email, "Room Available!",
                    $"Hi {entry.User.FirstName}, a room is now available for {entry.Trip.Destination}. You can now proceed with your booking.");

                entry.HasBeenNotified = true;
                await _context.SaveChangesAsync();
                TempData["AdminMessage"] = "The customer has been notified via email.";
            }
            return RedirectToAction(nameof(AdminWaitingList));
        }

        /// <summary>
        /// Admin Action: Removes a user from the waiting list.
        /// </summary>
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

        /// <summary>
        /// Admin Action: Sends automatic reminders to users whose trips start in exactly 5 days.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> SendReminders()
        {
            // Calculate target date (5 days from today)
            var targetDate = DateTime.Today.AddDays(5);

            // Fetch upcoming bookings starting on the target date
            var bookingsToRemind = await _context.Bookings
                .Include(b => b.User)
                .Include(b => b.Trip)
                .Where(b => b.Trip.StartDate.Date == targetDate.Date && b.bookingStatus == TripStatus.Upcoming)
                .ToListAsync();

            int count = 0;

            // Loop and send personalized emails
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

            TempData["AdminMessage"] = $"Success! {count} reminders were sent.";
            return RedirectToAction("Index", "Home");
        }
    }
}