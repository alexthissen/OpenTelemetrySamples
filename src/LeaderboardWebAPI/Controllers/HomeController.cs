using Microsoft.AspNetCore.Mvc;

namespace LeaderboardWebAPI.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return new RedirectResult("~/openapi");
        }
    }
}
