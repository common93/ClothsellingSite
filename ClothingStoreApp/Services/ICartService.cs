// ClothingStoreApp.Services/ICartService.cs
using ClothingStore.Core.Entities;
using ClothingStoreApp.Models;

namespace ClothingStoreApp.Services
{
    public interface ICartService
    {
        Task<List<CartViewModel>> GetSessionCartAsync();
        Task SaveSessionCartAsync(List<CartViewModel> cart);
        Task AddToCartAsync(int productId, int quantity = 1);
        Task RemoveFromCartAsync(int productId);
        Task IncreaseAsync(int productId);
        Task DecreaseAsync(int productId);
        Task ClearCartAsync();
        Task<List<CartItem>> GetDbCartItemsAsync();
        Task MergeSessionCartToDbAsync();
        Task<List<CartViewModel>> GetUserCartAsync();
        Task ClearUserCartAsync();

    }
}
