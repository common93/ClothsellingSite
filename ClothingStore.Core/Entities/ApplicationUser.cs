using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;

namespace ClothingStore.Core.Entities
{
    public class ApplicationUser : IdentityUser
    {
        public string? FullName { get; set; }
        public string? ProfilePictureUrl { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public bool IsDeleted { get; set; } = false;

        public ICollection<UserAddress> Addresses { get; set; }
    }
}
