using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TravelAgencyProject.Data;
using TravelAgencyProject.Extensions;
using TravelAgencyProject.Models;
using TravelAgencyProject.Services;

namespace TravelAgencyProject.Controllers
{
    [RequireHttps]
    public class TripsController : Controller
    {
        private readonly EmailService _emailService;
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public TripsController(AppDbContext context, IWebHostEnvironment webHostEnvironment, EmailService emailService)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
            _emailService = emailService;
        }

        // --- 1. DISPLAY TRIPS (INDEX) ---
        // Handles searching, filtering by category/sales, and sorting
        public IActionResult Index(string searchString, string category, string sortBy, bool onlySales, DateTime? startDate, DateTime? endDate)
        {
            var trips = _context.Trips.Include(t => t.Reviews).AsQueryable();

            // Search filtering
            if (!string.IsNullOrEmpty(searchString))
            {
                trips = trips.Where(s => s.Destination.Contains(searchString)
                                      || s.Country.Contains(searchString)
                                      || s.Category.Contains(searchString)
                                      || s.Description.Contains(searchString));
            }

            // Category filtering
            if (!string.IsNullOrEmpty(category))
            {
                trips = trips.Where(t => t.Category == category);
            }

            // Sales filtering
            if (onlySales)
            {
                trips = trips.Where(t => t.SalePrice != null && t.DiscountEndDate >= DateTime.Now);
            }

            // Sorting logic
            trips = sortBy switch
            {
                "price_asc" => trips.OrderBy(t => t.SalePrice ?? t.Price),
                "price_desc" => trips.OrderByDescending(t => t.SalePrice ?? t.Price),
                "destination" => trips.OrderBy(t => t.Destination),
                "popularity" => trips.OrderByDescending(t => t.Reviews.Any() ? t.Reviews.Average(r => r.Rating) : 0),
                _ => trips.OrderBy(t => t.StartDate)
            };

            // Date filtering
            if (startDate.HasValue) trips = trips.Where(t => t.StartDate >= startDate.Value);
            if (endDate.HasValue) trips = trips.Where(t => t.EndDate <= endDate.Value);

            // Keep data for the UI state preservation
            ViewData["CurrentFilter"] = searchString;
            ViewData["CurrentCategory"] = category;
            ViewData["CurrentSort"] = sortBy;
            ViewData["OnlySales"] = onlySales;
            ViewData["StartDate"] = startDate?.ToString("yyyy-MM-dd");
            ViewData["EndDate"] = endDate?.ToString("yyyy-MM-dd");

            return View(trips.ToList());
        }

        // --- 2. TRIP DETAILS ---
        // Displays detailed info and handles waiting list availability
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var trip = await _context.Trips
                .Include(t => t.Reviews).ThenInclude(r => r.User)
                .FirstOrDefaultAsync(m => m.TripId == id);

            if (trip == null) return NotFound();

            var userIdStr = HttpContext.Session.GetString("UserId");
            int? userId = string.IsNullOrEmpty(userIdStr) ? null : int.Parse(userIdStr);

            // Check if the user has a valid booking to allow reviews
            bool canLeaveReview = false;
            if (userId.HasValue)
            {
                canLeaveReview = await _context.Bookings
                    .AnyAsync(b => b.TripId == trip.TripId && b.UserId == userId.Value && b.bookingStatus != TripStatus.Cancelled);
            }
            ViewBag.CanLeaveReview = canLeaveReview;

            // Handle Waiting List Logic and UI buttons
            var userEntry = userId.HasValue
                ? await _context.WaitingLists.FirstOrDefaultAsync(w => w.TripId == trip.TripId && w.UserId == userId.Value)
                : null;

            if (userEntry != null)
            {
                int peopleAhead = await _context.WaitingLists
                    .CountAsync(w => w.TripId == trip.TripId && w.RequestDate < userEntry.RequestDate);

                ViewBag.IsInWaitingList = true;
                ViewBag.PeopleAhead = peopleAhead;
                ViewBag.CanBookNow = (trip.Stock > 0 && peopleAhead == 0);
            }
            else
            {
                ViewBag.IsInWaitingList = false;
                bool hasWaitingList = await _context.WaitingLists.AnyAsync(w => w.TripId == trip.TripId);
                ViewBag.CanBookNow = (!hasWaitingList && trip.Stock > 0);
            }

