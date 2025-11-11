using ClothingStore.Core.Interfaces;
using ClothingStoreApp.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace ClothingStoreApp.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IProductRepository _repo;
        public HomeController(ILogger<HomeController> logger, IProductRepository repo)
        {
            _logger = logger;
            _repo = repo;
        }

        //public IActionResult Index()
        //{
        //    return View();
        //}
        public async Task<IActionResult> Index()
        {
            var products = await _repo.GetAllAsync();
            return View(products);
        }


        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
