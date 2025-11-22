using Microsoft.AspNetCore.Mvc;

namespace TravelAgencyProject.Controllers
{
    public class AccountController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
