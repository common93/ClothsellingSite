namespace ClothingStoreApp.Models
{
    public class CartViewModel
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = "";
        public string ProductImageUrl { get; set; } = "";
        public decimal ProductPrice { get; set; }
        public int ProductQuantity { get; set; }
    }
}
