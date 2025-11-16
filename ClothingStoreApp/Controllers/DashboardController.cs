using ClothingStore.Core.Entities;
using ClothingStore.Core.Interfaces;
using ClothingStore.Infrastructure.Data;
using ClothingStoreApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClothingStoreApp.Controllers
{
   // [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class DashboardController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IProductRepository _repo;
        public DashboardController (AppDbContext context, IProductRepository repo)
        {
            _context = context;
            _repo = repo;
        }
        //public DashboardController(AppDbContext context)
        //{
        //    _context = context;
        //}

        public async Task<IActionResult> Index()
        {
            // ✅ KPI Summary Counts
            ViewBag.TotalOrders = await _context.Orders.CountAsync();
            ViewBag.TotalSales = await _context.Orders.SumAsync(o => o.TotalAmount);
            ViewBag.TotalUsers = await _context.Users.CountAsync(); // Identity Users

            // ✅ Daily Sales - Last 30 days timeline
            var dailySalesData = await _context.Orders
                .Where(o => o.OrderDate >= DateTime.UtcNow.AddDays(-30))
                .OrderBy(o => o.OrderDate)
                .Select(o => new
                {
                    Date = o.OrderDate,
                    Amount = o.TotalAmount
                })
                .ToListAsync();

            // ✅ Format for Chart.js (Datetime + Amount)
            ViewBag.OrderDates = dailySalesData
                .Select(d => d.Date.ToString("yyyy-MM-dd HH:mm"))
                .ToList();

            ViewBag.SalesAmounts = dailySalesData
                .Select(d => d.Amount)
                .ToList();

            return View();
        }
        public async Task<IActionResult> AdminProductList(string category, string search, int page = 1, int pageSize = 12)
        {
            var products = _context.Products.AsQueryable();

            // ✅ Category Filter
            if (!string.IsNullOrEmpty(category))
                products = products.Where(p => p.ProductCategory.ToLower() == category.ToLower());

            // ✅ Search Filter
            if (!string.IsNullOrEmpty(search))
                products = products.Where(p =>
                    p.ProductName.Contains(search) ||
                    p.ProductDescription.Contains(search) ||
                    p.ProductCategory.Contains(search)
                );

            // ✅ Pagination logic
            var totalItems = await products.CountAsync();
            var data = await products
                .OrderBy(p => p.ProductName)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            ViewBag.Search = search;
            ViewBag.Category = category;

            return View(data);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateStock(int id, int newStock)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
                return NotFound();

            product.ProductStockQuantity = newStock;
            await _context.SaveChangesAsync();

            return Ok(new { success = true });
        }

                // Create Product (GET)
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(ProductViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            string imagePath = null;

            if (model.ProductImage != null)
            {
                string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/products");
                Directory.CreateDirectory(uploadsFolder); // ensure folder exists

                string fileName = Guid.NewGuid().ToString() + Path.GetExtension(model.ProductImage.FileName);
                string filePath = Path.Combine(uploadsFolder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await model.ProductImage.CopyToAsync(stream);
                }

                imagePath = "/images/products/" + fileName;
            }

            if (imagePath == null)
                imagePath = "";

            var product = new Product
            {
                ProductName = model.Name,
                ProductDescription = model.Description,
                ProductPrice = model.Price,
                ProductCategory = model.Category,
                ProductStockQuantity = model.StockQuantity,
                //ImageUrl = model.ImageUrl,
                ProductImageUrl = imagePath
            };

            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            TempData["success"] = "Product added successfully ✅";
            return RedirectToAction("Index");
        }

        // Edit Product (GET)
        public async Task<IActionResult> Edit(int id)
        {
            var product = await _repo.GetByIdAsync(id);
            if (product == null) return NotFound();

            var vm = new ProductViewModel
            {
                Id = product.ProductId,
                Name = product.ProductName,
                Description = product.ProductDescription,
                Price = product.ProductPrice,
                Category = product.ProductCategory,
                StockQuantity = product.ProductStockQuantity,
                ImageUrl = product.ProductImageUrl
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


                string imagePath = null;
                if (model.ProductImage != null)
                {
                    string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/products");
                    Directory.CreateDirectory(uploadsFolder); // ensure folder exists

                    string fileName = Guid.NewGuid().ToString() + Path.GetExtension(model.ProductImage.FileName);
                    string filePath = Path.Combine(uploadsFolder, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await model.ProductImage.CopyToAsync(stream);
                    }

                    imagePath = "/images/products/" + fileName;
                }

                if (imagePath == null)
                    imagePath = "";

                product.ProductName = model.Name;
                product.ProductDescription = model.Description;
                product.ProductPrice = model.Price;
                product.ProductCategory = model.Category;
                product.ProductStockQuantity = model.StockQuantity;
                product.ProductImageUrl = imagePath;

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
        //[Area("Admin")]
        //[Authorize(Roles = "Admin")]
        //[Route("Admin/Products")]
        //public async Task<IActionResult> Index(string category)
        //{
        //    var categories = await _context.Products
        //        .Select(p => p.Category)
        //        .Distinct()
        //        .ToListAsync();

        //    ViewBag.Categories = categories;

        //    var products = _context.Products.AsQueryable();

        //    if (!string.IsNullOrEmpty(category))
        //    {
        //        products = products.Where(p => p.Category == category);
        //    }

        //    return View("ProductsList", await products.ToListAsync());
        //}

        //// GET: /Admin/Products/Edit/1
        //[HttpGet]
        //[Authorize(Roles = "Admin")]
        //[Route("Product")]
        //public async Task<IActionResult> Edit(int id)
        //{
        //    var product = await _context.Products.FindAsync(id);
        //    if (product == null) return NotFound();

        //    return View(product); // loads Areas/Admin/Views/Products/Edit.cshtml
        //}

        //// POST: /Admin/Products/Edit/1
        //[HttpPost]
        //[Authorize(Roles = "Admin")]
        //[Route("Product")]
        //public async Task<IActionResult> Edit(Product model)
        //{
        //    if (!ModelState.IsValid) return View(model);

        //    _context.Products.Update(model);
        //    await _context.SaveChangesAsync();

        //    return RedirectToAction("Index");
        //}

        //// GET: /Admin/Products/Delete/1
        //[HttpGet]
        //[Authorize(Roles = "Admin")]
        //[Route("Product")]
        //public async Task<IActionResult> Delete(int id)
        //{
        //    var product = await _context.Products.FindAsync(id);
        //    if (product == null) return NotFound();

        //    _context.Products.Remove(product);
        //    await _context.SaveChangesAsync();

        //    return RedirectToAction("Index");
        //}
    }
}
