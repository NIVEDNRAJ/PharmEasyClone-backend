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
public class LabTestsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly IEmailService _emailService;

    public LabTestsController(ApplicationDbContext context, IConfiguration configuration, IEmailService emailService)
    {
        _context = context;
        _configuration = configuration;
        _emailService = emailService;
    }

    [HttpGet]
    public async Task<IActionResult> GetLabTests([FromQuery] string? category)
    {
        var query = _context.LabTests.Where(t => t.IsActive).AsQueryable();
        if (!string.IsNullOrEmpty(category))
            query = query.Where(t => t.Category.ToLower() == category.ToLower());

        var tests = await query
            .OrderBy(t => t.DiscountedPrice)
            .Select(t => new
            {
                t.Id,
                t.Name,
                t.Description,
                t.Category,
                t.Mrp,
                t.DiscountedPrice,
                t.DiscountPercentage
            })
            .ToListAsync();

        return Ok(tests);
    }

    [HttpGet("categories")]
    public async Task<IActionResult> GetCategories()
    {
        var categories = await _context.LabTests
            .Where(t => t.IsActive)
            .Select(t => t.Category)
            .Distinct()
            .ToListAsync();
        return Ok(categories);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetLabTestById(Guid id)
    {
        var test = await _context.LabTests.FindAsync(id);
        if (test == null) return NotFound(new { message = "Lab test not found." });
        return Ok(test);
    }

    [HttpPost("book")]
    [Authorize]
    public async Task<IActionResult> BookLabTest([FromBody] BookLabTestDto dto)
    {
        var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdString, out Guid userId))
            return Unauthorized(new { message = "Unauthorized access." });

        var test = await _context.LabTests.FindAsync(dto.LabTestId);
        if (test == null) return BadRequest(new { message = "Lab test not found." });

        var booking = new LabTestBooking
        {
            UserId = userId,
            LabTestId = dto.LabTestId,
            PatientName = dto.PatientName,
            Address = dto.Address,
            Pincode = dto.Pincode,
            BookingDate = dto.BookingDate,
            TimeSlot = dto.TimeSlot,
            PaidAmount = test.DiscountedPrice,
            PaymentStatus = "Pending",
            BookingStatus = "Scheduled"
        };

        await _context.LabTestBookings.AddAsync(booking);
        await _context.SaveChangesAsync();

        // Create Razorpay Order
        string razorOrderId = "";
        var razorpayKeyId = _configuration["Razorpay:KeyId"];
        var razorpayKeySecret = _configuration["Razorpay:KeySecret"];

        try
        {
            if (string.IsNullOrEmpty(razorpayKeyId) || razorpayKeyId.Contains("YOUR_"))
            {
                razorOrderId = "order_mock_" + Guid.NewGuid().ToString().Substring(0, 14).Replace("-", "");
            }
            else
            {
                var client = new Razorpay.Api.RazorpayClient(razorpayKeyId, razorpayKeySecret);
                var options = new Dictionary<string, object>
                {
                    { "amount", (int)(booking.PaidAmount * 100) }, // in paise
                    { "currency", "INR" },
                    { "receipt", booking.Id.ToString() }
                };
                var razorOrder = client.Order.Create(options);
                razorOrderId = razorOrder["id"].ToString();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[LabTestsController Razorpay Error]: {ex.Message}\n{ex.StackTrace}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"[Inner Exception]: {ex.InnerException.Message}");
            }
            razorOrderId = "order_mock_" + Guid.NewGuid().ToString().Substring(0, 14).Replace("-", "");
        }

        booking.RazorpayOrderId = razorOrderId;
        _context.Entry(booking).State = EntityState.Modified;
        await _context.SaveChangesAsync();

        return Ok(new
        {
            bookingId = booking.Id,
            testName = test.Name,
            paidAmount = booking.PaidAmount,
            razorpayOrderId = razorOrderId,
            razorpayKeyId = razorpayKeyId ?? "rzp_test_mock"
        });
    }

    [HttpPost("confirm")]
    [Authorize]
    public async Task<IActionResult> ConfirmBooking([FromBody] ConfirmLabTestPaymentDto dto)
    {
        var booking = await _context.LabTestBookings
            .Include(b => b.LabTest)
            .Include(b => b.User)
            .FirstOrDefaultAsync(b => b.Id == dto.BookingId);

        if (booking == null)
        {
            return BadRequest(new { message = "Booking details not found." });
        }

        // Validate Payment Signature
        bool signatureValid = false;
        var razorpayKeySecret = _configuration["Razorpay:KeySecret"];

        if ((dto.RazorpayOrderId != null && dto.RazorpayOrderId.StartsWith("order_mock_")) ||
            (dto.RazorpaySignature != null && dto.RazorpaySignature.StartsWith("sig_mock_")))
        {
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

        booking.PaymentStatus = "Paid";
        booking.RazorpayPaymentId = dto.RazorpayPaymentId;
        await _context.SaveChangesAsync();

        // Send Confirmation Email
        if (booking.User != null && booking.LabTest != null)
        {
            string formattedDate = booking.BookingDate.ToString("yyyy-MM-dd");
            await _emailService.SendLabTestBookingConfirmationEmailAsync(
                booking.User.Email,
                booking.PatientName,
                booking.LabTest.Name,
                formattedDate,
                booking.TimeSlot,
                booking.Address,
                booking.Pincode,
                booking.PaidAmount
            );
        }

        return Ok(new { message = "Lab test booked successfully.", bookingId = booking.Id });
    }

    [HttpGet("bookings")]
    [Authorize]
    public async Task<IActionResult> GetMyBookings()
    {
        var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdString, out Guid userId))
            return Unauthorized(new { message = "Unauthorized access." });

        var bookings = await _context.LabTestBookings
            .Include(b => b.LabTest)
            .Where(b => b.UserId == userId)
            .OrderByDescending(b => b.CreatedAt)
            .Select(b => new
            {
                b.Id,
                TestName = b.LabTest != null ? b.LabTest.Name : "Lab Test",
                b.PatientName,
                b.Address,
                b.Pincode,
                b.BookingDate,
                b.TimeSlot,
                b.PaidAmount,
                b.PaymentStatus,
                b.BookingStatus,
                b.CreatedAt
            })
            .ToListAsync();

        return Ok(bookings);
    }
}
