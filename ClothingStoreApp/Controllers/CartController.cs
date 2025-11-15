// Controllers/CartController.cs
using ClothingStore.Core.Entities;
using ClothingStore.Core.Interfaces;
using ClothingStore.Infrastructure.Data;
using ClothingStoreApp.Models;
using ClothingStoreApp.Services;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ClothingStoreApp.Controllers
{
    public class CartController : Controller
    {
        private readonly ICartService _cartService;
        private readonly IProductRepository _productRepo;
        private readonly AppDbContext _context;

        public CartController(ICartService cartService, IProductRepository productRepo, AppDbContext context)
        {
            _cartService = cartService;
            _productRepo = productRepo;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!string.IsNullOrEmpty(userId))
            {
                var dbItems = await _cartService.GetDbCartItemsAsync();

                return View(new CartPageViewModel
                {
                    DbCart = dbItems
                });
            }
            else
            {
                var sessionItems = await _cartService.GetSessionCartAsync();

                return View(new CartPageViewModel
                {
                    SessionCart = sessionItems
                });
            }
        }

        // Allow GET for simple link, or use POST forms for CSRF-safe approach
        [HttpGet]
        public async Task<IActionResult> Add(int id)
        {
            await _cartService.AddToCartAsync(id, 1);
            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Remove(int id)
        {
            await _cartService.RemoveFromCartAsync(id);
            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Increase(int id)
        {
            await _cartService.IncreaseAsync(id);
            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Decrease(int id)
        {
            await _cartService.DecreaseAsync(id);
            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Clear()
        {
            await _cartService.ClearCartAsync();
            return RedirectToAction("Index");
        }
        public async Task<IActionResult> Checkout()
        {
            // 👤 Check login status
            if (!User.Identity.IsAuthenticated)
            {
                TempData["CheckoutError"] = "Please login to continue with checkout.";
                return RedirectToAction("Login", "Account", new { returnUrl = "/Cart/Checkout" });
            }

            // ✔ User is logged in → use their DB cart
            var dbCart = await _cartService.GetDbCartItemsAsync();

            if (dbCart == null || !dbCart.Any())
                return RedirectToAction("Index", "Cart");

            ViewBag.Cart = dbCart.Select(x => new CartViewModel
            {
                ProductId = x.ProductId,
                Name = x.Product.Name,
                Price = x.Product.Price,
                Quantity = x.Quantity,
                ImageUrl = x.Product.ImageUrl
            }).ToList();

            ViewBag.Total = dbCart.Sum(x => x.Product.Price * x.Quantity);

            return View(new CheckoutViewModel());
        }
        // =============================
        //  CHECKOUT (POST)
        // =============================
        [HttpPost]
        public async Task<IActionResult> Checkout(CheckoutViewModel model)
        {
            // 1️⃣ Must Login Before Checkout
            if (!User.Identity.IsAuthenticated)
            {
                TempData["CheckoutError"] = "Please login to complete checkout.";
                return RedirectToAction("Login", "Account", new { returnUrl = "/Cart/Checkout" });
            }

            // 2️⃣ Validate Model
            if (!ModelState.IsValid)
                return View(model);

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // 3️⃣ ALWAYS load cart using unified method
            var cartItems = await _cartService.GetUserCartAsync();

            if (cartItems == null || !cartItems.Any())
                return RedirectToAction("Index", "Cart");

            using var transaction = await _context.Database.BeginTransactionAsync();


            try
            {
                // 4️⃣ Create Order
                var order = new Order
                {
                    CustomerId = int.Parse(userId),
                    CustomerName = model.CustomerName,
                    Email = model.Email,
                    Address = model.Address,
                    PaymentMethod = model.PaymentMethod,
                    OrderDate = DateTime.UtcNow,
                    OrderStatus = OrderStatus.Pending,
                    TotalAmount = cartItems.Sum(x => x.Price * x.Quantity)
                };

                _context.Orders.Add(order);
                await _context.SaveChangesAsync();

                // 5️⃣ Insert Order Items + Update Stock
                foreach (var item in cartItems)
                {
                    var product = await _context.Products.FindAsync(item.ProductId);

                    if (product == null)
                        throw new Exception($"Product {item.ProductId} not found.");

                    if (product.StockQuantity < item.Quantity)
                        throw new Exception($"{product.Name} is out of stock.");

                    product.StockQuantity -= item.Quantity;

                    _context.OrderItems.Add(new OrderItem
                    {
                        OrderId = order.Id,
                        ProductId = item.ProductId,
                        ProductName = product.Name,
                        ImageUrl = product.ImageUrl,
                        Quantity = item.Quantity,
                        Price = product.Price
                    });
                }

                await _context.SaveChangesAsync();

                // 6️⃣ Clear cart with hybrid logic
                await _cartService.ClearUserCartAsync();

                await transaction.CommitAsync();

                return RedirectToAction("Success", new { id = order.Id });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                Console.WriteLine("Checkout Error: " + ex.Message);

                TempData["CheckoutError"] = "Something went wrong during checkout.";
                return RedirectToAction("Index", "Cart");
            }
        }


    }
}
