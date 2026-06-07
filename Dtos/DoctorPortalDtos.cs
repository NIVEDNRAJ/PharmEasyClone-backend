namespace pharmEasyClone_backend.Dtos;

public class UpdateDoctorProfileDto
{
    public string Name { get; set; } = string.Empty;
    public string Specialty { get; set; } = string.Empty;
    public string Qualifications { get; set; } = string.Empty;
    public int ExperienceYears { get; set; }
    public string Languages { get; set; } = string.Empty;
    public decimal ConsultationFee { get; set; }
    public string? ImageUrl { get; set; }
}

public class UpdateBookingStatusDto
{
    public string Status { get; set; } = string.Empty;
}
