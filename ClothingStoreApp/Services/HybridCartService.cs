// ClothingStoreApp.Services/HybridCartService.cs
using ClothingStore.Core.Entities;
using ClothingStore.Core.Interfaces;
using ClothingStore.Infrastructure.Data;
using ClothingStoreApp.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using SkiaSharp;
using System.Security.Claims;
using static System.Net.WebRequestMethods;

namespace ClothingStoreApp.Services
{
    public class HybridCartService : ICartService
    {
        private const string SessionCartKey = "cart_session";
        private readonly AppDbContext _db;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IProductRepository _productRepo;

        public HybridCartService(AppDbContext db, IHttpContextAccessor httpContextAccessor, IProductRepository productRepo)
        {
            _db = db;
            _httpContextAccessor = httpContextAccessor;
            _productRepo = productRepo;
        }

        private HttpContext HttpContext => _httpContextAccessor.HttpContext!;
        private string? GetUserId() => HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);

        public string? UserId =>
        HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        public bool IsUserLoggedIn() =>
            !string.IsNullOrEmpty(UserId);
        // -------- Session helpers --------
        public async Task<List<CartViewModel>> GetSessionCartAsync()
        {
            var data = HttpContext.Session.GetString(SessionCartKey);
            if (string.IsNullOrEmpty(data)) return new List<CartViewModel>();
            return JsonConvert.DeserializeObject<List<CartViewModel>>(data) ?? new List<CartViewModel>();
        }

        public async Task SaveSessionCartAsync(List<CartViewModel> cart)
        {
            HttpContext.Session.SetString(SessionCartKey, JsonConvert.SerializeObject(cart));
            await Task.CompletedTask;
        }

        // -------- DB helpers --------
        private async Task<Cart> GetOrCreateDbCartAsync(string userId)
        {
            var cart = await _db.Carts.Include(c => c.Items).
                   ThenInclude(i => i.Product).FirstOrDefaultAsync(c => c.UserId == userId);
                        //.Include(c => c.Items)
                        //.ThenInclude(i => i.Product)
                        //.FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null)
            {
                cart = new Cart { UserId = userId, CreatedAt = DateTime.UtcNow };
                _db.Carts.Add(cart);
                await _db.SaveChangesAsync();
            }

            return cart;
        }

        // -------- Public actions --------

