using Microsoft.AspNetCore.Mvc;

namespace TravelAgencyProject.Controllers
{
    public class ReviewsController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
