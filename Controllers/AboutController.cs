using Microsoft.AspNetCore.Mvc;

namespace KBN.Controllers
{
    public class AboutController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
