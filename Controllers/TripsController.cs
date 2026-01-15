using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TravelAgencyProject.Data;
using TravelAgencyProject.Models;
using TravelAgencyProject.Services;

namespace TravelAgencyProject.Controllers
{
    [RequireHttps]
    public class TripsController : Controller
    {
        private readonly EmailService _emailService;
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment; //Help us to find the wwwroot (images folder)
        public TripsController(AppDbContext context, IWebHostEnvironment webHostEnvironment, EmailService emailService)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
            _emailService = emailService;
        }
        public IActionResult Index(string searchString, string category, string sortBy, bool onlySales, DateTime? startDate, DateTime? endDate)
        {
            // Start with a query including Reviews for the popularity sort (average rating)
            var trips = _context.Trips.Include(t => t.Reviews).AsQueryable();

            // --- 1. SEARCH FILTERING ---
            if (!string.IsNullOrEmpty(searchString))
            {
                trips = trips.Where(s => s.Destination.Contains(searchString)
                                      || s.Country.Contains(searchString)
                                      || s.Category.Contains(searchString)
                                      || s.Description.Contains(searchString));
            }

            // --- 2. CATEGORY FILTERING ---
            if (!string.IsNullOrEmpty(category))
            {
                trips = trips.Where(t => t.Category == category);
            }

            // --- 3. SALES FILTERING ---
            // Only shows trips with a valid SalePrice and an active discount end date
            if (onlySales)
            {
                trips = trips.Where(t => t.SalePrice != null && t.DiscountEndDate >= DateTime.Now);
            }

            // --- 4. SORTING LOGIC ---
            trips = sortBy switch
            {
                "price_asc" => trips.OrderBy(t => t.SalePrice ?? t.Price),
                "price_desc" => trips.OrderByDescending(t => t.SalePrice ?? t.Price),
                "destination" => trips.OrderBy(t => t.Destination),
                // Sort by average review rating. If no reviews exist, treat rating as 0.
                "popularity" => trips.OrderByDescending(t => t.Reviews.Any() ? t.Reviews.Average(r => r.Rating) : 0),
                // Default sort: by start date (soonest first)
                _ => trips.OrderBy(t => t.StartDate)
            };
            // --- 5. DATE FILTERING ---
            if (startDate.HasValue)
            {
                // Filter trips that start on or after the specified start date
                trips = trips.Where(t => t.StartDate >= startDate.Value);
            }
            if (endDate.HasValue)
            {
                // Filter trips that end on or before the specified end date
                trips = trips.Where(t => t.EndDate <= endDate.Value);
            }

                // Keep data for the UI to remember what the user selected
                ViewData["CurrentFilter"] = searchString;
            ViewData["CurrentCategory"] = category;
            ViewData["CurrentSort"] = sortBy;
            ViewData["OnlySales"] = onlySales;
            ViewData["StartDate"] = startDate?.ToString("yyyy-MM-dd");
            ViewData["EndDate"] = endDate?.ToString("yyyy-MM-dd");

            return View(trips.ToList());
        }

        // GET: Trips/Create
        public IActionResult Create()
        {
            // Check if the user is admin
            if (HttpContext.Session.GetString("IsAdmin") != "true")
            {
                return RedirectToAction("Login", "Account"); // Redirect to login if not admin
            }
            return View();
        }


        // POST: Trips/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Trip trip)
        {
            ModelState.Remove("ImageUrl");
            if (trip.ImageFile != null) // If the user uploaded an image
            {

                // Save the image to wwwroot/images/trips
                string folder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "trips");
                if (!Directory.Exists(folder)) // Create the folder if it doesn't exist
                {
                    Directory.CreateDirectory(folder);
                }
                string fileName = Guid.NewGuid().ToString() + "_" + trip.ImageFile.FileName; // Unique file name
                string filePath = Path.Combine(folder, fileName);

                // Save the file
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await trip.ImageFile.CopyToAsync(fileStream);
                }

