using Newtonsoft.Json.Linq;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;

namespace ClothingStoreApp.Services
{
    public class RazorpayService
    {
        private readonly HttpClient _http;
        private readonly string _key;
        private readonly string _secret;

        public RazorpayService(IConfiguration config)
        {
            _key = config["Razorpay:Key"];
            _secret = config["Razorpay:Secret"];

            _http = new HttpClient();
            var creds = Encoding.ASCII.GetBytes($"{_key}:{_secret}");
            _http.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Basic", Convert.ToBase64String(creds));
        }

        // Create order on Razorpay
        public async Task<string> CreateOrder(decimal amount, string receiptId)
        {
            var data = new
            {
                amount = (int)(amount * 100), // convert to paise
                currency = "INR",
                receipt = receiptId,
                payment_capture = 1
            };

            var response = await _http.PostAsync(
                "https://api.razorpay.com/v1/orders",
                new StringContent(Newtonsoft.Json.JsonConvert.SerializeObject(data),
                Encoding.UTF8, "application/json")
            );

            string result = await response.Content.ReadAsStringAsync();
            var json = JObject.Parse(result);
            return json["id"].ToString(); // Razorpay order id
        }

        // Verify payment signature (VERY IMPORTANT)
        public bool VerifySignature(string orderId, string paymentId, string signature)
        {
            var payload = $"{orderId}|{paymentId}";
            var secretBytes = Encoding.UTF8.GetBytes(_secret);
            var payloadBytes = Encoding.UTF8.GetBytes(payload);

            using var hmac = new HMACSHA256(secretBytes);
            var hash = hmac.ComputeHash(payloadBytes);

            var generatedSignature = BitConverter.ToString(hash).Replace("-", "").ToLower();
            return generatedSignature == signature.ToLower();
        }

        public string GetKey() => _key;
    }
}
