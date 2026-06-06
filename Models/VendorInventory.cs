using System.ComponentModel.DataAnnotations;

namespace pharmEasyClone_backend.Models;

public class VendorInventory
{
    public Guid VendorId { get; set; }
    public Vendor Vendor { get; set; } = null!;

    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;

    [Required]
    public decimal Price { get; set; }

    [Required]
    public int StockCount { get; set; }

    public decimal DiscountPercentage { get; set; } = 0.00m;
}