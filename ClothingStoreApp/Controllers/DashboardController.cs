using ClothingStore.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ClothingStore.Core.Entities;

namespace ClothingStoreApp.Controllers
{
   // [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class DashboardController : Controller
    {
        private readonly AppDbContext _context;

        public DashboardController(AppDbContext context)
        {
            _context = context;
        }

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

        [Area("Admin")]
        [Authorize(Roles = "Admin")]
       // [Route("Admin/Products")]
        public async Task<IActionResult> Index(string category)
        {
            var categories = await _context.Products
                .Select(p => p.Category)
                .Distinct()
                .ToListAsync();

            ViewBag.Categories = categories;

            var products = _context.Products.AsQueryable();

            if (!string.IsNullOrEmpty(category))
            {
                products = products.Where(p => p.Category == category);
            }

            return View("ProductsList", await products.ToListAsync());
        }

        // GET: /Admin/Products/Edit/1
        [HttpGet]
        [Authorize(Roles = "Admin")]
        [Route("Product")]
        public async Task<IActionResult> Edit(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound();

            return View(product); // loads Areas/Admin/Views/Products/Edit.cshtml
        }

        // POST: /Admin/Products/Edit/1
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [Route("Product")]
        public async Task<IActionResult> Edit(Product model)
        {
            if (!ModelState.IsValid) return View(model);

            _context.Products.Update(model);
            await _context.SaveChangesAsync();

            return RedirectToAction("Index");
        }

        // GET: /Admin/Products/Delete/1
        [HttpGet]
        [Authorize(Roles = "Admin")]
        [Route("Product")]
        public async Task<IActionResult> Delete(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound();

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();

            return RedirectToAction("Index");
        }
    }
}
