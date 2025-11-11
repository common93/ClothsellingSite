using ClothingStoreApp.Models;
using Newtonsoft.Json;


namespace ClothingStoreApp.Services
{
    public static class CartService
    {
        private const string CartKey = "cart";

        public static List<CartViewModel> GetCart(HttpContext context)
        {
            var data = context.Session.GetString(CartKey);
            return data == null ? new List<CartViewModel>() : JsonConvert.DeserializeObject<List<CartViewModel>>(data);
        }

        public static void SaveCart(HttpContext context, List<CartViewModel> cart)
        {
            context.Session.SetString(CartKey, JsonConvert.SerializeObject(cart));
        }
    }
}
