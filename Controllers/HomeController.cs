using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TravelAgencyProject.Data;
using TravelAgencyProject.Models;

namespace TravelAgencyProject.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly AppDbContext _context;
        public HomeController(ILogger<HomeController> logger, AppDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // Counts the total number of trip packages available in the database
            ViewBag.TotalTripsCount = await _context.Trips.CountAsync();

            // Fetch top 3 latest reviews where TripId is null (General Service Reviews)
            // Ratings filtered for 4 stars and above
            var serviceReviews = await _context.Reviews
                .Include(r => r.User)
                .Where(r => r.TripId == null && r.Rating >= 4)
                .OrderByDescending(r => r.PostedDate)
                .Take(3)
                .ToListAsync();

            return View(serviceReviews);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
