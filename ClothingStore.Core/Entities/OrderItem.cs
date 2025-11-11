using System.ComponentModel.DataAnnotations;

namespace ClothingStore.Core.Entities
{
    public class OrderItem
    {
        public int Id { get; set; }

        public int OrderId { get; set; }
        public Order Order { get; set; }   // ✅ Required navigation

        public int ProductId { get; set; }
        public string ProductName { get; set; } = "";
        public string ImageUrl { get; set; } = "";
        public decimal Price { get; set; }
        public int Quantity { get; set; }
    }
}
