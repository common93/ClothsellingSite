using ClothingStore.Core.Entities;

namespace ClothingStoreApp.Models
{
    public class UserAddress
    {
        public int Id { get; set; }

        public string UserId { get; set; }   // FK to AspNetUsers table
        public ApplicationUser User { get; set; }

        public string AddressLine1 { get; set; }
        public string? AddressLine2 { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string PostalCode { get; set; }
        public string Country { get; set; }

        public bool IsDefault { get; set; } = false;
    }

}
