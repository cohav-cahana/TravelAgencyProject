using Microsoft.AspNetCore.Mvc;

namespace TravelAgencyProject.Controllers
{
    public class AdminController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
