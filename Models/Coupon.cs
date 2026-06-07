using System.ComponentModel.DataAnnotations;

namespace pharmEasyClone_backend.Models;

public class Coupon
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public string Code { get; set; } = string.Empty;

    public decimal DiscountAmount { get; set; }

    public bool IsActive { get; set; } = true;
}
