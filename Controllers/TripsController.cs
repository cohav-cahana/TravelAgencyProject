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
        public IActionResult Index(string searchString, string category, string sortBy)
        {
            var trips = from t in _context.Trips select t;

            if (!string.IsNullOrEmpty(searchString))
            {
                trips = trips.Where(s => s.Destination.Contains(searchString)
                                      || s.Country.Contains(searchString)
                                      || s.Category.Contains(searchString)
                                      || s.Description.Contains(searchString));
            }

            if (!string.IsNullOrEmpty(category))
            {
                trips = trips.Where(t => t.Category == category);
            }

            trips = sortBy switch
            {
                "price_asc" => trips.OrderBy(t => t.SalePrice ?? t.Price), 
                "price_desc" => trips.OrderByDescending(t => t.SalePrice ?? t.Price), 
                "destination" => trips.OrderBy(t => t.Destination), 
                _ => trips.OrderBy(t => t.StartDate) 
            };

            ViewData["CurrentFilter"] = searchString;
            ViewData["CurrentCategory"] = category;
            ViewData["CurrentSort"] = sortBy;

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



            ModelState.Remove("ImageUrl");// We will set it in the code below
            if (string.IsNullOrEmpty(trip.ImageUrl) && trip.ImageFile == null)
            {
                //       ModelState.AddModelError("ImageFile", "Please upload an image.");
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
        //GET: Trips/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var trip = await _context.Trips
                .FirstOrDefaultAsync(m => m.TripId == id);

            if (trip == null)
            {
                return NotFound();
            }

            return View(trip);
        }

        [HttpPost]
        public IActionResult BookTrip(int tripId)
        {
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

            if (booking == null) return NotFound();

            var timeUntilTrip = booking.Trip.StartDate - DateTime.Now;

            if (timeUntilTrip.TotalHours < 24)
            {
                TempData["ErrorMessage"] = "Cancellation failed: You cannot cancel a trip less than 24 hours before departure.";
                return RedirectToAction("Index", "Booking");
            }

            if (booking.Trip != null)
            {
              booking.Trip.Stock += booking.PeopleCount;
                _context.Update(booking.Trip);
            }

            booking.bookingStatus = TripStatus.Cancelled;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Your booking has been successfully cancelled and the seats are released.";

            return RedirectToAction("Index", "Booking");
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> JoinWaitingList(int tripId)
        {
            var userIdString = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdString)) return RedirectToAction("Login", "Account");

            var entry = new WaitingList
            {
                TripId = tripId,
                UserId = int.Parse(userIdString),
                RequestDate = DateTime.Now
            };

            _context.WaitingLists.Add(entry);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "You have been successfully added to the waiting list!";

            return RedirectToAction("Index", "Trips");
        }

        [HttpPost]
        public async Task<IActionResult> AddToCart(int id, bool goToCheckout = false) // Added bool parameter
        {
            var cartJson = HttpContext.Session.GetString("Cart");
            List<int> cart = string.IsNullOrEmpty(cartJson)
                ? new List<int>()
                : System.Text.Json.JsonSerializer.Deserialize<List<int>>(cartJson);

            var userIdString = HttpContext.Session.GetString("UserId");
            int activeBookingsCount = 0;

            if (!string.IsNullOrEmpty(userIdString))
            {
                int userId = int.Parse(userIdString);
                activeBookingsCount = await _context.Bookings
                    .CountAsync(b => b.UserId == userId && b.bookingStatus == TripStatus.Upcoming);
            }

            // Limit check
            if (activeBookingsCount + cart.Count >= 3)
            {
                TempData["Error"] = "You can only have up to 3 active trips.";
                return RedirectToAction("Index");
            }

            // Add trip to cart if it's not already there (optional check)
            if (!cart.Contains(id))
            {
                cart.Add(id);
                HttpContext.Session.SetString("Cart", System.Text.Json.JsonSerializer.Serialize(cart));
            }

            // If "BOOK NOW" was clicked, go straight to Checkout
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

                // Remove the first instance of this trip ID
                cart.Remove(id);

                HttpContext.Session.SetString("Cart", System.Text.Json.JsonSerializer.Serialize(cart));
            }

            return Ok(); // Return 200 OK for AJAX
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
            var userIdString = HttpContext.Session.GetString("UserId");
            var cartJson = HttpContext.Session.GetString("Cart");

            if (string.IsNullOrEmpty(userIdString) || string.IsNullOrEmpty(cartJson))
            {
                return RedirectToAction("Index");
            }
            if (string.IsNullOrEmpty(cardNumber) || cardNumber.Length < 16)
            {
                ModelState.AddModelError("", "Invalid Credit Card details.");
                List<int> tripIdsForError = System.Text.Json.JsonSerializer.Deserialize<List<int>>(cartJson);
                var tripsInCart = await _context.Trips.Where(t => tripIdsForError.Contains(t.TripId)).ToListAsync();

                return View("CartCheckout", tripsInCart);
            }
            if (string.IsNullOrEmpty(cvv) || cvv.Length != 3 || !cvv.All(char.IsDigit))
            {
                ModelState.AddModelError("", "CVV must be exactly 3 digits.");
            }
            if (!string.IsNullOrEmpty(expiryDate) && expiryDate.Contains("/"))
            {
                var parts = expiryDate.Split('/');
                if (parts.Length == 2 && int.TryParse(parts[0], out int month) && int.TryParse(parts[1], out int year))
                {
                    var fullYear = 2000 + year;
                    var expiryDateTime = new DateTime(fullYear, month, 1).AddMonths(1).AddDays(-1);
                    if (expiryDateTime < DateTime.Now)
                    {
                        ModelState.AddModelError("", "The credit card has expired.");
                    }
                }
                else { ModelState.AddModelError("", "Invalid expiry date format."); }
            }
            if (!ModelState.IsValid)
            {
                List<int> tripIdsForError = System.Text.Json.JsonSerializer.Deserialize<List<int>>(cartJson);
                var tripsInCart = await _context.Trips.Where(t => tripIdsForError.Contains(t.TripId)).ToListAsync();
                return View("~/Views/Booking/CartCheckout.cshtml", tripsInCart);
            }

            int userId = int.Parse(userIdString);
            List<int> tripIds = System.Text.Json.JsonSerializer.Deserialize<List<int>>(cartJson);

            decimal totalOrderPrice = 0;
            List<string> destinationNames = new List<string>();

            // 1. Process each trip in the cart
            foreach (var id in tripIds)
            {
                var trip = await _context.Trips.FindAsync(id);

                if (trip != null && trip.Stock > 0)
                {
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

                    totalOrderPrice += currentTripPrice;
                    destinationNames.Add(trip.Destination);

                    trip.Stock -= 1;
                    _context.Bookings.Add(booking);
                }
            }

            // 2. Save all bookings to Database
            await _context.SaveChangesAsync();

            // 3. SEND EMAIL (Requirement: Notification after payment)
            var user = await _context.Users.FindAsync(userId);
            if (user != null)
            {
                //  Sending the email right after saving to DB
                await _emailService.SendEmailAsync(user.Email, "Booking Confirmation",
                    $"Hi {user.FirstName}, your booking for {string.Join(", ", destinationNames)} is confirmed!");
            }

            // 4. Create the summary object for the View
            var summaryBooking = new Booking
            {
                PeopleCount = peopleCount,
                TotalPrice = totalOrderPrice,
                BookingDate = DateTime.Now,
                Trip = new Trip
                {
                    Destination = string.Join(", ", destinationNames)
                }
            };

            // 5. Clear the cart session
            HttpContext.Session.Remove("Cart");

            // 6. Return the confirmation view
            return View("~/Views/Booking/Confirmation.cshtml", summaryBooking);
        }
    }
}
