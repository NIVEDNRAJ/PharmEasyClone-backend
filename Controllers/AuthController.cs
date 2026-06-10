using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
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
        dto.Email = dto.Email.Trim().ToLowerInvariant();

        // Check if user already exists
        var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
        if (existingUser != null)
        {
            var requestedRole = dto.Role ?? "Customer";
            if (existingUser.Role == requestedRole || existingUser.Role != "Customer")
            {
                return BadRequest(new { message = "Email already registered. Please log in directly." });
            }
        }

        // 1. Generate 6-digit numeric OTP
        string otpCode = RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
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

        // 3. Dispatch via Brevo Email API. In local development, email delivery can fail
        // because the API key/sender is not configured, so expose the OTP for testing.
        bool isDevelopment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development";
        if (isDevelopment)
        {
            Console.WriteLine($"[Development] Generated OTP for {dto.Email} is: {otpCode}");
        }

        bool emailSent = await _emailService.SendOtpEmailAsync(dto.Email, otpCode, expiryMinutes);
        if (!emailSent)
        {
            if (isDevelopment)
            {
                return Ok(new
                {
                    message = "Verification code generated. Email delivery failed. Please check the backend console for the code.",
                    emailSent = false
                });
            }

            return StatusCode(500, new { message = "Failed to dispatch verification email via Brevo." });
        }

        if (isDevelopment)
        {
            return Ok(new
            {
                message = "Verification code sent successfully. Please check your email or backend console.",
                emailSent = true
            });
        }

        return Ok(new { message = "Verification code sent successfully.", emailSent = true });
    }

    // Direct login without OTP
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        dto.Email = dto.Email.Trim().ToLowerInvariant();

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
        if (user == null)
        {
            return BadRequest(new { message = "Email is not registered. Please register first." });
        }

        var requestedRole = ResolveRole(dto.Email, dto.Role, user.Role);
        if (user.Role != requestedRole)
        {
            return BadRequest(new { message = $"This account is registered as a {user.Role}, not a {requestedRole}." });
        }

        // Generate access token
        string jwtToken = GenerateJwtToken(user);

        return Ok(new AuthResponseDto
        {
            Token = jwtToken,
            Email = user.Email,
            FullName = user.FullName,
            Role = user.Role
        });
    }

    // STEP 2: Verify OTP & Issue Token
    [HttpPost("verify-otp")]
    public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpDto dto)
    {
        dto.Email = dto.Email.Trim().ToLowerInvariant();
        dto.Code = dto.Code.Trim();

        Console.WriteLine($"[VerifyOtp] Email: '{dto.Email}', Code: '{dto.Code}'");

        // 1. Validate the OTP presence and expiry
        var otpRecord = await _context.OtpVerifications
            .FirstOrDefaultAsync(o => o.Email == dto.Email);

        if (otpRecord == null)
        {
            Console.WriteLine($"[VerifyOtp] OTP Record not found for Email: '{dto.Email}'");
            return BadRequest(new { message = "No OTP request found for this email address. Please request a new OTP." });
        }

        Console.WriteLine($"[VerifyOtp] Found record. Expected Code: '{otpRecord.Code}', Input Code: '{dto.Code}'. ExpiresAt (UTC): {otpRecord.ExpiresAt:yyyy-MM-dd HH:mm:ss}, Current UtcNow: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}");

        if (otpRecord.Code != dto.Code)
        {
            Console.WriteLine($"[VerifyOtp] Code mismatch.");
            return BadRequest(new { message = "Invalid verification code. Please check the OTP sent to your email." });
        }

        if (otpRecord.ExpiresAt < DateTime.UtcNow)
        {
            Console.WriteLine($"[VerifyOtp] OTP has expired.");
            return BadRequest(new { message = "Verification code has expired. Please request a new OTP." });
        }

        // 2. Remove the OTP record so it cannot be reused
        _context.OtpVerifications.Remove(otpRecord);

        // 3. Auto-Register or retrieve the User (Seamless Sign-in/Sign-up)
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
        var requestedRole = ResolveRole(dto.Email, dto.Role, user?.Role);

        if (user == null)
        {
            user = new User
            {
                Email = dto.Email,
                FullName = dto.Email.Split('@')[0],
                Role = requestedRole
            };
            await _context.Users.AddAsync(user);
        }
        else if (user.Role != requestedRole && requestedRole != user.Role)
        {
            // Only allow role change for new vendor/doctor signups from customer accounts
            if (user.Role == "Customer" && (requestedRole == "Vendor" || requestedRole == "Doctor"))
            {
                user.Role = requestedRole;
            }
        }
        await _context.SaveChangesAsync();

        // 3b. Auto-create Vendor or Doctor associated profiles if signing up with those roles
        if (user.Role == "Vendor")
        {
            var vendor = await _context.Vendors.FirstOrDefaultAsync(v => v.UserId == user.Id);
            if (vendor == null)
            {
                vendor = new Vendor
                {
                    BusinessName = user.FullName + " Pharmacy Store",
                    LicenseNumber = "LIC-" + Guid.NewGuid().ToString().Substring(0, 8).ToUpper(),
                    IsApproved = false,
                    UserId = user.Id
                };
                await _context.Vendors.AddAsync(vendor);
                await _context.SaveChangesAsync();
            }
        }
        else if (user.Role == "Doctor")
        {
            var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.UserId == user.Id);
            if (doctor == null)
            {
                doctor = new Doctor
                {
                    Name = "Dr " + user.FullName,
                    Specialty = "General Health",
                    Qualifications = "MBBS",
                    ExperienceYears = 5,
                    Languages = "English",
                    ConsultationFee = 350.00M,
                    UserId = user.Id,
                    IsApproved = false
                };
                await _context.Doctors.AddAsync(doctor);
                await _context.SaveChangesAsync();
            }
        }

        // 4. Generate access token
        string jwtToken = GenerateJwtToken(user);

        return Ok(new AuthResponseDto
        {
            Token = jwtToken,
            Email = user.Email,
            FullName = user.FullName,
            Role = user.Role
        });
    }

    private static string ResolveRole(string email, string? requestedRole, string? existingRole)
    {
        const string adminEmail = "admin@pharmeasy.com";
        if (email.Equals(adminEmail, StringComparison.OrdinalIgnoreCase))
            return "Admin";

        if (!string.IsNullOrEmpty(existingRole) && existingRole != "Customer")
            return existingRole;

        var role = requestedRole ?? "Customer";
        if (role == "Admin") return "Customer";
        if (role is "Customer" or "Vendor" or "Doctor") return role;
        return "Customer";
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
            new Claim(ClaimTypes.Name, user.FullName ?? string.Empty),
            new Claim(ClaimTypes.Role, user.Role)
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
