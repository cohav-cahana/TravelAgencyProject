using Microsoft.AspNetCore.Mvc;

namespace TravelAgencyProject.Controllers
{
    public class BookingController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
