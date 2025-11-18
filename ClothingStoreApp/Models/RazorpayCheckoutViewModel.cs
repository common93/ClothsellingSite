namespace ClothingStoreApp.Models
{
    public class RazorpayCheckoutViewModel
    {
        public string OrderId { get; set; }
        public string RazorpayOrderId { get; set; }
        public decimal Amount { get; set; }
        public string Key { get; set; }
        public string CustomerName { get; set; }
        public string Email { get; set; }
        public string Address { get; set; } = "";
        public string PaymentMethod { get; set; } = "";
    }
}
