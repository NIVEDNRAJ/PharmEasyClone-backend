namespace pharmEasyClone_backend.Services;

public interface IEmailService
{
    Task<bool> SendOtpEmailAsync(string targetEmail, string otpCode, int expiryMinutes);
    Task<bool> SendBookingConfirmationEmailAsync(string targetEmail, string patientName, string doctorName, string specialty, string date, string timeSlot, string mode, decimal paidAmount);
}