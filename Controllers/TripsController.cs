using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TravelAgencyProject.Data;
using TravelAgencyProject.Models;

namespace TravelAgencyProject.Controllers
{
    public class TripsController : Controller
    {

        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment; //Help us to find the wwwroot (images folder)
        public TripsController(AppDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
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
                "price_asc" => trips.OrderBy(t => t.SalePrice ?? t.Price), // מהזול ליקר
                "price_desc" => trips.OrderByDescending(t => t.SalePrice ?? t.Price), // מהיקר לזול
                "destination" => trips.OrderBy(t => t.Destination), // לפי א'-ב' של יעד
                _ => trips.OrderBy(t => t.StartDate) // ברירת מחדל: לפי תאריך יציאה
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

        public async Task<IActionResult> Checkout(int tripId)
        {
            var userIdString = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdString))
            {
                return RedirectToAction("Login", "Account");
            }

            var trip = await _context.Trips.FindAsync(tripId);
            if (trip == null) return NotFound();

            if (trip.Stock <= 0)
            {
                return View("~/Views/Booking/WaitingListNotice.cshtml", trip);
            }
            var booking = new Booking
            {
                TripId = trip.TripId,
                Trip = trip,
                UserId = int.Parse(userIdString),
                TotalPrice = trip.Price
            };

            return View("~/Views/Booking/Checkout.cshtml", booking);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessBooking(Booking booking)
        {
            var trip = await _context.Trips.FindAsync(booking.TripId);

            if (trip != null)
            {
                booking.TotalPrice = trip.Price * booking.PeopleCount;
            }

            ModelState.Remove("User");
            ModelState.Remove("Trip");
            ModelState.Remove("TotalPrice");

            if (ModelState.IsValid)
            {
                if (trip != null)
                {
                    if (trip.Stock <= 0)
                    {
                        TempData["Error"] = "Sorry, this trip is now fully booked.";
                        return RedirectToAction("Index");
                    }

                    trip.Stock -= 1;
                    _context.Update(trip);
                }

                booking.BookingDate = DateTime.Now;
                booking.PaymentStatus = PaymentStatus.Completed;
                booking.bookingStatus = TripStatus.Upcoming;

                _context.Bookings.Add(booking);

                await _context.SaveChangesAsync();

                booking.Trip = trip;
                return View("~/Views/Booking/Confirmation.cshtml", booking);
            }

            booking.Trip = trip;
            return View("~/Views/Booking/Checkout.cshtml", booking);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelBooking(int id)
        {
            var booking = await _context.Bookings
                .Include(b => b.Trip)
                .FirstOrDefaultAsync(b => b.BookingId == id);

            if (booking != null)
            {
                if (booking.Trip != null)
                {
                    booking.Trip.Stock += booking.PeopleCount;
                    _context.Update(booking.Trip);
                }

                booking.bookingStatus = TripStatus.Cancelled;
                await _context.SaveChangesAsync();
            }

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
        public async Task<IActionResult> AddToCart(int id)
        {
            // Get current cart from session
            var cartJson = HttpContext.Session.GetString("Cart");
            List<int> cart = string.IsNullOrEmpty(cartJson)
                ? new List<int>()
                : System.Text.Json.JsonSerializer.Deserialize<List<int>>(cartJson);

            // Get UserId from session to check for active bookings in DB
            var userIdString = HttpContext.Session.GetString("UserId");
            int activeBookingsCount = 0;

            if (!string.IsNullOrEmpty(userIdString))
            {
                int userId = int.Parse(userIdString);
                // Count existing bookings with 'Upcoming' status for this user
                activeBookingsCount = await _context.Bookings
                    .CountAsync(b => b.UserId == userId && b.bookingStatus == TripStatus.Upcoming);
            }

            // Limit check: Total active trips (bookings + cart) cannot exceed 3
            if (activeBookingsCount + cart.Count >= 3)
            {
                // Display error message and stop the process
                TempData["Error"] = "You can only have up to 3 active trips in total (including your cart and bookings).";
                return RedirectToAction("Index");
            }

            // Add trip ID to the cart list
            cart.Add(id);

            // Save updated cart back to session
            HttpContext.Session.SetString("Cart", System.Text.Json.JsonSerializer.Serialize(cart));

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
        public async Task<IActionResult> ProcessCartBooking(int peopleCount)
        {
            var userIdString = HttpContext.Session.GetString("UserId");
            var cartJson = HttpContext.Session.GetString("Cart");

            if (string.IsNullOrEmpty(userIdString) || string.IsNullOrEmpty(cartJson))
            {
                return RedirectToAction("Index");
            }

            int userId = int.Parse(userIdString);
            List<int> tripIds = System.Text.Json.JsonSerializer.Deserialize<List<int>>(cartJson);

            // Loop through each trip in the cart and create a Booking entry
            foreach (var id in tripIds)
            {
                var trip = await _context.Trips.FindAsync(id);

                // Ensure trip exists and has available stock
                if (trip != null && trip.Stock > 0)
                {
                    var booking = new Booking
                    {
                        UserId = userId,
                        TripId = id,
                        PeopleCount = peopleCount,
                        TotalPrice = (trip.SalePrice ?? trip.Price) * peopleCount,
                        BookingDate = DateTime.Now,
                        PaymentStatus = PaymentStatus.Completed,
                        bookingStatus = TripStatus.Upcoming
                    };

                    // Reduce the stock count in the database
                    trip.Stock -= 1;
                    _context.Bookings.Add(booking);
                }
            }

            // Save all changes to the database
            await _context.SaveChangesAsync();

            // Clear the shopping cart session after successful payment
            HttpContext.Session.Remove("Cart");

            TempData["Message"] = "Thank you! All your trips have been successfully booked.";
            return RedirectToAction("Index", "Booking");
        }
    }
}
