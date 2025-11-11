using ClothingStore.Core.Entities;
using ClothingStore.Core.Interfaces;
using ClothingStore.Infrastructure.Data;
using ClothingStoreApp.Models;
using ClothingStoreApp.Services;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

public class CartController : Controller
{
    private readonly IProductRepository _productRepo;
    private readonly AppDbContext _context;

    public CartController(IProductRepository productRepo, AppDbContext context)
    {
        _productRepo = productRepo;
        _context = context;
    }

    public IActionResult Index()
    {
        var cart = CartService.GetCart(HttpContext);
        return View(cart);
    }

    public async Task<IActionResult> Add(int id)
    {
        var product = await _productRepo.GetByIdAsync(id);
        if (product == null) return NotFound();

        var cart = CartService.GetCart(HttpContext);
        var item = cart.FirstOrDefault(c => c.ProductId == id);

        if (item == null)
        {
            cart.Add(new CartViewModel
            {
                ProductId = product.Id,
                Name = product.Name,
                Price = product.Price,
                ImageUrl = product.ImageUrl,
                Quantity = 1
            });
        }
        else item.Quantity++;

        CartService.SaveCart(HttpContext, cart);
        return RedirectToAction("Index");
    }

    public IActionResult Remove(int id)
    {
        var cart = CartService.GetCart(HttpContext);
        cart.RemoveAll(c => c.ProductId == id);
        CartService.SaveCart(HttpContext, cart);

        return RedirectToAction("Index");
    }

    public IActionResult Clear()
    {
        HttpContext.Session.Remove("cart");
        return RedirectToAction("Index");
    }

    public IActionResult Increase(int id)
    {
        var cart = CartService.GetCart(HttpContext);
        var item = cart.FirstOrDefault(c => c.ProductId == id);

        if (item != null)
            item.Quantity++;

        CartService.SaveCart(HttpContext, cart);
        return RedirectToAction("Index");
    }

    public IActionResult Decrease(int id)
    {
        var cart = CartService.GetCart(HttpContext);
        var item = cart.FirstOrDefault(c => c.ProductId == id);

        if (item != null)
        {
            item.Quantity--;
            if (item.Quantity <= 0)
                cart.Remove(item);
        }

        CartService.SaveCart(HttpContext, cart);
        return RedirectToAction("Index");
    }
    public IActionResult Checkout()
    {
        var cart = CartService.GetCart(HttpContext);
        if (!cart.Any()) return RedirectToAction("Index");

        ViewBag.Cart = cart;
        ViewBag.Total = cart.Sum(x => x.Price * x.Quantity);

        return View(new CheckoutViewModel());
    }

    [HttpPost]
    public async Task<IActionResult> Checkout(CheckoutViewModel model)
    {
        var cart = CartService.GetCart(HttpContext);
        if (!cart.Any()) return RedirectToAction("Index");

        var order = new Order
        {
            CustomerName = model.CustomerName,
            Email = model.Email,
            Address = model.Address,
            PaymentMethod = model.PaymentMethod,
            OrderStatus = "Pending",
            TotalAmount = cart.Sum(c => c.Price * c.Quantity)
        };

        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        foreach (var item in cart)
        {
            _context.OrderItems.Add(new OrderItem
            {
                OrderId = order.Id,
                ProductId = item.ProductId,
                ProductName = item.Name,
                ImageUrl = item.ImageUrl,
                Price = item.Price,
                Quantity = item.Quantity
            });
        }

        await _context.SaveChangesAsync();

        HttpContext.Session.Remove("cart"); // clear cart

        return RedirectToAction("Success", new { id = order.Id });
    }
    public IActionResult Success(int id)
    {
        ViewBag.OrderId = id;
        return View();
    }

}
