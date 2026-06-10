using System.ComponentModel.DataAnnotations;

namespace pharmEasyClone_backend.Dtos;

public class CreateBookingDto
{
    [Required]
    public Guid DoctorId { get; set; }

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
    public string TimeSlot { get; set; } = string.Empty;

    public string? CouponCode { get; set; }
}

public class ConfirmPaymentDto
{
    [Required]
    public Guid BookingId { get; set; }

    public string? RazorpayOrderId { get; set; }

    public string? RazorpayPaymentId { get; set; }

    public string? RazorpaySignature { get; set; }
}
