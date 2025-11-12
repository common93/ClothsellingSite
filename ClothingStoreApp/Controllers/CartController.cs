using ClothingStore.Core.Entities;
using ClothingStore.Core.Interfaces;
using ClothingStore.Infrastructure.Data;
using ClothingStoreApp.Models;
using ClothingStoreApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ClothingStoreApp.Controllers
{
    //[Authorize] // Require login to persist cart in DB
    public class CartController : Controller
    {
        private readonly IProductRepository _productRepo;
        private readonly CartService _cartService;
        private readonly AppDbContext _context;

        public CartController(IProductRepository productRepo, CartService cartService, AppDbContext context)
        {
            _productRepo = productRepo;
            _cartService = cartService;
            _context = context;
        }

        // 🛒 Cart Overview
        public async Task<IActionResult> Index()
        {
            var items = await _cartService.GetCartItemsAsync();
            ViewBag.Total = items.Sum(x => x.Product.Price * x.Quantity);
            return View(items);
        }

        // ➕ Add to Cart
        //[HttpPost]
        public async Task<IActionResult> Add(int id)
        {
            var product = await _productRepo.GetByIdAsync(id);
            if (product == null) return NotFound();

            await _cartService.AddToCartAsync(id);
            return RedirectToAction("Index");
        }

        // ➖ Remove from Cart
        public async Task<IActionResult> Remove(int id)
        {
            await _cartService.RemoveFromCartAsync(id);
            return RedirectToAction("Index");
        }

        // 🔄 Clear Cart
        public async Task<IActionResult> Clear()
        {
            await _cartService.ClearCartAsync();
            return RedirectToAction("Index");
        }

        // 🔼 Increase quantity
        public async Task<IActionResult> Increase(int id)
        {
            var items = await _cartService.GetCartItemsAsync();
            var item = items.FirstOrDefault(i => i.ProductId == id);
            if (item != null)
            {
                await _cartService.AddToCartAsync(id, 1);
            }
            return RedirectToAction("Index");
        }

        // 🔽 Decrease quantity
        public async Task<IActionResult> Decrease(int id)
        {
            var cart = await _cartService.GetOrCreateCartAsync();
            var item = cart.Items.FirstOrDefault(i => i.ProductId == id);

            if (item != null)
            {
                item.Quantity--;
                if (item.Quantity <= 0)
                    _context.CartItems.Remove(item);

                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Index");
        }

        // ✅ Checkout Page
        public async Task<IActionResult> Checkout()
        {
            var items = await _cartService.GetCartItemsAsync();
            if (!items.Any()) return RedirectToAction("Index");

            ViewBag.Total = items.Sum(i => i.Product.Price * i.Quantity);
            return View(new CheckoutViewModel());
        }

        // ✅ Checkout POST
        [HttpPost]
        public async Task<IActionResult> Checkout(CheckoutViewModel model)
        {
            var items = await _cartService.GetCartItemsAsync();
            if (!items.Any()) return RedirectToAction("Index");

            var order = new Order
            {
                CustomerName = model.CustomerName,
                Email = model.Email,
                Address = model.Address,
                PaymentMethod = model.PaymentMethod,
                OrderStatus = OrderStatus.Pending,
                TotalAmount = items.Sum(i => i.Product.Price * i.Quantity),
                OrderDate = DateTime.UtcNow
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            // Add order items
            foreach (var i in items)
            {
                _context.OrderItems.Add(new OrderItem
                {
                    OrderId = order.Id,
                    ProductId = i.ProductId,
                    ProductName = i.Product.Name,
                    ImageUrl = i.Product.ImageUrl,
                    Price = i.Product.Price,
                    Quantity = i.Quantity
                });

                // Decrease stock
                var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == i.ProductId);
                if (product != null)
                {
                    product.StockQuantity -= i.Quantity;
                }
            }

            await _context.SaveChangesAsync();

            // Clear cart after checkout
            await _cartService.ClearCartAsync();

            return RedirectToAction("Success", new { id = order.Id });
        }

        // 🧾 Order Success Page
        public IActionResult Success(int id)
        {
            ViewBag.OrderId = id;
            return View();
        }
    }
}
