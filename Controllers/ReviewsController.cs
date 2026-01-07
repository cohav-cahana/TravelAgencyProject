using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TravelAgencyProject.Models;
using TravelAgencyProject.Data;

namespace TravelAgencyProject.Controllers
{
    public class ReviewsController : Controller
    {
        // 1. These must be INSIDE the class
        private readonly AppDbContext _context;

        public ReviewsController(AppDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("TripId,UserId,Rating,Comment")] Review review)
        {
            // Security Check: Block Admins from posting reviews on the server side
            if (HttpContext.Session.GetString("IsAdmin") == "true")
            {
                TempData["Error"] = "Admins are not allowed to post reviews.";
                return RedirectToAction("Details", "Trips", new { id = review.TripId });
            }
            // 1. Manually set the posting date to now
            review.PostedDate = DateTime.Now;

            // 2. Remove validation for properties we don't send from the form
            ModelState.Remove("User");
            ModelState.Remove("Trip");

            if (ModelState.IsValid)
            {
                // 3. Save the review to the database
                _context.Add(review);
                await _context.SaveChangesAsync();

                // 4. CRITICAL: Redirect back to the Trip Details page
                return RedirectToAction("Details", "Trips", new { id = review.TripId });
            }

            // If something fails, redirect back anyway with the error
            TempData["Error"] = "Failed to post review. Please ensure all fields are correct.";
            return RedirectToAction("Details", "Trips", new { id = review.TripId });
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        // Only Admins are allowed to delete reviews to maintain site integrity
        // Regular users can only post reviews, not delete them.
        public async Task<IActionResult> Delete(int id, int tripId)
        {
            // Security Check: Ensure only Admins can delete
            if (HttpContext.Session.GetString("IsAdmin") != "true")
            {
                return RedirectToAction("Login", "Account");
            }

            var review = await _context.Reviews.FindAsync(id);
            if (review != null)
            {
                _context.Reviews.Remove(review);
                await _context.SaveChangesAsync();
                TempData["Message"] = "The review was successfully deleted.";
            }

            // Redirect back to the Trip Details page where the admin was
            return RedirectToAction("Details", "Trips", new { id = tripId });
        }
    }

}