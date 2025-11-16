// Controllers/CartController.cs
using ClothingStore.Core.Entities;
using ClothingStore.Core.Interfaces;
using ClothingStore.Infrastructure.Data;
using ClothingStore.Infrastructure.Migrations;
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
                ProductName = x.Product.ProductName,
                ProductPrice = x.Product.ProductPrice,
                ProductQuantity = x.Quantity,
                ProductImageUrl = x.Product.ProductImageUrl
            }).ToList();

            ViewBag.Total = dbCart.Sum(x => x.Product.ProductPrice * x.Quantity);

            return View(new CheckoutViewModel());
        }
        // =============================
        //  CHECKOUT (POST)
        // =============================
        [HttpPost]
        public async Task<IActionResult> Checkout(CheckoutViewModel model)
        {
            if (!User.Identity.IsAuthenticated)
            {
                TempData["CheckoutError"] = "Please login to complete checkout.";
                return RedirectToAction("Login", "Account", new { returnUrl = "/Cart/Checkout" });
            }

            if (!ModelState.IsValid)
                return View(model);

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var cartItems = await _cartService.GetUserCartAsync();

            if (!cartItems.Any())
                return RedirectToAction("Index", "Cart");

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var order = new Order
                {
                    CustomerId = userId,
                    CustomerName = model.CustomerName,
                    Email = model.Email,
                    Address = model.Address,
                    PaymentMethod = model.PaymentMethod,
                    OrderDate = DateTime.UtcNow,
                    OrderStatus = OrderStatus.Pending,
                    TotalAmount = cartItems.Sum(x => x.ProductPrice * x.ProductQuantity)
                };

                _context.Orders.Add(order);
                await _context.SaveChangesAsync();
                var orderId = _context.Orders
                              .Where(o => o.CustomerId == userId)
                              .OrderByDescending(o => o.OrderDate)
                              .Select(o => o.OrderId)
                              .FirstOrDefault();

                foreach (var item in cartItems)
                {
                    var product = await _context.Products.FindAsync(item.ProductId);

                    if (product == null)
                        throw new Exception($"Product {item.ProductId} not found.");

                    if (product.ProductStockQuantity < item.ProductQuantity)
                        throw new Exception($"{product.ProductName} not enough stock.");

                    product.ProductStockQuantity -= item.ProductQuantity;

                    _context.OrderItems.Add(new OrderItem
                    {
                        OrderId = orderId,
                        ProductId = item.ProductId,
                        ProductName = product.ProductName,
                        ImageUrl = product.ProductImageUrl,
                        Quantity = item.ProductQuantity,
                        Price = product.ProductPrice
                    });
                }

                await _context.SaveChangesAsync();

                await _cartService.ClearUserCartAsync();
                await transaction.CommitAsync();

                // 🟦 ONLINE PAYMENT FLOW
                if (model.PaymentMethod == "Online")
                {
                    string url = Url.Action("Start", "Payments", new { orderId = orderId }, Request.Scheme);

                    return Json(new { razorpayStartUrl = url });
                }

                // 🟩 COD FLOW
                return RedirectToAction("Success", new { orderId = orderId });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                TempData["CheckoutError"] = "Checkout failed.";
                Console.WriteLine(ex.Message);
                return RedirectToAction("Index", "Cart");
            }
        }


        public async Task<IActionResult> Success(string OrderId)
        {
            var order = await _context.Orders.FindAsync(OrderId);
            ViewBag.OrderId = OrderId;
            //if (order == null || order.OrderId != User.FindFirstValue(ClaimTypes.NameIdentifier))
             //   return NotFound();
           // return View(order);
            return View();
        }
    }
}
