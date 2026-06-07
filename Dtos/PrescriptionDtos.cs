namespace pharmEasyClone_backend.Dtos;

public class UploadPrescriptionDto
{
    public string PatientName { get; set; } = string.Empty;
    public string DeliveryAddress { get; set; } = string.Empty;
    public string Pincode { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public string? Notes { get; set; }
}

public class UpdatePrescriptionStatusDto
{
    public string Status { get; set; } = string.Empty;
}
