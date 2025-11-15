using System;
using System.Collections.Generic;

namespace ClothingStore.Core.Entities
{
    public class Order
    {
        public int Id { get; set; }

        public int CustomerId { get; set; } = 0;    
        public string CustomerName { get; set; } = "";
        public string Email { get; set; } = "";
        public string Address { get; set; } = "";

        public string PaymentMethod { get; set; } = "";

       // public string OrderStatus { get; set; } = "";
        public DateTime OrderDate { get; set; } = DateTime.Now;

        public DateTime? ApprovedAt { get; set; }
        public DateTime? ShippedAt { get; set; }
        public DateTime? DeliveredAt { get; set; }
        public DateTime? CancelledAt { get; set; }


        public decimal TotalAmount { get; set; }

        public OrderStatus OrderStatus { get; set; } = OrderStatus.Pending;
        public List<OrderItem> Items { get; set; } = new();
    }
}
