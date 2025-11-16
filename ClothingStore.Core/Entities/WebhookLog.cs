using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClothingStore.Core.Entities
{
        public class WebhookLog
        {
            public int Id { get; set; }

            /// <summary>
            /// Event name sent by Razorpay, e.g. "payment.captured"
            /// </summary>
            public string Event { get; set; } = null!;

            /// <summary>
            /// Raw JSON payload (store full body for audit/troubleshooting)
            /// </summary>
            public string Payload { get; set; } = null!;

            /// <summary>
            /// Value from header X-Razorpay-Signature
            /// </summary>
            public string SignatureHeader { get; set; } = null!;

            public string? RazorpayPaymentId { get; set; }
            public string? RazorpayOrderId { get; set; }

            public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;

            /// <summary>
            /// Whether processing (business update) succeeded
            /// </summary>
            public bool Processed { get; set; } = false;

            /// <summary>
            /// Short description/result for troubleshooting
            /// </summary>
            public string? ProcessingResult { get; set; }
        }
}
