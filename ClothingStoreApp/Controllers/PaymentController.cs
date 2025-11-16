using ClothingStore.Core.Entities;
using ClothingStore.Infrastructure.Data;
using ClothingStoreApp.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ClothingStoreApp.Controllers
{
    [ApiController]
    [Route("payments")]
    public class PaymentsController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly ILogger<PaymentsController> _logger;
        private readonly string _webhookSecret;
        private readonly CartService _cartService; // adjust type if you used an interface

        public PaymentsController(
            AppDbContext db,
            IConfiguration config,
            ILogger<PaymentsController> logger,
            CartService cartService) // inject your cart service
        {
            _db = db;
            _logger = logger;
            _webhookSecret = config["Razorpay:WebhookSecret"] ?? "";
            _cartService = cartService;
        }
        //public IActionResult Start(int orderId)
        //{
        //    return View(orderId);
        //}


        /// <summary>
        /// Razorpay will POST JSON here. Signature is in header X-Razorpay-Signature.
        /// </summary>
        [HttpPost("webhook")]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> Webhook()
        {
            // 1) read body
            string body;
            using (var sr = new StreamReader(Request.Body, Encoding.UTF8))
            {
                body = await sr.ReadToEndAsync();
            }

            var signature = Request.Headers["X-Razorpay-Signature"].FirstOrDefault();

            // 2) persist raw webhook for audit (initial record)
            var log = new WebhookLog
            {
                Event = TryGetEventFromBody(body) ?? "unknown",
                Payload = body,
                SignatureHeader = signature ?? string.Empty,
                ReceivedAt = DateTime.UtcNow,
                Processed = false
            };

            _db.WebhookLogs.Add(log);
            await _db.SaveChangesAsync();

            // 3) verify signature
            if (!VerifyRazorpaySignature(body, signature, _webhookSecret))
            {
                _logger.LogWarning("Webhook signature mismatch. Sig: {sig}", signature);
                log.ProcessingResult = "InvalidSignature";
                await MarkLogProcessedAsync(log);
                return Unauthorized();
            }

            // 4) process event
            try
            {
                var json = JObject.Parse(body);
                var eventName = (string)json["event"] ?? "";

                _logger.LogInformation("Received Razorpay webhook: {evt}", eventName);

                switch (eventName)
                {
                    case "payment.captured":
                        await HandlePaymentCapturedAsync(json, log);
                        break;

                    case "payment.failed":
                        await HandlePaymentFailedAsync(json, log);
                        break;

                    case "order.paid":
                        await HandleOrderPaidAsync(json, log);
                        break;

                    default:
                        if (eventName?.StartsWith("refund.") == true)
                            await HandleRefundAsync(json, log);
                        else
                        {
                            _logger.LogInformation("Unhandled Razorpay event: {evt}", eventName);
                            log.ProcessingResult = $"Ignored:{eventName}";
                            log.Processed = true;
                            await MarkLogProcessedAsync(log);
                        }
                        break;
                }

                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception processing webhook");
                log.ProcessingResult = "Exception:" + ex.Message;
                await MarkLogProcessedAsync(log);
                return StatusCode(500);
            }
        }

        // -------------------------
        // Handlers
        // -------------------------
        private async Task HandlePaymentCapturedAsync(JObject json, WebhookLog log)
        {
            var payment = json["payload"]?["payment"]?["entity"];
            if (payment == null)
            {
                log.ProcessingResult = "MissingPaymentEntity";
                await MarkLogProcessedAsync(log);
                return;
            }

            string razorpayPaymentId = (string)payment["id"] ?? "";
            string razorpayOrderId = (string)payment["order_id"] ?? "";

            // idempotency: skip if already processed
            if (await IsPaymentProcessedAsync(razorpayPaymentId))
            {
                log.ProcessingResult = $"AlreadyProcessed:{razorpayPaymentId}";
                log.Processed = true;
                await MarkLogProcessedAsync(log);
                return;
            }

            var order = await _db.Orders.FirstOrDefaultAsync(o => o.RazorpayOrderId == razorpayOrderId);
            if (order == null)
            {
                log.ProcessingResult = "OrderNotFound";
                await MarkLogProcessedAsync(log);
                return;
            }

            using var tx = await _db.Database.BeginTransactionAsync();
            try
            {
                order.PaymentId = razorpayPaymentId;
                order.PaymentStatus = PaymentStatus.Pending;
                order.OrderStatus = OrderStatus.Approved;
                await _db.SaveChangesAsync();

                // optional: clear DB cart (if you maintain DB cart keyed by CustomerId)
                try
                {
                    if (!string.IsNullOrEmpty(order.CustomerId))
                    {
                        await _cartService.ClearUserCartAsync();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "ClearUserCartAsync failed (non-fatal).");
                }

                await tx.CommitAsync();

                log.RazorpayPaymentId = razorpayPaymentId;
                log.RazorpayOrderId = razorpayOrderId;
                log.Processed = true;
                log.ProcessingResult = "PaymentCaptured";
                await MarkLogProcessedAsync(log);
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                _logger.LogError(ex, "Failed to persist payment.captured changes");
                log.ProcessingResult = "ExceptionDuringUpdate:" + ex.Message;
                await MarkLogProcessedAsync(log);
                throw;
            }
        }

        private async Task HandlePaymentFailedAsync(JObject json, WebhookLog log)
        {
            var payment = json["payload"]?["payment"]?["entity"];
            if (payment == null)
            {
                log.ProcessingResult = "MissingPaymentEntity";
                await MarkLogProcessedAsync(log);
                return;
            }

            string razorpayPaymentId = (string)payment["id"] ?? "";
            string razorpayOrderId = (string)payment["order_id"] ?? "";

            var order = await _db.Orders.FirstOrDefaultAsync(o => o.RazorpayOrderId == razorpayOrderId);
            if (order != null)
            {
                order.PaymentStatus = PaymentStatus.failed;
                order.OrderStatus = OrderStatus.Cancelled;
                await _db.SaveChangesAsync();
            }

            log.RazorpayPaymentId = razorpayPaymentId;
            log.RazorpayOrderId = razorpayOrderId;
            log.Processed = true;
            log.ProcessingResult = "PaymentFailed";
            await MarkLogProcessedAsync(log);
        }

        private async Task HandleOrderPaidAsync(JObject json, WebhookLog log)
        {
            var orderEntity = json["payload"]?["order"]?["entity"];
            if (orderEntity == null)
            {
                log.ProcessingResult = "MissingOrderEntity";
                await MarkLogProcessedAsync(log);
                return;
            }

            string razorpayOrderId = (string)orderEntity["id"] ?? "";

            var order = await _db.Orders.FirstOrDefaultAsync(o => o.RazorpayOrderId == razorpayOrderId);
            if (order == null)
            {
                log.ProcessingResult = "OrderNotFound";
                await MarkLogProcessedAsync(log);
                return;
            }

            if (order.PaymentStatus == PaymentStatus.captured || order.OrderStatus == OrderStatus.Approved)
            {
                log.ProcessingResult = "OrderAlreadyPaid";
                log.Processed = true;
                await MarkLogProcessedAsync(log);
                return;
            }

            order.PaymentStatus = PaymentStatus.captured;
            order.OrderStatus = OrderStatus.Approved;
            await _db.SaveChangesAsync();

            log.RazorpayOrderId = razorpayOrderId;
            log.Processed = true;
            log.ProcessingResult = "OrderMarkedPaid";
            await MarkLogProcessedAsync(log);
        }

        private async Task HandleRefundAsync(JObject json, WebhookLog log)
        {
            var refund = json["payload"]?["refund"]?["entity"];
            if (refund == null)
            {
                log.ProcessingResult = "MissingRefundEntity";
                await MarkLogProcessedAsync(log);
                return;
            }

            string refundId = (string)refund["id"] ?? "";
            string paymentId = (string)refund["payment_id"] ?? "";

            // You may want to map refund -> order and update refund table
            log.ProcessingResult = $"RefundReceived:{refundId}";
            log.Processed = true;
            await MarkLogProcessedAsync(log);
        }

        // -------------------------
        // Utility helper methods
        // -------------------------
        private async Task MarkLogProcessedAsync(WebhookLog log)
        {
            var existing = await _db.WebhookLogs.FindAsync(log.Id);
            if (existing != null)
            {
                existing.Processed = log.Processed;
                existing.ProcessingResult = log.ProcessingResult;
                existing.RazorpayOrderId = log.RazorpayOrderId;
                existing.RazorpayPaymentId = log.RazorpayPaymentId;
                await _db.SaveChangesAsync();
            }
        }

        private async Task<bool> IsPaymentProcessedAsync(string razorpayPaymentId)
        {
            if (string.IsNullOrEmpty(razorpayPaymentId)) return false;

            var already = await _db.Orders.AnyAsync(o => o.PaymentId == razorpayPaymentId);
            if (already) return true;

            var log = await _db.WebhookLogs.FirstOrDefaultAsync(l => l.RazorpayPaymentId == razorpayPaymentId && l.Processed);
            return log != null;
        }

        private static string? TryGetEventFromBody(string body)
        {
            try
            {
                var j = JObject.Parse(body);
                return (string?)j["event"];
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Verify Razorpay webhook body signature using shared webhook secret (HMAC SHA256).
        /// Razorpay sends signature in header X-Razorpay-Signature.
        /// </summary>
        private bool VerifyRazorpaySignature(string body, string? signatureHeader, string webhookSecret)
        {
            if (string.IsNullOrEmpty(signatureHeader) || string.IsNullOrEmpty(webhookSecret))
                return false;

            var keyBytes = Encoding.UTF8.GetBytes(webhookSecret);
            var bodyBytes = Encoding.UTF8.GetBytes(body);

            using var hmac = new HMACSHA256(keyBytes);
            var hash = hmac.ComputeHash(bodyBytes);

            // Compare hex
            var hex = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
            if (hex == signatureHeader.ToLowerInvariant()) return true;

            // Compare base64 (some providers use base64)
            var b64 = Convert.ToBase64String(hash);
            if (b64 == signatureHeader) return true;

            return false;
        }
    }
}
