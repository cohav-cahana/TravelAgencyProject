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
        public IActionResult Index()
        {
            return View(_context.Trips.ToList());
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
            ModelState.Remove("ImageUrl");// We will set it in the code below
            if (ModelState.IsValid)
            {
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
                    // Set a default image if no image is uploaded
                    trip.ImageUrl = "/images/trips/default.png";
                }

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
            // Only admin acn delete trips
            if (HttpContext.Session.GetString("IsAdmin") != "true")
            {
                return RedirectToAction("Login", "Account");
            }

            var trip = await _context.Trips
                .FirstOrDefaultAsync(m => m.TripId == id);

            if (trip == null)
            {
                return NotFound();
            }

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
    }
}
