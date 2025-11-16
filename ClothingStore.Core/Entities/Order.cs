using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ClothingStore.Core.Entities
{
    public class Order
    {
        [Key]
        public string OrderId { get; set; } = Guid.NewGuid().ToString();
        public string CustomerId { get; set; } = "";    
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
