using System.ComponentModel.DataAnnotations;

namespace pharmEasyClone_backend.Models;

public class Doctor
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public string Name { get; set; } = string.Empty;

    [Required]
    public string Specialty { get; set; } = string.Empty;

    [Required]
    public string Qualifications { get; set; } = string.Empty;

    public int ExperienceYears { get; set; }

    [Required]
    public string Languages { get; set; } = string.Empty;

    public decimal ConsultationFee { get; set; }

    public string? ImageUrl { get; set; }

    public Guid? UserId { get; set; }
    public User? User { get; set; }

    public bool IsApproved { get; set; } = false;
}
