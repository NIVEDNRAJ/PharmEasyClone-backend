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

    public async Task<bool> SendBookingConfirmationEmailAsync(string targetEmail, string patientName, string doctorName, string specialty, string date, string timeSlot, string mode, decimal paidAmount)
    {
        var requestUrl = "https://api.brevo.com/v3/smtp/email";

        var payload = new
        {
            sender = new { name = _settings.SenderName, email = _settings.SenderEmail },
            to = new[] { new { email = targetEmail } },
            subject = $"Confirmed: Your consultation with {doctorName}",
            htmlContent = $@"
                <div style='font-family: Arial, sans-serif; max-width: 550px; margin: auto; padding: 25px; border: 1px solid #e0e0e0; border-radius: 12px; box-shadow: 0 4px 6px rgba(0,0,0,0.05);'>
                    <div style='text-align: center; border-bottom: 2px solid #f4f7f6; padding-bottom: 15px;'>
                        <h2 style='color: #10847e; margin: 0;'>PharmEasy Doctor Consult</h2>
                        <span style='color: #2e7d32; font-weight: bold; background-color: #e8f5e9; padding: 4px 12px; border-radius: 20px; font-size: 12px; display: inline-block; margin-top: 8px;'>APPOINTMENT CONFIRMED</span>
                    </div>
                    <p style='color: #303642; font-size: 16px; margin-top: 20px;'>Dear Patient,</p>
                    <p style='color: #555555; font-size: 14px;'>Your online consultation booking is confirmed. Below are your booking details:</p>
                    
                    <div style='background-color: #f8f9fa; padding: 15px; border-radius: 8px; margin: 20px 0;'>
                        <table style='width: 100%; border-collapse: collapse; font-size: 14px;'>
                            <tr>
                                <td style='padding: 6px 0; color: #777777;'>Doctor:</td>
                                <td style='padding: 6px 0; font-weight: bold; color: #303642;'>{doctorName} ({specialty})</td>
                            </tr>
                            <tr>
                                <td style='padding: 6px 0; color: #777777;'>Patient Name:</td>
                                <td style='padding: 6px 0; font-weight: bold; color: #303642;'>{patientName}</td>
                            </tr>
                            <tr>
                                <td style='padding: 6px 0; color: #777777;'>Date & Time:</td>
                                <td style='padding: 6px 0; font-weight: bold; color: #303642;'>{date} at {timeSlot}</td>
                            </tr>
                            <tr>
                                <td style='padding: 6px 0; color: #777777;'>Consultation Mode:</td>
                                <td style='padding: 6px 0; font-weight: bold; color: #303642;'>{mode} Call</td>
                            </tr>
                            <tr>
                                <td style='padding: 6px 0; color: #777777;'>Paid Amount:</td>
                                <td style='padding: 6px 0; font-weight: bold; color: #10847e;'>₹{paidAmount}</td>
                            </tr>
                        </table>
                    </div>
                    
                    <p style='font-size: 13px; color: #666666; line-height: 1.5;'>
                        <b>Instructions:</b><br/>
                        1. Please log in to your account 5 minutes before your scheduled slot.<br/>
                        2. Go to your Consultation dashboard to join the {mode.ToLower()} call.<br/>
                        3. Make sure you have a stable internet connection.
                    </p>
                    
                    <div style='text-align: center; border-top: 1px solid #eeeeee; padding-top: 15px; margin-top: 25px;'>
                        <p style='font-size: 11px; color: #aaaaaa;'>Thank you for choosing PharmEasy. Live healthy, feel great!</p>
                    </div>
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

    public async Task<bool> SendMedicineOrderConfirmationEmailAsync(string targetEmail, string patientName, string orderReceiptId, string deliveryAddress, string pincode, decimal paidAmount, string formattedItems)
    {
        var requestUrl = "https://api.brevo.com/v3/smtp/email";

        var payload = new
        {
            sender = new { name = _settings.SenderName, email = _settings.SenderEmail },
            to = new[] { new { email = targetEmail } },
            subject = $"Confirmed: Your Medicine Order #{orderReceiptId.Substring(0, 8).ToUpper()}",
            htmlContent = $@"
                <div style='font-family: Arial, sans-serif; max-width: 550px; margin: auto; padding: 25px; border: 1px solid #e0e0e0; border-radius: 12px; box-shadow: 0 4px 6px rgba(0,0,0,0.05);'>
                    <div style='text-align: center; border-bottom: 2px solid #f4f7f6; padding-bottom: 15px;'>
                        <h2 style='color: #10847e; margin: 0;'>PharmEasy Medicine Orders</h2>
                        <span style='color: #2e7d32; font-weight: bold; background-color: #e8f5e9; padding: 4px 12px; border-radius: 20px; font-size: 12px; display: inline-block; margin-top: 8px;'>ORDER PLACED & PAID</span>
                    </div>
                    <p style='color: #303642; font-size: 16px; margin-top: 20px;'>Dear Customer,</p>
                    <p style='color: #555555; font-size: 14px;'>Thank you for your order! Your medicine purchase is confirmed and payment has been processed successfully.</p>
                    
                    <div style='background-color: #f8f9fa; padding: 15px; border-radius: 8px; margin: 20px 0;'>
                        <table style='width: 100%; border-collapse: collapse; font-size: 14px;'>
                            <tr>
                                <td style='padding: 6px 0; color: #777777; width: 120px;'>Order ID:</td>
                                <td style='padding: 6px 0; font-weight: bold; color: #303642;'>#{orderReceiptId.ToUpper()}</td>
                            </tr>
                            <tr>
                                <td style='padding: 6px 0; color: #777777;'>Patient Name:</td>
                                <td style='padding: 6px 0; font-weight: bold; color: #303642;'>{patientName}</td>
                            </tr>
                            <tr>
                                <td style='padding: 6px 0; color: #777777;'>Delivery Address:</td>
                                <td style='padding: 6px 0; font-weight: bold; color: #303642;'>{deliveryAddress}, {pincode}</td>
                            </tr>
                            <tr>
                                <td style='padding: 6px 0; color: #777777;'>Items Ordered:</td>
                                <td style='padding: 6px 0; font-weight: bold; color: #303642; white-space: pre-line;'>{formattedItems}</td>
                            </tr>
                            <tr>
                                <td style='padding: 6px 0; color: #777777;'>Paid Amount:</td>
                                <td style='padding: 6px 0; font-weight: bold; color: #10847e;'>₹{paidAmount}</td>
                            </tr>
                        </table>
                    </div>
                    
                    <p style='font-size: 13px; color: #666666; line-height: 1.5;'>
                        <b>What Happens Next?</b><br/>
                        1. Our partner pharmacist will verify your prescription (if required) and package your medicines.<br/>
                        2. The order will be shipped to your address within 24-48 hours.<br/>
                        3. You can track your order status in your Medicine Orders History page.
                    </p>
                    
                    <div style='text-align: center; border-top: 1px solid #eeeeee; padding-top: 15px; margin-top: 25px;'>
                        <p style='font-size: 11px; color: #aaaaaa;'>Thank you for choosing PharmEasy. Live healthy, feel great!</p>
                    </div>
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

    public async Task<bool> SendLabTestBookingConfirmationEmailAsync(string targetEmail, string patientName, string testName, string date, string timeSlot, string address, string pincode, decimal paidAmount)
    {
        var requestUrl = "https://api.brevo.com/v3/smtp/email";

        var payload = new
        {
            sender = new { name = _settings.SenderName, email = _settings.SenderEmail },
            to = new[] { new { email = targetEmail } },
            subject = $"Confirmed: Your Lab Test Appointment - {testName}",
            htmlContent = $@"
                <div style='font-family: Arial, sans-serif; max-width: 550px; margin: auto; padding: 25px; border: 1px solid #e0e0e0; border-radius: 12px; box-shadow: 0 4px 6px rgba(0,0,0,0.05);'>
                    <div style='text-align: center; border-bottom: 2px solid #f4f7f6; padding-bottom: 15px;'>
                        <h2 style='color: #10847e; margin: 0;'>PharmEasy Diagnostics</h2>
                        <span style='color: #2e7d32; font-weight: bold; background-color: #e8f5e9; padding: 4px 12px; border-radius: 20px; font-size: 12px; display: inline-block; margin-top: 8px;'>APPOINTMENT SCHEDULED & PAID</span>
                    </div>
                    <p style='color: #303642; font-size: 16px; margin-top: 20px;'>Dear Patient,</p>
                    <p style='color: #555555; font-size: 14px;'>Your lab test appointment is confirmed and payment has been processed successfully. A certified lab technician will visit your address to collect samples.</p>
                    
                    <div style='background-color: #f8f9fa; padding: 15px; border-radius: 8px; margin: 20px 0;'>
                        <table style='width: 100%; border-collapse: collapse; font-size: 14px;'>
                            <tr>
                                <td style='padding: 6px 0; color: #777777; width: 120px;'>Test Selected:</td>
                                <td style='padding: 6px 0; font-weight: bold; color: #303642;'>{testName}</td>
                            </tr>
                            <tr>
                                <td style='padding: 6px 0; color: #777777;'>Patient Name:</td>
                                <td style='padding: 6px 0; font-weight: bold; color: #303642;'>{patientName}</td>
                            </tr>
                            <tr>
                                <td style='padding: 6px 0; color: #777777;'>Date & Slot:</td>
                                <td style='padding: 6px 0; font-weight: bold; color: #303642;'>{date} ({timeSlot})</td>
                            </tr>
                            <tr>
                                <td style='padding: 6px 0; color: #777777;'>Collection Address:</td>
                                <td style='padding: 6px 0; font-weight: bold; color: #303642;'>{address}, {pincode}</td>
                            </tr>
                            <tr>
                                <td style='padding: 6px 0; color: #777777;'>Paid Amount:</td>
                                <td style='padding: 6px 0; font-weight: bold; color: #10847e;'>₹{paidAmount}</td>
                            </tr>
                        </table>
                    </div>
                    
                    <p style='font-size: 13px; color: #666666; line-height: 1.5;'>
                        <b>Important Guidelines for Sample Collection:</b><br/>
                        1. Please ensure the patient is available at the collection address during the selected time slot.<br/>
                        2. If the test requires fasting (e.g., Fasting Blood Sugar, Lipid Profile), ensure a minimum of 10-12 hours of overnight fasting before sample collection. Normal water intake is allowed.<br/>
                        3. Digital reports will be shared via email and your dashboard within 24 hours of sample receipt at our lab.
                    </p>
                    
                    <div style='text-align: center; border-top: 1px solid #eeeeee; padding-top: 15px; margin-top: 25px;'>
                        <p style='font-size: 11px; color: #aaaaaa;'>Thank you for choosing PharmEasy. Live healthy, feel great!</p>
                    </div>
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