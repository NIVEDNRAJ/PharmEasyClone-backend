using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using pharmEasyClone_backend.Data;
using pharmEasyClone_backend.Dtos;
using pharmEasyClone_backend.Models;
using pharmEasyClone_backend.Services;

namespace pharmEasyClone_backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;

    public AuthController(
        ApplicationDbContext context, 
        IEmailService emailService, 
        IConfiguration configuration)
    {
        _context = context;
        _emailService = emailService;
        _configuration = configuration;
    }

    // STEP 1: Generate and Send OTP
    [HttpPost("send-otp")]
    public async Task<IActionResult> SendOtp([FromBody] SendOtpDto dto)
    {
        // 1. Generate 6-digit numeric OTP
        string otpCode = new Random().Next(100000, 999999).ToString();
        int expiryMinutes = 5;
        var expiresAt = DateTime.UtcNow.AddMinutes(expiryMinutes);

        // 2. Save or update OTP tracking table in MySQL
        var existingOtp = await _context.OtpVerifications.FirstOrDefaultAsync(o => o.Email == dto.Email);
        if (existingOtp != null)
        {
            existingOtp.Code = otpCode;
            existingOtp.ExpiresAt = expiresAt;
        }
        else
        {
            var newOtp = new OtpVerification
            {
                Email = dto.Email,
                Code = otpCode,
                ExpiresAt = expiresAt
            };
            await _context.OtpVerifications.AddAsync(newOtp);
        }
        await _context.SaveChangesAsync();

        // 3. Dispatch via Brevo Email API
        bool emailSent = await _emailService.SendOtpEmailAsync(dto.Email, otpCode, expiryMinutes);
        if (!emailSent)
        {
            return StatusCode(500, new { message = "Failed to dispatch verification email via Brevo." });
        }

        return Ok(new { message = "Verification code sent successfully." });
    }

    // STEP 2: Verify OTP & Issue Token
    [HttpPost("verify-otp")]
    public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpDto dto)
    {
        // 1. Validate the OTP presence and expiry
        var otpRecord = await _context.OtpVerifications
            .FirstOrDefaultAsync(o => o.Email == dto.Email && o.Code == dto.Code);

        if (otpRecord == null || otpRecord.ExpiresAt < DateTime.UtcNow)
        {
            return BadRequest(new { message = "Invalid or expired verification code." });
        }

        // 2. Remove the OTP record so it cannot be reused
        _context.OtpVerifications.Remove(otpRecord);

        // 3. Auto-Register or retrieve the User (Seamless Sign-in/Sign-up)
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
        if (user == null)
        {
            user = new User
            {
                Email = dto.Email,
                FullName = dto.Email.Split('@')[0] // Default placeholder name from email handle
            };
            await _context.Users.AddAsync(user);
        }
        await _context.SaveChangesAsync();

        // 4. Generate access token
        string jwtToken = GenerateJwtToken(user);

        return Ok(new AuthResponseDto
        {
            Token = jwtToken,
            Email = user.Email,
            FullName = user.FullName
        });
    }

    private string GenerateJwtToken(User user)
    {
        var jwtSettings = _configuration.GetSection("JwtSettings");
        var secretKey = jwtSettings["Secret"] ?? "A_Very_Secure_And_Ultra_Long_Secret_Key_For_PharmEasy_Clone_Authentication_2026";
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Name, user.FullName ?? string.Empty)
        };

        var token = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddDays(7), // Token valid for 7 days
            Issuer = jwtSettings["Issuer"] ?? "PharmEasyCloneBackend",
            Audience = jwtSettings["Audience"] ?? "PharmEasyCloneAngular",
            SigningCredentials = creds
        };

        var tokenHandler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
        var createdToken = tokenHandler.CreateToken(token);

        return tokenHandler.WriteToken(createdToken);
    }
}