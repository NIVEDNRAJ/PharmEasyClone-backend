using System.ComponentModel.DataAnnotations;

namespace pharmEasyClone_backend.Models;

public class OtpVerification
{
    [Key]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(6, MinimumLength = 6)]
    public string Code { get; set; } = string.Empty;

    public DateTime ExpiresAt { get; set; }
}