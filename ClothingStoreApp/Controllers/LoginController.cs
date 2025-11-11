using Microsoft.AspNetCore.Mvc;

namespace ClothingStoreApp.Controllers
{
    public class LoginController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
