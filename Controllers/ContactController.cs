using Microsoft.AspNetCore.Mvc;

namespace MyMvcPostgresApp.Controllers
{
    public class ContactController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}