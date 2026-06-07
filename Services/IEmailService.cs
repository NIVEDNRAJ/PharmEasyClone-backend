namespace pharmEasyClone_backend.Services;

public interface IEmailService
{
    Task<bool> SendOtpEmailAsync(string targetEmail, string otpCode, int expiryMinutes);
    Task<bool> SendBookingConfirmationEmailAsync(string targetEmail, string patientName, string doctorName, string specialty, string date, string timeSlot, string mode, decimal paidAmount);
    Task<bool> SendMedicineOrderConfirmationEmailAsync(string targetEmail, string patientName, string orderReceiptId, string deliveryAddress, string pincode, decimal paidAmount, string formattedItems);
    Task<bool> SendLabTestBookingConfirmationEmailAsync(string targetEmail, string patientName, string testName, string date, string timeSlot, string address, string pincode, decimal paidAmount);
}