                // Set the ImageUrl to the relative path
                trip.ImageUrl = "/images/trips/" + fileName;
            }

            else
            {
                ModelState.AddModelError("ImageFile", "Please upload an image. This is mandatory.");
            }

            if (string.IsNullOrEmpty(trip.ImageUrl) && trip.ImageFile == null)
            {
                 ModelState.AddModelError("ImageFile", "Please upload an image.");
            }
            if (ModelState.IsValid)
            {

                //Add and save the trip to the database
                _context.Add(trip);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));


            }
            return View(trip);
        }
        // GET: Trips/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var trip = await _context.Trips
                .Include(t => t.Reviews)
                    .ThenInclude(r => r.User)
                .FirstOrDefaultAsync(m => m.TripId == id);

            if (trip == null) return NotFound();

            var userIdStr = HttpContext.Session.GetString("UserId");
            int? userId = string.IsNullOrEmpty(userIdStr) ? null : int.Parse(userIdStr);

            bool canLeaveReview = false;

            if (userId.HasValue)
            {
                canLeaveReview = await _context.Bookings
                    .AnyAsync(b => b.TripId == trip.TripId &&
                                   b.UserId == userId.Value &&
                                   b.bookingStatus != TripStatus.Cancelled);

                // canLeaveReview = canLeaveReview && trip.StartDate <= DateTime.Now;
            }

            ViewBag.CanLeaveReview = canLeaveReview;

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

        [HttpPost]
        public IActionResult BookTrip(int tripId)
        {
            // --- ADMIN RESTRICTION ---
            if (HttpContext.Session.GetString("IsAdmin") == "true")
            {
                TempData["Error"] = "Admins cannot book trips.";
                return RedirectToAction("Index");
            }
            // --- LOGIN CHECK ---
            if (HttpContext.Session.GetString("Username") == null)
            {
                return RedirectToAction("Login", "Account");
            }

            TempData["Message"] = "Successfully selected! We will contact you soon.";

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var trip = await _context.Trips
                .FirstOrDefaultAsync(m => m.TripId == id);
            if (trip == null) return NotFound();

            return View(trip);
        }

        //POST: Trips/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var trip = await _context.Trips.FindAsync(id);
            if (trip != null)
            {
                _context.Trips.Remove(trip); // Delete the trip from the database
                await _context.SaveChangesAsync(); // Save changes
            }
            return RedirectToAction(nameof(Index));
        }

        // GET: Trips/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            // checking thr admin
            if (HttpContext.Session.GetString("IsAdmin") != "true")
            {
                return RedirectToAction("Login", "Account");
            }

            var trip = await _context.Trips.FindAsync(id);
            if (trip == null) return NotFound();

            return View(trip);
        }

        // POST: Trips/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Trip trip)
        {
            if (id != trip.TripId) return NotFound();

            // remove image validation to allow optional upload
            ModelState.Remove("ImageFile");
            ModelState.Remove("ImageUrl");

            if (ModelState.IsValid)
            {
                try
                {
                    // pulling existing trip to get current image URL
                    var existingTrip = await _context.Trips.AsNoTracking().FirstOrDefaultAsync(t => t.TripId == id);

                    if (trip.ImageFile != null)
                    {
                        // if a new image is uploaded, save it and update the URL
                        string folder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "trips");
                        string fileName = Guid.NewGuid().ToString() + "_" + trip.ImageFile.FileName;
                        string filePath = Path.Combine(folder, fileName);

                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await trip.ImageFile.CopyToAsync(fileStream);
                        }
                        trip.ImageUrl = "/images/trips/" + fileName;
                    }
                    else
                    {
                        // if we dont upload a new image, keep the existing URL
                        trip.ImageUrl = existingTrip.ImageUrl;
                    }

                    _context.Update(trip);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Trips.Any(e => e.TripId == trip.TripId)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(trip);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelBooking(int id)
        {
            var booking = await _context.Bookings
                .Include(b => b.Trip)
                .FirstOrDefaultAsync(b => b.BookingId == id);

            if (booking == null || booking.Trip == null) return NotFound();

            // Check cancellation deadline
            int allowedHours = booking.Trip.CancellationDeadlineHours > 0
                               ? booking.Trip.CancellationDeadlineHours
                               : 24;

            var timeUntilTrip = booking.Trip.StartDate - DateTime.Now;

            if (timeUntilTrip.TotalHours < allowedHours)
            {
                TempData["ErrorMessage"] = $"Cancellation failed: This trip can only be cancelled up to {allowedHours} hours before departure.";
                return RedirectToAction("Index", "Booking");
            }

            // 1. Update Trip Stock and Booking Status
            booking.Trip.Stock += booking.PeopleCount;
            _context.Update(booking.Trip);
            booking.bookingStatus = TripStatus.Cancelled;

            // Save initial changes to ensure stock is updated
            await _context.SaveChangesAsync();

            // --- 2. AUTOMATIC WAITING LIST NOTIFICATION ---
            // Look for the first person in line for this specific trip who hasn't been notified yet
            var nextInLine = await _context.WaitingLists
                .Include(w => w.User)
                .Where(w => w.TripId == booking.TripId && !w.HasBeenNotified)
                .OrderBy(w => w.RequestDate) // FIFO: First In, First Served
                .FirstOrDefaultAsync();

            if (nextInLine != null && nextInLine.User != null)
            {
                try
                {
                    // Send the automatic email notification
                    await _emailService.SendEmailAsync(
                        nextInLine.User.Email,
                        "Good News! A spot opened up",
                        $"Hi {nextInLine.User.FirstName}, a spot is now available for your trip to {booking.Trip.Destination}. " +
                        "Since you are next in line, you can now proceed to book this trip. Don't wait too long!"
                    );

                    // Update the entry to mark that they have been notified
                    nextInLine.HasBeenNotified = true;
                    _context.Update(nextInLine);
                    await _context.SaveChangesAsync();
                }
                catch (Exception)
                {
                    // Fail silently if email server is down, so the cancellation still completes
                }
            }

            TempData["SuccessMessage"] = $"Your booking has been successfully cancelled. {booking.PeopleCount} seats have been returned to stock.";
            return RedirectToAction("Index", "Booking");
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> JoinWaitingList(int tripId)
        {
            var userIdString = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdString)) return RedirectToAction("Login", "Account");

            int userId = int.Parse(userIdString);

            // prevent duplicates
            bool alreadyInList = await _context.WaitingLists
                .AnyAsync(w => w.TripId == tripId && w.UserId == userId);

            if (alreadyInList)
            {
                TempData["SuccessMessage"] = "You are already in the waiting list for this trip.";
                return RedirectToAction("CartCheckout");
            }

            var entry = new WaitingList
            {
                TripId = tripId,
                UserId = userId,
                RequestDate = DateTime.Now
            };

            _context.WaitingLists.Add(entry);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "You have been successfully added to the waiting list!";
            return RedirectToAction("CartCheckout");
        }
        [HttpPost]
        [ValidateAntiForgeryToken]

        public async Task<IActionResult> AddToCart(int id, bool goToCheckout = false, bool directPurchase = false)
        {
            // --- Admin restriction ---
            if (HttpContext.Session.GetString("IsAdmin") == "true")
            {
                TempData["Error"] = "Administrative accounts are not permitted to book trips.";
                return RedirectToAction("Details", new { id = id });
            }

            // --- Login check ---
            var userIdString = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdString))
            {
                return RedirectToAction("Login", "Account");
            }
            int userId = int.Parse(userIdString);

            //  Waiting list rule:
            // If there is a waiting list for this trip, ONLY the first user in line can book/add to cart.
            var firstInWaitingList = await _context.WaitingLists
                .Where(w => w.TripId == id)
                .OrderBy(w => w.RequestDate)
                .FirstOrDefaultAsync();

            if (firstInWaitingList != null && firstInWaitingList.UserId != userId)
            {
                TempData["Error"] =
                    "This trip currently has a waiting list. Only the first user in line can book right now. " +
                    "If you want, you can join the waiting list and we will notify you when it's your turn.";

                TempData["WaitingListTripId"] = id;

                return RedirectToAction("Details", new { id = id });
            }
            var trip = await _context.Trips.FindAsync(id); // verify trip exists
            if (trip == null) return NotFound(); // trip doesn't exist
            if (trip.StartDate.Date <= DateTime.Today) // trip already started
            {
                TempData["Error"] = "Booking is closed for this trip as it starts today or has already passed.";
                return RedirectToAction("Index");
            }


            if (directPurchase)
            {
                var soloCart = new List<int> { id };
                HttpContext.Session.SetString("Cart", System.Text.Json.JsonSerializer.Serialize(soloCart));
                return RedirectToAction("CartCheckout");
            }

            var cartJson = HttpContext.Session.GetString("Cart");
            List<int> cart = string.IsNullOrEmpty(cartJson)
                ? new List<int>()
                : System.Text.Json.JsonSerializer.Deserialize<List<int>>(cartJson);

            int activeBookingsCount = await _context.Bookings
    .Include(b => b.Trip)
    .CountAsync(b => b.UserId == userId &&
                b.bookingStatus != TripStatus.Cancelled &&
                b.Trip.StartDate >= DateTime.Today); // Only count trips from today onwards

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

            if (goToCheckout)
            {
                return RedirectToAction("CartCheckout");
            }

            TempData["Message"] = "Trip added to your cart!";
            return RedirectToAction("Index");
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


        public async Task<IActionResult> GetCartSummary()
        {
            var cartJson = HttpContext.Session.GetString("Cart");
            if (string.IsNullOrEmpty(cartJson))
            {
                return PartialView("_CartSummaryPartial", new List<Trip>());
            }

            List<int> tripIds = System.Text.Json.JsonSerializer.Deserialize<List<int>>(cartJson);

            // Fetch details for the trips in the cart
            var tripsInCart = await _context.Trips
                .Where(t => tripIds.Contains(t.TripId))
                .ToListAsync();

            return PartialView("_CartSummaryPartial", tripsInCart);
        }
        public async Task<IActionResult> CartCheckout()
        {
            // 1. Check if user is logged in
            var userIdString = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdString)) return RedirectToAction("Login", "Account");

            // 2. Get trip IDs from session cart
            var cartJson = HttpContext.Session.GetString("Cart");
            if (string.IsNullOrEmpty(cartJson)) return RedirectToAction("Index");

            List<int> tripIds = System.Text.Json.JsonSerializer.Deserialize<List<int>>(cartJson);

            // 3. Fetch trip details from DB
            var tripsInCart = await _context.Trips
                .Where(t => tripIds.Contains(t.TripId))
                .ToListAsync();

            if (!tripsInCart.Any()) return RedirectToAction("Index");

            // 4. Return the checkout view with the list of trips
            return View("~/Views/Booking/CartCheckout.cshtml", tripsInCart);
        }
        // Action to process the multiple bookings from the cart
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessCartBooking(int peopleCount, string cardNumber, string expiryDate, string cvv)
        {
            // --- SERVER-SIDE SECURITY CHECK ---
            if (HttpContext.Session.GetString("IsAdmin") == "true")
            {
                return RedirectToAction("Index", "Home");
            }

            var userIdString = HttpContext.Session.GetString("UserId");
            var cartJson = HttpContext.Session.GetString("Cart");

            if (string.IsNullOrEmpty(userIdString) || string.IsNullOrEmpty(cartJson))
            {
                return RedirectToAction("Index");
            }

            // --- Validation Section ---

            // Validate Credit Card Number (16 digits)
            if (string.IsNullOrEmpty(cardNumber) || cardNumber.Length < 16)
            {
                ModelState.AddModelError("", "Invalid Credit Card details. Must be 16 digits.");
            }

            // Validate CVV (3 digits)
            if (string.IsNullOrEmpty(cvv) || cvv.Length != 3 || !cvv.All(char.IsDigit))
            {
                ModelState.AddModelError("", "CVV must be exactly 3 digits.");
            }

            // Validate Expiry Date format and date
            if (!string.IsNullOrEmpty(expiryDate) && expiryDate.Contains("/"))
            {
                var parts = expiryDate.Split('/');
                if (parts.Length == 2 && int.TryParse(parts[0], out int month) && int.TryParse(parts[1], out int year))
                {
                    var fullYear = 2000 + year;

                    // prevent invalid month
                    if (month < 1 || month > 12)
                    {
                        ModelState.AddModelError("", "Invalid expiry date month.");
                    }
                    else
                    {
                        var expiryDateTime = new DateTime(fullYear, month, 1).AddMonths(1).AddDays(-1);
                        if (expiryDateTime < DateTime.Now)
                        {
                            ModelState.AddModelError("", "The credit card has expired.");
                        }
                    }
                }
                else
                {
                    ModelState.AddModelError("", "Invalid expiry date format (MM/YY).");
                }
            }
            else
            {
                ModelState.AddModelError("", "Expiry date is required.");
            }

            // If there are validation errors, return to the checkout view with trip details
            if (!ModelState.IsValid)
            {
                List<int> tripIdsForError = System.Text.Json.JsonSerializer.Deserialize<List<int>>(cartJson);
                var tripsInCart = await _context.Trips
                    .Where(t => tripIdsForError.Contains(t.TripId))
                    .ToListAsync();

                return View("~/Views/Booking/CartCheckout.cshtml", tripsInCart);
            }

            int userId = int.Parse(userIdString);
            List<int> tripIds = System.Text.Json.JsonSerializer.Deserialize<List<int>>(cartJson);

            decimal totalOrderPrice = 0;
            List<string> destinationNames = new List<string>();

            // Collect trips that were sold out due to concurrency (last spot taken)
            List<int> soldOutTripIds = new List<int>();
            Dictionary<int, string> soldOutTripNames = new Dictionary<int, string>();

            // Transaction for the whole cart processing
            await using var tx = await _context.Database.BeginTransactionAsync();

            // --- Processing Bookings ---
            foreach (var id in tripIds)
            {
                // Pull trip for name/price only (NOT for stock logic)
                var trip = await _context.Trips
                    .AsNoTracking()
                    .FirstOrDefaultAsync(t => t.TripId == id);

                if (trip == null)
                    continue;

                // Waiting list: only first user can book
                var firstInWaitingList = await _context.WaitingLists
                    .Where(w => w.TripId == id)
                    .OrderBy(w => w.RequestDate)
                    .FirstOrDefaultAsync();

                if (firstInWaitingList != null && firstInWaitingList.UserId != userId)
                {
                    TempData["Error"] = $"Cannot book {trip.Destination}. There is a waiting list and it's not your turn.";
                    continue;
                }

                //  ATOMIC STOCK DECREASE (prevents double booking of last spot)
                var rowsAffected = await _context.Database.ExecuteSqlInterpolatedAsync($@"
            UPDATE Trips
            SET Stock = Stock - {peopleCount}
            WHERE TripId = {id} AND Stock >= {peopleCount}
        ");

                // If no rows updated, there was not enough stock (someone else took the last one)
                if (rowsAffected == 0)
                {
                    soldOutTripIds.Add(id);
                    soldOutTripNames[id] = trip.Destination;
                    continue;
                }

                // Create booking
                decimal currentTripPrice = (trip.SalePrice ?? trip.Price) * peopleCount;

                var booking = new Booking
                {
                    UserId = userId,
                    TripId = id,
                    PeopleCount = peopleCount,
                    TotalPrice = currentTripPrice,
                    BookingDate = DateTime.Now,
                    PaymentStatus = PaymentStatus.Completed,
                    bookingStatus = TripStatus.Upcoming
                };

                _context.Bookings.Add(booking);

                // If the booking was allowed (and user was first), remove them from waiting list
                if (firstInWaitingList != null && firstInWaitingList.UserId == userId)
                {
                    _context.WaitingLists.Remove(firstInWaitingList);
                }

                totalOrderPrice += currentTripPrice;
                destinationNames.Add(trip.Destination);
            }

            //  If some trips failed due to last-spot being taken, pass them to the checkout view
            if (soldOutTripIds.Count > 0)
            {
                TempData["SoldOutTripIds"] = System.Text.Json.JsonSerializer.Serialize(soldOutTripIds);

                // one full sentence message (what you asked for)
                TempData["SoldOutMessage"] =
                    "Some trips were just booked by another user (the last available spot was taken). Would you like to join the waiting list for those trips?";
            }

            // ✅ If nothing was booked successfully, rollback and return user to checkout
            if (destinationNames.Count == 0)
            {
                await tx.RollbackAsync();

                var tripsInCart = await _context.Trips
                    .Where(t => tripIds.Contains(t.TripId))
                    .ToListAsync();

                // show message + allow waiting list joins
                TempData["Error"] = "No bookings were completed because there is not enough availability.";
                return View("~/Views/Booking/CartCheckout.cshtml", tripsInCart);
            }

            await _context.SaveChangesAsync();
            await tx.CommitAsync();

            // --- Post-Processing ---

            // Send notification email
            var user = await _context.Users.FindAsync(userId);
            if (user != null)
            {
                try
                {
                    await _emailService.SendEmailAsync(
                        user.Email,
                        "Booking Confirmation",
                        $"Hi {user.FirstName}, your booking for {string.Join(", ", destinationNames)} is confirmed!"
                    );
                }
                catch
                {
                    // Fail silently if email service is not configured
                }
            }

            // Create summary for the Confirmation View
            var summaryBooking = new Booking
            {
                PeopleCount = peopleCount,
                TotalPrice = totalOrderPrice,
                BookingDate = DateTime.Now,
                Trip = new Trip { Destination = string.Join(", ", destinationNames) }
            };

            HttpContext.Session.Remove("Cart");
            return View("~/Views/Booking/Confirmation.cshtml", summaryBooking);
        }


    }
}
