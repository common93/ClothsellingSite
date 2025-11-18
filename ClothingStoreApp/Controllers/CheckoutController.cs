using ClothingStore.Core.Entities;
using ClothingStore.Infrastructure.Data;
using ClothingStoreApp.Models;
using ClothingStoreApp.Services;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Text;

namespace ClothingStoreApp.Controllers
{
    public class CheckoutController : Controller
    {
        private readonly RazorpayService _razorpay;
        private readonly AppDbContext _db;
        private readonly ICartService _cartService;

        public CheckoutController(ICartService cartService, RazorpayService razorpay, AppDbContext db)
        {
            _razorpay = razorpay;
            _db = db;
        }

        // Show checkout view
        public async Task<IActionResult> Index()
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

            var model = new RazorpayCheckoutViewModel
            {
                Key = _razorpay.GetKey(),
                Amount = dbCart.Sum(x => x.Product.ProductPrice * x.Quantity)
            };

            return View("Checkout", model);
        }


        // 🚀 Create Razorpay Order
        [HttpPost]
        public async Task<IActionResult> CreateOrder(decimal amount)
        {
            try
            {
                string receipt = Guid.NewGuid().ToString();   // unique receipt ID

                string razorpayOrderId = await _razorpay.CreateOrder(amount, receipt);

                // Save order in DB
                var order = new Order
                {
                    RazorpayOrderId = razorpayOrderId,
                    TotalAmount = amount,
                    PaymentStatus = PaymentStatus.Pending,
                    OrderStatus = OrderStatus.Pending,
                    OrderDate = DateTime.UtcNow
                };

                _db.Orders.Add(order);
                await _db.SaveChangesAsync();

                return Json(new { orderId = razorpayOrderId, key = _razorpay.GetKey() });
            }
            catch (Exception)
            {

                throw;
            }
        }


        // 🛡 Verify signature (Frontend → Backend)
        [HttpPost]
        public IActionResult Verify([FromBody] RazorpayVerifyRequest response)
        {
            bool isValid = _razorpay.VerifySignature(
                response.razorpay_order_id,
                response.razorpay_payment_id,
                response.razorpay_signature
            );

            if (!isValid)
                return Json(new { success = false });

            // Only TEMP mark paid → real confirmation comes from webhook
            var order = _db.Orders
                .FirstOrDefault(o => o.RazorpayOrderId == response.razorpay_order_id);

            if (order != null)
            {
                order.RazorpayPaymentId = response.razorpay_payment_id;
                order.PaymentStatus = PaymentStatus.Pending; // will be updated by webhook
                _db.SaveChanges();
            }

            return Json(new
            {
                success = true,
                redirect = Url.Action("Success")
            });
        }


        public IActionResult Success()
        {
            return View();
        }
    }


    public class RazorpayVerifyRequest
    {
        public string razorpay_payment_id { get; set; }
        public string razorpay_order_id { get; set; }
        public string razorpay_signature { get; set; }
    }
}
