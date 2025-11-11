using ClothingStore.Core.Entities;
using ClothingStore.Core.Interfaces;
using ClothingStore.Infrastructure.Data;
using ClothingStoreApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClothingStoreApp.Controllers
{
    public class ProductController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IProductRepository _repo;
        public ProductController(AppDbContext context, IProductRepository repo)
        {
            _context = context;
            _repo = repo;
        }

        //public async Task<IActionResult> index()
        //{
        //    var products = await _context.products.tolistasync();
        //    return view(products);
        //}

        public async Task<IActionResult> Index()
        {
            var products = await _repo.GetAllAsync();
            return View(products);
        }

        public async Task<IActionResult> Details(int id)
        {
            var product = await _repo.GetByIdAsync(id);
            if (product == null) return NotFound();
            return View(product);
        }

        // Create Product (GET)
        public IActionResult Create()
        {
            return View();
        }

        // Create Product (POST)
        [HttpPost]
        public async Task<IActionResult> Create(ProductViewModel model)
        {
            if (ModelState.IsValid)
            {
                var product = new Product
                {
                    Name = model.Name,
                    Description = model.Description,
                    Price = model.Price,
                    Category = model.Category,
                    StockQuantity = model.StockQuantity,
                    ImageUrl = model.ImageUrl
                };

                await _repo.AddAsync(product);
                return RedirectToAction("Index");
            }
            return View(model);
        }

        // Edit Product (GET)
        public async Task<IActionResult> Edit(int id)
        {
            var product = await _repo.GetByIdAsync(id);
            if (product == null) return NotFound();

            var vm = new ProductViewModel
            {
                Id = product.Id,
                Name = product.Name,
                Description = product.Description,
                Price = product.Price,
                Category = product.Category,
                StockQuantity = product.StockQuantity,
                ImageUrl = product.ImageUrl
            };
            return View(vm);
        }

        // Edit Product (POST)
        [HttpPost]
        public async Task<IActionResult> Edit(ProductViewModel model)
        {
            if (ModelState.IsValid)
            {
                var product = await _repo.GetByIdAsync(model.Id);
                if (product == null) return NotFound();

                product.Name = model.Name;
                product.Description = model.Description;
                product.Price = model.Price;
                product.Category = model.Category;
                product.StockQuantity = model.StockQuantity;
                product.ImageUrl = model.ImageUrl;

                await _repo.UpdateAsync(product);
                return RedirectToAction("Index");
            }
            return View(model);
        }

        // Delete Product
        public async Task<IActionResult> Delete(int id)
        {
            await _repo.DeleteAsync(id);
            return RedirectToAction("Index");
        }
    }
}
