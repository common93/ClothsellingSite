using ClothingStore.Core.Entities;
using ClothingStore.Infrastructure.Data;
using ClothingStoreApp.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Security.Claims;
using static System.Net.WebRequestMethods;

namespace ClothingStoreApp.Services
{
    public class CartService
    {
        private readonly AppDbContext _context;
        private readonly IHttpContextAccessor _contextAccessor;

        private const string SessionCartKey = "SESSION_CART";

        public CartService(AppDbContext context, IHttpContextAccessor contextAccessor)
        {
            _context = context;
            _contextAccessor = contextAccessor;
        }

        // ---------------------------------------
        //  SHORTCUTS
        // ---------------------------------------
        private HttpContext HttpContext => _contextAccessor.HttpContext;

        public string? UserId =>
            HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        public bool IsUserLoggedIn() =>
            !string.IsNullOrEmpty(HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value);


        // =====================================================
        //  🔹 SESSION CART FUNCTIONS (Guest Users)
        // =====================================================
        private List<CartViewModel> GetSessionCart()
        {
            var data = HttpContext.Session.GetString(SessionCartKey);
            return data == null ? new List<CartViewModel>()
                                : JsonConvert.DeserializeObject<List<CartViewModel>>(data);
        }

        private void SaveSessionCart(List<CartViewModel> cart)
        {
            HttpContext.Session.SetString(SessionCartKey,
                JsonConvert.SerializeObject(cart));
        }


        // =====================================================
        //  🔹 DB CART FUNCTIONS (Logged-in Users)
        // =====================================================
        private async Task<List<CartItem>> GetDbCart()
        {
            return await _context.CartItems
                .Include(p => p.Product)
               .Where(x => x.Cart.UserId == UserId)
                .ToListAsync();
        }


        // =====================================================
        //  🔹 HYBRID: GET USER CART (SESSION OR DB)
        // =====================================================
        //public async Task<List<CartViewModel>> GetUserCartAsync()
        //{
        //    if (IsUserLoggedIn())
        //    {
        //        var dbItems = await GetDbCart();

        //        return dbItems.Select(x => new CartViewModel
        //        {
        //            ProductId = x.ProductId,
        //            Name = x.Product.Name,
        //            Price = x.Product.Price,
        //            Quantity = x.Quantity,
        //            ImageUrl = x.Product.ImageUrl
        //        }).ToList();
        //    }
        //    else
        //    {
        //        return GetSessionCart();
        //    }
        //}


        // =====================================================
        //  🔹 ADD TO CART (Session / DB Smart Switch)
        // =====================================================
        public async Task AddToCartAsync(int productId, int qty = 1)
        {
            if (IsUserLoggedIn())
            {
                var dbCart = await GetDbCart();
                var item = dbCart.FirstOrDefault(x => x.ProductId == productId);

                if (item == null)
                {
                    _context.CartItems.Add(new CartItem
                    {
                        ProductId = productId,
                        Quantity = qty
                   //     UserId = UserId
                    });
                }
                else
                {
                    item.Quantity += qty;
                }

                await _context.SaveChangesAsync();
            }
            else
            {
                var cart = GetSessionCart();
                var item = cart.FirstOrDefault(x => x.ProductId == productId);

                var product = await _context.Products.FindAsync(productId);

                if (product == null) return;

                if (item == null)
                {
                    cart.Add(new CartViewModel
                    {
                        ProductId = product.ProductId,
                        ProductName = product.ProductName,
                        ProductPrice = product.ProductPrice,
                        ProductQuantity = qty,
                        ProductImageUrl = product.ProductImageUrl
                    });
                }
                else
                {
                    item.ProductQuantity += qty;
                }

                SaveSessionCart(cart);
            }
        }


        // =====================================================
        //  🔹 CLEAR CART (Session or DB)
        // =====================================================
        public async Task ClearUserCartAsync()
        {
            if (!IsUserLoggedIn())
            {
                HttpContext.Session.Remove(SessionCartKey);
                return;
            }

            var items = _context.CartItems.Where(x => x.Cart.UserId == UserId);
            _context.CartItems.RemoveRange(items);
            await _context.SaveChangesAsync();
        }


