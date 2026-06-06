namespace pharmEasyClone_backend.Services;

public interface IEmailService
{
    Task<bool> SendOtpEmailAsync(string targetEmail, string otpCode, int expiryMinutes);
}