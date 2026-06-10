using System.ComponentModel.DataAnnotations;

namespace pharmEasyClone_backend.Models;

public class Prescription
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid UserId { get; set; }
    public User? User { get; set; }

    [Required]
    public string PatientName { get; set; } = string.Empty;

    [Required]
    public string DeliveryAddress { get; set; } = string.Empty;

    [Required]
    public string Pincode { get; set; } = string.Empty;

    public string? ImageUrl { get; set; }
    public string? Notes { get; set; }

    public string Status { get; set; } = "Pending"; // Pending, Verified, Rejected, Fulfilled
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
