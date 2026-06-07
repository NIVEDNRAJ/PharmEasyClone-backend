using System.ComponentModel.DataAnnotations;

namespace pharmEasyClone_backend.Dtos;

public class VendorAddProductDto
{
    [Required]
    public string Name { get; set; } = string.Empty;

    [Required]
    public string Description { get; set; } = string.Empty;

    [Required]
    public string Category { get; set; } = string.Empty;

    public string? ImageUrl { get; set; }

    public bool RequiresPrescription { get; set; } = false;

    [Required]
    [Range(0.01, 10000.00)]
    public decimal Price { get; set; }

    [Required]
    [Range(1, 10000)]
    public int StockCount { get; set; }

    [Range(0, 99)]
    public decimal DiscountPercentage { get; set; } = 0.00m;
}
