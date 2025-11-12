using ClothingStore.Core.Entities;
using ClothingStore.Infrastructure.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ClothingStoreApp.Services
{
    public class CartService
    {
        private readonly AppDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CartService(AppDbContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        private string? GetUserId()
        {
            return _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
        }

        public async Task<Cart> GetOrCreateCartAsync()
        {
            var userId = GetUserId();
            if (string.IsNullOrEmpty(userId))
                throw new InvalidOperationException("User not logged in.");

            var cart = await _context.Carts
                .Include(c => c.Items)
                .ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null)
            {
                cart = new Cart { UserId = userId };
                _context.Carts.Add(cart);
                await _context.SaveChangesAsync();
            }

            return cart;
        }

        public async Task AddToCartAsync(int productId, int quantity = 1)
        {
            var cart = await GetOrCreateCartAsync();
            var item = cart.Items.FirstOrDefault(i => i.ProductId == productId);

            if (item == null)
            {
                cart.Items.Add(new CartItem { ProductId = productId, Quantity = quantity });
            }
            else
            {
                item.Quantity += quantity;
            }

            await _context.SaveChangesAsync();
        }

        public async Task RemoveFromCartAsync(int productId)
        {
            var cart = await GetOrCreateCartAsync();
            var item = cart.Items.FirstOrDefault(i => i.ProductId == productId);

            if (item != null)
            {
                cart.Items.Remove(item);
                await _context.SaveChangesAsync();
            }
        }

        public async Task ClearCartAsync()
        {
            var cart = await GetOrCreateCartAsync();
            _context.CartItems.RemoveRange(cart.Items);
            await _context.SaveChangesAsync();
        }

        public async Task<List<CartItem>> GetCartItemsAsync()
        {
            var cart = await GetOrCreateCartAsync();
            return cart.Items.ToList();
        }
    }
}
