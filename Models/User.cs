using System.ComponentModel.DataAnnotations;

namespace pharmEasyClone_backend.Models;

public class User
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    public string? FullName { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}