using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace pharmEasyClone_backend.Models;

public class Order
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid UserId { get; set; }

    [ForeignKey("UserId")]
    public User? User { get; set; }

    [Required]
    public string PatientName { get; set; } = string.Empty;

    [Required]
    public string DeliveryAddress { get; set; } = string.Empty;

    [Required]
    public string Pincode { get; set; } = string.Empty;

    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalMrp { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal DiscountAmount { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal PaidAmount { get; set; }

    [Required]
    public string PaymentStatus { get; set; } = "Pending"; // Pending, Paid, Failed

    [Required]
    public string OrderStatus { get; set; } = "Processing"; // Processing, Shipped, Delivered, Cancelled

    public string? RazorpayOrderId { get; set; }

    public string? RazorpayPaymentId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}
