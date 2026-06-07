using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using pharmEasyClone_backend.Data;
using pharmEasyClone_backend.Dtos;
using pharmEasyClone_backend.Models;

namespace pharmEasyClone_backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LabTestsController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public LabTestsController(ApplicationDbContext context)
    {
        _context = context;
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
            PaymentStatus = "Paid",
            BookingStatus = "Scheduled"
        };

        await _context.LabTestBookings.AddAsync(booking);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Lab test booked successfully. Sample collection will happen at your address.",
            bookingId = booking.Id,
            testName = test.Name,
            paidAmount = booking.PaidAmount
        });
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
