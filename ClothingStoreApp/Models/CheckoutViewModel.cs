namespace ClothingStoreApp.Models
{
    public class CheckoutViewModel
    {
        public string CustomerName { get; set; } = "";
        public string Email { get; set; } = "";
        public string Address { get; set; } = "";

        public string PaymentMethod { get; set; } = "";
    }
}
