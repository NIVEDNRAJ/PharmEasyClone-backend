using System.ComponentModel.DataAnnotations;

namespace pharmEasyClone_backend.Models;

public class LabTest
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = "Full Body";
    public decimal Mrp { get; set; }
    public decimal DiscountedPrice { get; set; }
    public int DiscountPercentage { get; set; }
    public bool IsActive { get; set; } = true;
}
