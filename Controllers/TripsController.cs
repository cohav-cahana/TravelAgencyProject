using Microsoft.AspNetCore.Mvc;

namespace TravelAgencyProject.Controllers
{
    public class TripsController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
