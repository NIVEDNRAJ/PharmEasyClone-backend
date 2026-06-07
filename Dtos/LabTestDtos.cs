namespace pharmEasyClone_backend.Dtos;

public class BookLabTestDto
{
    public Guid LabTestId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Pincode { get; set; } = string.Empty;
    public DateTime BookingDate { get; set; }
    public string TimeSlot { get; set; } = "Morning (8 AM - 12 PM)";
}
