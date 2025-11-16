using ClothingStore.Core.Entities;

namespace ClothingStoreApp.Models
{
    public class CartPageViewModel
    {
        public List<CartViewModel>? SessionCart { get; set; }
        public List<CartItem>? DbCart { get; set; }

        public decimal Total =>
            (SessionCart?.Sum(x => x.ProductPrice * x.ProductQuantity) ?? 0) +
            (DbCart?.Sum(x => x.Product.ProductPrice * x.Quantity) ?? 0);

        public bool IsEmpty =>
            (SessionCart == null || !SessionCart.Any()) &&
            (DbCart == null || !DbCart.Any());
    }
}
