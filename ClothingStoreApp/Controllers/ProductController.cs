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

        //public async Task<IActionResult> Index()
        //{
        //    var products = await _repo.GetAllAsync();
        //    return View(products);
        //}

        public async Task<IActionResult> Index(string category, string search, int page = 1, int pageSize = 12)
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

        [HttpGet]
        public async Task<IActionResult> ProductsPartial(string category, string search, int page = 1, int pageSize = 12)
        {
            var query = _context.Products.AsQueryable();

            if (!string.IsNullOrEmpty(category))
                query = query.Where(p => p.ProductCategory.ToLower() == category.ToLower());

            if (!string.IsNullOrEmpty(search))
                query = query.Where(p => p.ProductName.Contains(search) || p.ProductDescription.Contains(search));

            var totalItems = await query.CountAsync();
            var products = await query
                .OrderBy(p => p.ProductName)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var model = new PagedProductsViewModel
            {
                Products = products,
                CurrentPage = page,
                TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize)
            };

            return PartialView("_ProductsPartial", model);
        }


        public async Task<IActionResult> Products(string category, string search, int page = 1, int pageSize = 12)
        {

            var allProducts =  _context.Products.AsQueryable();
            var total = allProducts.Count();

            // ✅ Category Filter
            if (!string.IsNullOrEmpty(category))
                allProducts = allProducts.Where(p => p.ProductCategory.ToLower() == category.ToLower());

            // ✅ Search Filter
            if (!string.IsNullOrEmpty(search))
                allProducts = allProducts.Where(p =>
                    p.ProductName.Contains(search) ||
                    p.ProductDescription.Contains(search) ||
                    p.ProductCategory.Contains(search)
                );

            var products = allProducts
                            .OrderBy(x => x.ProductId)
                            .Skip((page - 1) * pageSize)
                            .Take(pageSize)
                            .ToList();

            var vm = new PagedProductsViewModel
            {
                Products = products,
                CurrentPage = page,
                TotalPages = (int)Math.Ceiling(total / (double)pageSize)
            };

            return View(vm);
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
    }
}
