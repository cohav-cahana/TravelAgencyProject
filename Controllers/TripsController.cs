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
        public IActionResult Index(string searchString)
        {
            var trips = from t in _context.Trips
                        select t;

            if (!string.IsNullOrEmpty(searchString))
            {
                trips = trips.Where(s => s.Destination.Contains(searchString)
                                      || s.Country.Contains(searchString)
                                      || s.Category.Contains(searchString)
                                      || s.Description.Contains(searchString));
            }

            ViewData["CurrentFilter"] = searchString;
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
    }
}
