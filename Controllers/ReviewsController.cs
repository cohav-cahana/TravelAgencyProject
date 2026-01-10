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
        public async Task<IActionResult> Create([Bind("TripId,Rating,Comment")] Review review, string returnTo = "Trip")
        {
            // Retrieve UserId as a String from session (Project default) and parse to Int
            var userIdStr = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdStr))
            {
                return RedirectToAction("Login", "Account");
            }

            // Set administrative fields
            review.UserId = int.Parse(userIdStr);
            review.PostedDate = DateTime.Now;

            // Remove navigation objects from validation to avoid False-Negative results
            ModelState.Remove("User");
            ModelState.Remove("Trip");

            if (ModelState.IsValid)
            {
                _context.Add(review);
                await _context.SaveChangesAsync();

                // Redirect back to Home if the review was submitted from the Confirmation page
                if (returnTo == "Home")
                {
                    return RedirectToAction("Index", "Home");
                }

                // Otherwise, redirect back to the specific trip details
                return RedirectToAction("Details", "Trips", new { id = review.TripId });
            }

            // Fallback redirect if validation fails
            return RedirectToAction("Index", "Home");
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