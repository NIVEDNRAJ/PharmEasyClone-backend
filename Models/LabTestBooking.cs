using System.ComponentModel.DataAnnotations;

namespace pharmEasyClone_backend.Models;

public class LabTestBooking
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid UserId { get; set; }
    public User? User { get; set; }

    public Guid LabTestId { get; set; }
    public LabTest? LabTest { get; set; }

    [Required]
    public string PatientName { get; set; } = string.Empty;

    [Required]
    public string Address { get; set; } = string.Empty;

    [Required]
    public string Pincode { get; set; } = string.Empty;

    public DateTime BookingDate { get; set; }
    public string TimeSlot { get; set; } = "Morning (8 AM - 12 PM)";
    public decimal PaidAmount { get; set; }
    public string PaymentStatus { get; set; } = "Pending";
    public string? RazorpayOrderId { get; set; }
    public string? RazorpayPaymentId { get; set; }
    public string BookingStatus { get; set; } = "Scheduled";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
