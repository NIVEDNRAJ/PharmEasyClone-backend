using System.ComponentModel.DataAnnotations;

namespace pharmEasyClone_backend.Models
{
    public class Vendor
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public string BusinessName { get; set; } = string.Empty;

        [Required]
        public string LicenseNumber { get; set; } = string.Empty;

        public bool IsApproved { get; set; } = false;

        public Guid? UserId { get; set; }
        public User? User { get; set; }

        // Fully declared collection navigation property
        public ICollection<VendorInventory> Inventories { get; set; } = new List<VendorInventory>();
    }
}