        // =====================================================
        //  🔹 REMOVE ITEM (Session or DB)
        // =====================================================
        public async Task RemoveItemAsync(int productId)
        {
            if (IsUserLoggedIn())
            {
                var dbItems = await GetDbCart();
                var item = dbItems.FirstOrDefault(x => x.ProductId == productId);

                if (item != null)
                {
                    _context.CartItems.Remove(item);
                    await _context.SaveChangesAsync();
                }
            }
            else
            {
                var cart = GetSessionCart();
                cart.RemoveAll(x => x.ProductId == productId);
                SaveSessionCart(cart);
            }
        }


        // =====================================================
        //  🔹 INCREASE / DECREASE (common)
        // =====================================================
        public async Task IncreaseAsync(int productId)
        {
            await AddToCartAsync(productId, qty: 1);
        }

        public async Task DecreaseAsync(int productId)
        {
            if (IsUserLoggedIn())
            {
                var dbItems = await GetDbCart();
                var item = dbItems.FirstOrDefault(x => x.ProductId == productId);

                if (item != null)
                {
                    item.Quantity -= 1;

                    if (item.Quantity <= 0)
                        _context.CartItems.Remove(item);

                    await _context.SaveChangesAsync();
                }
            }
            else
            {
                var cart = GetSessionCart();
                var item = cart.FirstOrDefault(x => x.ProductId == productId);

                if (item != null)
                {
                    item.ProductQuantity -= 1;
                    if (item.ProductQuantity <= 0)
                        cart.Remove(item);
                }

                SaveSessionCart(cart);
            }
        }
        // =====================================================
        public async Task<List<CartViewModel>> GetUserCartAsync()
        {
            if (IsUserLoggedIn())
            {
                var items = await _context.CartItems
                    .Include(i => i.Product)
                    .Where(i => i.Cart.UserId == UserId)
                    .ToListAsync();

                return items.Select(i => new CartViewModel
                {
                    ProductId = i.ProductId,
                    ProductName = i.Product.ProductName,
                    ProductImageUrl = i.Product.ProductImageUrl,
                    ProductPrice = i.Product.ProductPrice,
                    ProductQuantity = i.Quantity
                }).ToList();
            }

            // Guest cart from session
            var data = HttpContext.Session.GetString(SessionCartKey);
            if (string.IsNullOrEmpty(data))
                return new List<CartViewModel>();

            return JsonConvert.DeserializeObject<List<CartViewModel>>(data)
                   ?? new List<CartViewModel>();
        }

        //public async Task<List<CartViewModel>> GetUserCartAsync()
        //{
        //    // 🔹 If user logged in → return DB cart
        //    if (IsUserLoggedIn())
        //    {
        //        var dbItems = await _context.CartItems
        //            .Include(c => c.Product)
        //            .Where(c => c.UserId == int.Parse(UserId))
        //            .ToListAsync();

        //        return dbItems.Select(i => new CartViewModel
        //        {
        //            ProductId = i.ProductId,
        //            Name = i.Product!.Name,
        //            Price = i.Product.Price,
        //            Quantity = i.Quantity,
        //            ImageUrl = i.Product.ImageUrl
        //        }).ToList();
        //    }

        //    // 🔹 Guest user → return Session cart
        //    var sessionData = HttpContext.Session.GetString(SessionCartKey);

        //    if (string.IsNullOrEmpty(sessionData))
        //        return new List<CartViewModel>();

        //    return JsonConvert.DeserializeObject<List<CartViewModel>>(sessionData)
        //           ?? new List<CartViewModel>();
        //}
        //public async Task ClearUserCartAsync()
        //{
        //    // Logged-in user → Clear DB cart
        //    if (IsUserLoggedIn())
        //    {
        //        var items = _context.CartItems.Where(c => c.UserId == int.Parse(UserId));

        //        if (items.Any())
        //        {
        //            _context.CartItems.RemoveRange(items);
        //            await _context.SaveChangesAsync();
        //        }

        //        return;
        //    }

        //    // Guest → Clear session cart
        //    HttpContext.Session.Remove(SessionCartKey);
        //}

    }
}
