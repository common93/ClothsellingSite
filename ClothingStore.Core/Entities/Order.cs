using System;
using System.Collections.Generic;

namespace ClothingStore.Core.Entities
{
    public class Order
    {
        public int Id { get; set; }
        public string CustomerName { get; set; } = "";
        public string Email { get; set; } = "";
        public string Address { get; set; } = "";

        public string PaymentMethod { get; set; } = "";
        public string OrderStatus { get; set; } = "";
        public DateTime OrderDate { get; set; } = DateTime.Now;
        public decimal TotalAmount { get; set; }

        public List<OrderItem> Items { get; set; } = new();
    }
}
