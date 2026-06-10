using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace pharmEasyClone_backend.Models;

public class ConsultationBooking
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid UserId { get; set; }

    [ForeignKey("UserId")]
    public User? User { get; set; }

    [Required]
    public Guid DoctorId { get; set; }

    [ForeignKey("DoctorId")]
    public Doctor? Doctor { get; set; }

    [Required]
    public string PatientName { get; set; } = string.Empty;

    [Required]
    public string Gender { get; set; } = string.Empty; // Male, Female, Other

    public string? Symptoms { get; set; }

    [Required]
    public string Mode { get; set; } = "Video"; // Audio, Video

    [Required]
    public DateTime BookingDate { get; set; }

    [Required]
    public string TimeSlot { get; set; } = string.Empty; // e.g. "04:45 PM"

    [Column(TypeName = "decimal(18,2)")]
    public decimal ConsultationFee { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal DiscountAmount { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal PaidAmount { get; set; }

    [Required]
    public string PaymentStatus { get; set; } = "Pending"; // Pending, Paid, Failed

    [Required]
    public string BookingStatus { get; set; } = "Scheduled"; // Scheduled, Completed, Cancelled

    public string? RazorpayOrderId { get; set; }
    
    public string? RazorpayPaymentId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