            ViewBag.HasWaitingList = await _context.WaitingLists.AnyAsync(w => w.TripId == trip.TripId);
            return View(trip);
        }

        // --- 3. CREATE TRIP (ADMIN ONLY) ---
        public IActionResult Create()
        {
            if (HttpContext.Session.GetString("IsAdmin") != "true") return RedirectToAction("Login", "Account");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Trip trip)
        {
            ModelState.Remove("ImageUrl");
            if (trip.ImageFile != null)
            {
                string folder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "trips");
                if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
                string fileName = Guid.NewGuid().ToString() + "_" + trip.ImageFile.FileName;
                string filePath = Path.Combine(folder, fileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await trip.ImageFile.CopyToAsync(fileStream);
                }
                trip.ImageUrl = "/images/trips/" + fileName;
            }

            if (ModelState.IsValid)
            {
                _context.Add(trip);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(trip);
        }

        // --- 4. EDIT TRIP (ADMIN ONLY) ---
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || HttpContext.Session.GetString("IsAdmin") != "true") return NotFound();
            var trip = await _context.Trips.FindAsync(id);
            if (trip == null) return NotFound();
            return View(trip);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Trip trip)
        {
            if (id != trip.TripId) return NotFound();
            ModelState.Remove("ImageFile");
            ModelState.Remove("ImageUrl");

            if (ModelState.IsValid)
            {
                try
                {
                    var existingTrip = await _context.Trips.AsNoTracking().FirstOrDefaultAsync(t => t.TripId == id);
                    if (trip.ImageFile != null)
                    {
                        string folder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "trips");
                        string fileName = Guid.NewGuid().ToString() + "_" + trip.ImageFile.FileName;
                        string filePath = Path.Combine(folder, fileName);
                        using (var fileStream = new FileStream(filePath, FileMode.Create)) { await trip.ImageFile.CopyToAsync(fileStream); }
                        trip.ImageUrl = "/images/trips/" + fileName;
                    }
                    else trip.ImageUrl = existingTrip.ImageUrl;

                    _context.Update(trip);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException) { if (!_context.Trips.Any(e => e.TripId == trip.TripId)) return NotFound(); else throw; }
                return RedirectToAction(nameof(Index));
            }
            return View(trip);
        }

        // --- 5. DELETE TRIP (ADMIN ONLY) ---
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || HttpContext.Session.GetString("IsAdmin") != "true") return NotFound();
            var trip = await _context.Trips.FirstOrDefaultAsync(m => m.TripId == id);
            return trip == null ? NotFound() : View(trip);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var trip = await _context.Trips.FindAsync(id);
            if (trip != null) { _context.Trips.Remove(trip); await _context.SaveChangesAsync(); }
            return RedirectToAction(nameof(Index));
        }

        // --- 6. SHOPPING CART LOGIC ---
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddToCart(int id, bool goToCheckout = false, bool directPurchase = false)
        {
            if (HttpContext.Session.GetString("IsAdmin") == "true") return RedirectToAction("Details", new { id });

            var userIdString = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdString)) return RedirectToAction("Login", "Account");
            int userId = int.Parse(userIdString);

            // Queue validation
            var firstInWaitingList = await _context.WaitingLists.Where(w => w.TripId == id).OrderBy(w => w.RequestDate).FirstOrDefaultAsync();
            if (firstInWaitingList != null && firstInWaitingList.UserId != userId)
            {
                TempData["Error"] = "This trip has a waiting list. Only the first person in line can book.";
                return RedirectToAction("Details", new { id });
            }

            var trip = await _context.Trips.FindAsync(id);
            if (trip == null || trip.StartDate.Date <= DateTime.Today) return NotFound();

            var cartJson = HttpContext.Session.GetString("Cart");
            List<int> cart = string.IsNullOrEmpty(cartJson) ? new List<int>() : System.Text.Json.JsonSerializer.Deserialize<List<int>>(cartJson);

            // Enforcement of 3 active trips business rule
            int activeBookingsCount = await _context.Bookings.CountAsync(b => b.UserId == userId && b.bookingStatus != TripStatus.Cancelled && b.Trip.StartDate >= DateTime.Today);
            if (activeBookingsCount + cart.Count >= 3)
            {
                TempData["Error"] = "You can only have up to 3 active trips.";
                return RedirectToAction("Index");
            }

            if (!cart.Contains(id))
            {
                cart.Add(id);
                HttpContext.Session.SetString("Cart", System.Text.Json.JsonSerializer.Serialize(cart));
            }

            return goToCheckout || directPurchase ? RedirectToAction("CartCheckout") : RedirectToAction("Index");
        }
        [HttpGet]
        public IActionResult GetCartSummary()
        {
            // 1. Get IDs from Session
            var cartJson = HttpContext.Session.GetString("Cart");
            if (string.IsNullOrEmpty(cartJson))
            {
                return PartialView("_CartSummaryPartial", new List<Trip>());
            }

            // 2. Convert JSON to List of IDs
            var tripIds = System.Text.Json.JsonSerializer.Deserialize<List<int>>(cartJson);

            // 3. Fetch Trip objects from DB
            var trips = _context.Trips.Where(t => tripIds.Contains(t.TripId)).ToList();

            // 4. Return the Partial View (The UI for the modal)
            return PartialView("_CartSummaryPartial", trips);
        }

        [HttpPost]
        public IActionResult RemoveFromCart(int id)
        {
            var cartJson = HttpContext.Session.GetString("Cart");
            if (!string.IsNullOrEmpty(cartJson))
            {
                var cart = System.Text.Json.JsonSerializer.Deserialize<List<int>>(cartJson);
                cart.Remove(id);
                HttpContext.Session.SetString("Cart", System.Text.Json.JsonSerializer.Serialize(cart));
            }
            return Ok();
        }

        public async Task<IActionResult> CartCheckout()
        {
            var userIdString = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdString)) return RedirectToAction("Login", "Account");

            var cartJson = HttpContext.Session.GetString("Cart");
            if (string.IsNullOrEmpty(cartJson)) return RedirectToAction("Index");

            List<int> tripIds = System.Text.Json.JsonSerializer.Deserialize<List<int>>(cartJson);
            var tripsInCart = await _context.Trips.Where(t => tripIds.Contains(t.TripId)).ToListAsync();

            if (!tripsInCart.Any()) return RedirectToAction("Index");

            // Commit session state to ensure data persistence during redirect
            await HttpContext.Session.CommitAsync();

            return View("~/Views/Booking/CartCheckout.cshtml", tripsInCart);
        }

        // --- 7. BOOKING PROCESSING (CREDIT CARD) ---
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessCartBooking(int peopleCount, string cardNumber, string expiryDate, string cvv, string tripIdsString)
        {
            var userIdString = HttpContext.Session.GetString("UserId");
            var cartJson = HttpContext.Session.GetString("Cart");

            // Plan A: Session Data | Plan B: Hidden Form Field (Fallback for Localhost Session issues)
            List<int> tripIdList = new List<int>();
            if (!string.IsNullOrEmpty(cartJson))
                tripIdList = System.Text.Json.JsonSerializer.Deserialize<List<int>>(cartJson);
            else if (!string.IsNullOrEmpty(tripIdsString))
                tripIdList = tripIdsString.Split(',').Select(int.Parse).ToList();

            if (tripIdList == null || !tripIdList.Any())
            {
                TempData["Error"] = "Session expired. Please add the items to your cart again.";
                return RedirectToAction("Index");
            }

            if (string.IsNullOrEmpty(userIdString)) return RedirectToAction("Login", "Account");

            // Payment Input Validation
            if (string.IsNullOrEmpty(cardNumber) || cardNumber.Length < 16) ModelState.AddModelError("", "Invalid Card Number.");
            if (!ModelState.IsValid)
            {
                var trips = await _context.Trips.Where(t => tripIdList.Contains(t.TripId)).ToListAsync();
                return View("~/Views/Booking/CartCheckout.cshtml", trips);
            }
            if (!string.IsNullOrEmpty(expiryDate))
            {
                var parts = expiryDate.Split('/');
                if (parts.Length == 2 && int.TryParse(parts[0], out int month) && int.TryParse(parts[1], out int year))
                {
                    // convert 2-digit year to 4-digit year
                    int fullYear = 2000 + year;

                    // Create a date for the last day of the expiry month
                    var lastDayOfExpiry = new DateTime(fullYear, month, 1).AddMonths(1).AddDays(-1);

                    // Check if the card is expired
                    if (lastDayOfExpiry < DateTime.Today)
                    {
                        TempData["Error"] = "Credit card has expired.";
                        return RedirectToAction("CartCheckout", "Booking");
                    }
                }
            }

            int userId = int.Parse(userIdString);
            decimal totalOrderPrice = 0;
            List<string> destinationNames = new List<string>();

            using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                foreach (var id in tripIdList)
                {
                    var trip = await _context.Trips.FindAsync(id);
                    if (trip == null || trip.Stock < peopleCount) continue;

                    trip.Stock -= peopleCount;
                    _context.Update(trip);

                    decimal currentTripPrice = (trip.SalePrice ?? trip.Price) * peopleCount;
                    totalOrderPrice += currentTripPrice;

                    _context.Bookings.Add(new Booking
                    {
                        UserId = userId,
                        TripId = id,
                        PeopleCount = peopleCount,
                        TotalPrice = currentTripPrice,
                        BookingDate = DateTime.Now,
                        PaymentStatus = PaymentStatus.Completed,
                        bookingStatus = TripStatus.Upcoming
                    });
                    destinationNames.Add(trip.Destination);
                }

                await _context.SaveChangesAsync();
                await tx.CommitAsync();

                // Send Confirmation Email Asynchronously
                try
                {
                    var user = await _context.Users.FindAsync(userId);
                    if (user != null)
                    {
                        string body = $"<h3>Booking Confirmed!</h3><p>Hi {user.FirstName}, your booking for {string.Join(", ", destinationNames)} was successful.</p>";
                        await _emailService.SendEmailAsync(user.Email, "Booking Confirmation", body);
                    }
                }
                catch { /* Suppress email errors to prevent user interruption */ }

                HttpContext.Session.Remove("Cart");
                var summary = new Booking { PeopleCount = peopleCount, TotalPrice = totalOrderPrice, BookingDate = DateTime.Now, Trip = new Trip { Destination = string.Join(", ", destinationNames) } };
                return View("~/Views/Booking/Confirmation.cshtml", summary);
            }
            catch (Exception)
            {
                await tx.RollbackAsync();
                TempData["Error"] = "A technical error occurred. Please try again.";
                return RedirectToAction("CartCheckout");
            }
        }

        // --- 8. PAYPAL SUCCESS HANDLER ---
        [HttpGet]
        public async Task<IActionResult> PayPalSuccess(string orderId, int peopleCount, string tripIds)
        {
            var userIdString = HttpContext.Session.GetString("UserId");
            var cartJson = HttpContext.Session.GetString("Cart");

            List<int> tripIdList = new List<int>();
            if (!string.IsNullOrEmpty(cartJson))
                tripIdList = System.Text.Json.JsonSerializer.Deserialize<List<int>>(cartJson);
            else if (!string.IsNullOrEmpty(tripIds))
                tripIdList = tripIds.Split(',').Select(int.Parse).ToList();

            if (tripIdList == null || !tripIdList.Any()) return RedirectToAction("Index");

            // Redirect to login if user session is lost during PayPal redirect
            if (string.IsNullOrEmpty(userIdString))
            {
                TempData["Error"] = "Session lost. Please log in to complete your order record.";
                return RedirectToAction("Login", "Account");
            }

            int userId = int.Parse(userIdString);
            decimal totalOrderPrice = 0;
            List<string> destinationNames = new List<string>();

            using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                foreach (var id in tripIdList)
                {
                    var trip = await _context.Trips.FindAsync(id);
                    if (trip == null || trip.Stock < peopleCount) continue;

                    trip.Stock -= peopleCount;
                    _context.Update(trip);

                    decimal currentTripPrice = (trip.SalePrice ?? trip.Price) * peopleCount;
                    totalOrderPrice += currentTripPrice;

                    _context.Bookings.Add(new Booking
                    {
                        UserId = userId,
                        TripId = id,
                        PeopleCount = peopleCount,
                        TotalPrice = currentTripPrice,
                        BookingDate = DateTime.Now,
                        PaymentStatus = PaymentStatus.Completed,
                        bookingStatus = TripStatus.Upcoming
                    });
                    destinationNames.Add(trip.Destination);
                }

                await _context.SaveChangesAsync();
                await tx.CommitAsync();

                // Send PayPal Receipt via Email
                try
                {
                    var user = await _context.Users.FindAsync(userId);
                    if (user != null)
                    {
                        string body = $"<h3>Payment Received via PayPal</h3><p>Order ID: {orderId}</p><p>Trip(s): {string.Join(", ", destinationNames)}</p>";
                        await _emailService.SendEmailAsync(user.Email, "Order Confirmation", body);
                    }
                }
                catch { }

                HttpContext.Session.Remove("Cart");
                var summary = new Booking { PeopleCount = peopleCount, TotalPrice = totalOrderPrice, BookingDate = DateTime.Now, Trip = new Trip { Destination = string.Join(", ", destinationNames) } };
                return View("~/Views/Booking/Confirmation.cshtml", summary);
            }
            catch (Exception) { await tx.RollbackAsync(); return RedirectToAction("Index"); }
        }

        // --- 9. CANCELLATIONS & QUEUE NOTIFICATIONS ---
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelBooking(int id)
        {
            var booking = await _context.Bookings.Include(b => b.Trip).FirstOrDefaultAsync(b => b.BookingId == id);
            if (booking == null) return NotFound();

            int allowedHours = booking.Trip.CancellationDeadlineHours > 0 ? booking.Trip.CancellationDeadlineHours : 24;
            if ((booking.Trip.StartDate - DateTime.Now).TotalHours < allowedHours)
            {
                TempData["ErrorMessage"] = "Cancellation is no longer possible for this departure.";
                return RedirectToAction("Index", "Booking");
            }

            booking.Trip.Stock += booking.PeopleCount;
            booking.bookingStatus = TripStatus.Cancelled;
            await _context.SaveChangesAsync();

            // Automatic notification for the next person in the waiting list
            var nextInLine = await _context.WaitingLists.Include(w => w.User).Where(w => w.TripId == booking.TripId && !w.HasBeenNotified).OrderBy(w => w.RequestDate).FirstOrDefaultAsync();
            if (nextInLine != null)
            {
                try
                {
                    await _emailService.SendEmailAsync(nextInLine.User.Email, "Great News: A Spot is Available!", "A spot has opened up for your requested trip. Visit the site to book now!");
                    nextInLine.HasBeenNotified = true;
                    await _context.SaveChangesAsync();
                }
                catch { }
            }

            return RedirectToAction("Index", "Booking");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> JoinWaitingList(int tripId)
        {
            var userIdString = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdString)) return RedirectToAction("Login", "Account");

            int userId = int.Parse(userIdString);
            if (await _context.WaitingLists.AnyAsync(w => w.TripId == tripId && w.UserId == userId)) return RedirectToAction("CartCheckout");

            _context.WaitingLists.Add(new WaitingList { TripId = tripId, UserId = userId, RequestDate = DateTime.Now });
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Successfully added to the waiting list.";
            return RedirectToAction("CartCheckout");
        }
    }
}