using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace pharmEasyClone_backend.Services;

public class BrevoEmailService : IEmailService
{
    private readonly HttpClient _httpClient;
    private readonly BrevoSettings _settings;

    public BrevoEmailService(HttpClient httpClient, IOptions<BrevoSettings> settings)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
    }

    public async Task<bool> SendOtpEmailAsync(string targetEmail, string otpCode, int expiryMinutes)
    {
        var requestUrl = "https://api.brevo.com/v3/smtp/email";

        var payload = new
        {
            sender = new { name = _settings.SenderName, email = _settings.SenderEmail },
            to = new[] { new { email = targetEmail } },
            subject = $"{otpCode} is your Verification Code",
            htmlContent = $@"
                <div style='font-family: Arial, sans-serif; max-width: 500px; margin: auto; padding: 20px; border: 1px solid #e0e0e0; border-radius: 8px;'>
                    <h2 style='color: #10847e; text-align: center;'>PharmEasy Clone</h2>
                    <p>Hello,</p>
                    <p>Use the following One Time Password (OTP) to complete your login. This code is valid for the next <b>{expiryMinutes} minutes</b>.</p>
                    <div style='text-align: center; margin: 30px 0;'>
                        <span style='font-size: 32px; font-weight: bold; letter-spacing: 5px; color: #303642; background: #f4f7f6; padding: 10px 20px; border-radius: 4px; display: inline-block;'>{otpCode}</span>
                    </div>
                    <p style='font-size: 12px; color: #888888; text-align: center;'>If you did not request this code, please ignore this email.</p>
                </div>"
        };

        var request = new HttpRequestMessage(HttpMethod.Post, requestUrl)
        {
            Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
        };

        request.Headers.Add("api-key", _settings.ApiKey);

        try
        {
            var response = await _httpClient.SendAsync(request);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}

public class BrevoSettings
{
    public string ApiKey { get; set; } = string.Empty;
    public string SenderEmail { get; set; } = string.Empty;
    public string SenderName { get; set; } = string.Empty;
}