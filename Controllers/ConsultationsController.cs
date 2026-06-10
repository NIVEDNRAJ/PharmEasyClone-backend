using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using pharmEasyClone_backend.Data;
using pharmEasyClone_backend.Dtos;
using pharmEasyClone_backend.Models;
using pharmEasyClone_backend.Services;

namespace pharmEasyClone_backend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ConsultationsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly IEmailService _emailService;

    public ConsultationsController(
        ApplicationDbContext context,
        IConfiguration configuration,
        IEmailService emailService)
    {
        _context = context;
        _configuration = configuration;
        _emailService = emailService;
    }

    // POST: api/consultations/book
    [HttpPost("book")]
    public async Task<IActionResult> CreateBooking([FromBody] CreateBookingDto dto)
    {
        // 1. Get Logged in User ID from JWT Claim
        var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out Guid userId))
        {
            return Unauthorized(new { message = "Unauthorized access." });
        }

        // 2. Retrieve Doctor
        var doctor = await _context.Doctors.FindAsync(dto.DoctorId);
        if (doctor == null || !doctor.IsApproved)
        {
            return BadRequest(new { message = "Doctor not found or not approved." });
        }

        // 3. Compute Fees & Discount
        decimal baseFee = doctor.ConsultationFee;
        decimal discount = 0;

        if (!string.IsNullOrEmpty(dto.CouponCode))
        {
            var coupon = await _context.Coupons
                .FirstOrDefaultAsync(c => c.Code.ToLower() == dto.CouponCode.ToLower() && c.IsActive);
            if (coupon != null)
            {
                discount = coupon.DiscountAmount;
            }
        }

        decimal paidAmount = baseFee - discount;
        if (paidAmount < 0) paidAmount = 0;

        // 4. Create the booking entry in Database
        var booking = new ConsultationBooking
        {
            UserId = userId,
            DoctorId = dto.DoctorId,
            PatientName = dto.PatientName,
            Gender = dto.Gender,
            Symptoms = dto.Symptoms,
            Mode = dto.Mode,
            BookingDate = dto.BookingDate,
            TimeSlot = dto.TimeSlot,
            ConsultationFee = baseFee,
            DiscountAmount = discount,
            PaidAmount = paidAmount,
            PaymentStatus = "Pending",
            BookingStatus = "Scheduled",
            CreatedAt = DateTime.UtcNow
        };

        await _context.ConsultationBookings.AddAsync(booking);
        await _context.SaveChangesAsync();

        // 5. Create Razorpay Order
        string orderId = "";
        var razorpayKeyId = _configuration["Razorpay:KeyId"];
        var razorpayKeySecret = _configuration["Razorpay:KeySecret"];

        try
        {
            if (string.IsNullOrEmpty(razorpayKeyId) || razorpayKeyId.Contains("YOUR_"))
            {
                // Fallback to mock order creation
                orderId = "order_mock_" + Guid.NewGuid().ToString().Substring(0, 14).Replace("-", "");
            }
            else
            {
                var client = new Razorpay.Api.RazorpayClient(razorpayKeyId, razorpayKeySecret);
                var options = new Dictionary<string, object>
                {
                    { "amount", (int)(paidAmount * 100) }, // amount in paise (e.g. 349.00 -> 34900 paise)
                    { "currency", "INR" },
                    { "receipt", booking.Id.ToString() }
                };
                var order = client.Order.Create(options);
                orderId = order["id"].ToString();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ConsultationsController Razorpay Error]: {ex.Message}\n{ex.StackTrace}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"[Inner Exception]: {ex.InnerException.Message}");
            }
            orderId = "order_mock_" + Guid.NewGuid().ToString().Substring(0, 14).Replace("-", "");
        }

        // Update booking with Razorpay Order ID
        booking.RazorpayOrderId = orderId;
        _context.Entry(booking).State = EntityState.Modified;
        await _context.SaveChangesAsync();

        return Ok(new
        {
            bookingId = booking.Id,
            doctorName = doctor.Name,
            specialty = doctor.Specialty,
            paidAmount = booking.PaidAmount,
            razorpayOrderId = orderId,
            razorpayKeyId = razorpayKeyId ?? "rzp_test_mock"
        });
    }

    // POST: api/consultations/confirm
    [HttpPost("confirm")]
    public async Task<IActionResult> ConfirmBooking([FromBody] ConfirmPaymentDto dto)
    {
        // 1. Get booking
        var booking = await _context.ConsultationBookings
            .Include(b => b.Doctor)
            .Include(b => b.User)
            .FirstOrDefaultAsync(b => b.Id == dto.BookingId);

        if (booking == null)
        {
            return BadRequest(new { message = "Booking details not found." });
        }

        // 2. Validate Payment Signature
        bool signatureValid = false;
        var razorpayKeySecret = _configuration["Razorpay:KeySecret"];

        if ((dto.RazorpayOrderId != null && dto.RazorpayOrderId.StartsWith("order_mock_")) ||
            (dto.RazorpaySignature != null && dto.RazorpaySignature.StartsWith("sig_mock_")))
        {
            // Simulated success
            signatureValid = true;
        }
        else
        {
            try
            {
                var attributes = new Dictionary<string, string>
                {
                    { "razorpay_order_id", dto.RazorpayOrderId ?? "" },
                    { "razorpay_payment_id", dto.RazorpayPaymentId ?? "" },
                    { "razorpay_signature", dto.RazorpaySignature ?? "" }
                };
                Razorpay.Api.Utils.verifyPaymentSignature(attributes);
                signatureValid = true;
            }
            catch
            {
                signatureValid = false;
            }
        }

        if (!signatureValid)
        {
            booking.PaymentStatus = "Failed";
            await _context.SaveChangesAsync();
            return BadRequest(new { message = "Payment signature verification failed." });
        }

        // 3. Mark booking as paid
        booking.PaymentStatus = "Paid";
        booking.RazorpayPaymentId = dto.RazorpayPaymentId;
        await _context.SaveChangesAsync();

        // 4. Send Confirmation Email via Brevo
        if (booking.User != null && booking.Doctor != null)
        {
            string formattedDate = booking.BookingDate.ToString("yyyy-MM-dd");
            await _emailService.SendBookingConfirmationEmailAsync(
                booking.User.Email,
                booking.PatientName,
                booking.Doctor.Name,
                booking.Doctor.Specialty,
                formattedDate,
                booking.TimeSlot,
                booking.Mode,
                booking.PaidAmount
            );
        }

        return Ok(new { message = "Appointment booked successfully.", bookingId = booking.Id });
    }

    // GET: api/consultations/history
    [HttpGet("history")]
    public async Task<IActionResult> GetBookingHistory()
    {
        // Get user ID
        var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out Guid userId))
        {
            return Unauthorized(new { message = "Unauthorized access." });
        }

        var history = await _context.ConsultationBookings
            .Include(b => b.Doctor)
            .Where(b => b.UserId == userId)
            .OrderByDescending(b => b.BookingDate)
            .ThenByDescending(b => b.CreatedAt)
            .Select(b => new
            {
                b.Id,
                b.PatientName,
                b.Gender,
                b.Symptoms,
                b.Mode,
                b.BookingDate,
                b.TimeSlot,
                b.PaidAmount,
                b.PaymentStatus,
                b.BookingStatus,
                DoctorName = b.Doctor != null ? b.Doctor.Name : "General Doctor",
                DoctorSpecialty = b.Doctor != null ? b.Doctor.Specialty : "General Physician",
                DoctorImageUrl = b.Doctor != null ? b.Doctor.ImageUrl : null
            })
            .ToListAsync();

        return Ok(history);
    }
}