        // Adds either to DB (if logged in) or session (if guest)
        public async Task AddToCartAsync(int productId, int quantity = 1)
        {
           // var userId = GetUserId();
            var userId = _httpContextAccessor.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrEmpty(userId))
            {
                var cart = await GetOrCreateDbCartAsync(userId);
                var existing = cart.Items.FirstOrDefault(i => i.ProductId == productId);
                if (existing == null)
                {
                    var product = await _productRepo.GetByIdAsync(productId);
                    // Avoid null (should validate caller)
                    cart.Items.Add(new CartItem
                    {
                        ProductId = productId,
                        Quantity = quantity
                        //UserId = userId    // 🔥 REQUIRED
                    });

                }
                else
                {
                    existing.Quantity += quantity;
                }
                await _db.SaveChangesAsync();
            }
            else
            {
                var cart = await GetSessionCartAsync();
                var itm = cart.FirstOrDefault(c => c.ProductId == productId);
                if (itm == null)
                {
                    var product = await _productRepo.GetByIdAsync(productId);
                    cart.Add(new CartViewModel
                    {
                        ProductId = productId,
                        ProductName = product?.ProductName,
                        ProductImageUrl = product?.ProductImageUrl,
                        ProductPrice = product?.ProductPrice ?? 0,
                        ProductQuantity = quantity
                    });
                }
                else
                {
                    itm.ProductQuantity += quantity;
                }
                await SaveSessionCartAsync(cart);
            }
        }

        public async Task RemoveFromCartAsync(int productId)
        {
            var userId = GetUserId();
            if (!string.IsNullOrEmpty(userId))
            {
                var cart = await GetOrCreateDbCartAsync(userId);
                var item = cart.Items.FirstOrDefault(i => i.ProductId == productId);
                if (item != null)
                {
                    _db.CartItems.Remove(item);
                    await _db.SaveChangesAsync();
                }
            }
            else
            {
                var cart = await GetSessionCartAsync();
                cart.RemoveAll(c => c.ProductId == productId);
                await SaveSessionCartAsync(cart);
            }
        }

        public async Task IncreaseAsync(int productId)
        {
            var userId = GetUserId();
            if (!string.IsNullOrEmpty(userId))
            {
                var cart = await GetOrCreateDbCartAsync(userId);
                var item = cart.Items.FirstOrDefault(i => i.ProductId == productId);
                if (item != null) { item.Quantity++; await _db.SaveChangesAsync(); }
            }
            else
            {
                var cart = await GetSessionCartAsync();
                var itm = cart.FirstOrDefault(i => i.ProductId == productId);
                if (itm != null) { itm.ProductQuantity++; await SaveSessionCartAsync(cart); }
            }
        }

        public async Task DecreaseAsync(int productId)
        {
            var userId = GetUserId();
            if (!string.IsNullOrEmpty(userId))
            {
                var cart = await GetOrCreateDbCartAsync(userId);
                var item = cart.Items.FirstOrDefault(i => i.ProductId == productId);
                if (item != null)
                {
                    item.Quantity--;
                    if (item.Quantity <= 0) _db.CartItems.Remove(item);
                    await _db.SaveChangesAsync();
                }
            }
            else
            {
                var cart = await GetSessionCartAsync();
                var itm = cart.FirstOrDefault(i => i.ProductId == productId);
                if (itm != null)
                {
                    itm.ProductQuantity--;
                    if (itm.ProductQuantity <= 0) cart.Remove(itm);
                    await SaveSessionCartAsync(cart);
                }
            }
        }

        public async Task ClearCartAsync()
        {
            var userId = GetUserId();
            if (!string.IsNullOrEmpty(userId))
            {
                var cart = await GetOrCreateDbCartAsync(userId);
                _db.CartItems.RemoveRange(cart.Items);
                await _db.SaveChangesAsync();
            }
            else
            {
                await SaveSessionCartAsync(new List<CartViewModel>());
            }
        }

        // Return DB cart items (for logged-in users)
        public async Task<List<CartItem>> GetDbCartItemsAsync()
        {
            //var userId = GetUserId();
            var userId = _httpContextAccessor.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return new List<CartItem>();
            var cart = await GetOrCreateDbCartAsync(userId);
            return cart.Items.ToList();
        }  

        // Merge session cart -> DB cart (call this after login)
        public async Task MergeSessionCartToDbAsync()
        {
            var userId = GetUserId();
            if (string.IsNullOrEmpty(userId)) return;

            var sessionCart = await GetSessionCartAsync();
            if (sessionCart == null || !sessionCart.Any()) return;

            var dbCart = await GetOrCreateDbCartAsync(userId);

            foreach (var s in sessionCart)
            {
                var existing = dbCart.Items.FirstOrDefault(i => i.ProductId == s.ProductId);
                if (existing == null)
                {
                    dbCart.Items.Add(new CartItem
                    {
                        ProductId = s.ProductId,
                        Quantity = s.ProductQuantity
                    });
                }
                else
                {
                    existing.Quantity += s.ProductQuantity;
                }
            }

            await _db.SaveChangesAsync();

            // Clear session cart after merge
            await SaveSessionCartAsync(new List<CartViewModel>());
        }

        public async Task<List<CartViewModel>> GetUserCartAsync()
        {
            var userId = _httpContextAccessor.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!string.IsNullOrEmpty(userId))
            {
                var dbItems = await GetDbCartItemsAsync();

                return dbItems.Select(x => new CartViewModel
                {
                    ProductId = x.ProductId,
                    ProductName = x.Product.ProductName,
                    ProductPrice = x.Product.ProductPrice,
                    ProductQuantity = x.Quantity,
                    ProductImageUrl = x.Product.ProductImageUrl
                }).ToList();
            }
            else
            {
                return await GetSessionCartAsync();
            }
        }
        public async Task ClearUserCartAsync()
        {
            var userId = _httpContextAccessor.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
            // Logged-in user → Clear DB cart
            if (IsUserLoggedIn())
            {
                var items = _db.CartItems.Where(c => c.Cart.UserId == userId);

                if (items.Any())
                {
                    _db.CartItems.RemoveRange(items);
                    await _db.SaveChangesAsync();
                }

                return;
            }

            // Guest → Clear session cart
            HttpContext.Session.Remove(SessionCartKey);
        }


    }
}
