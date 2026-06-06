using System.ComponentModel.DataAnnotations;

namespace pharmEasyClone_backend.Models;

public class Product
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public string Name { get; set; } = string.Empty;

    [Required]
    public string Description { get; set; } = string.Empty;

    public string? ImageUrl { get; set; }

    [Required]
    public string Category { get; set; } = string.Empty; // Medicine, LabTest, Wellness

    public bool RequiresPrescription { get; set; } = false;

    // Many-to-Many Relationship to Vendors via Inventory
    public ICollection<VendorInventory> Inventories { get; set; } = new List<VendorInventory>();
}