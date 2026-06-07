using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using pharmEasyClone_backend.Data;
using pharmEasyClone_backend.Dtos;

namespace pharmEasyClone_backend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Doctor")]
public class DoctorPortalController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public DoctorPortalController(ApplicationDbContext context)
    {
        _context = context;
    }

    private async Task<Models.Doctor?> GetCurrentDoctor()
    {
        var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdString, out Guid userId)) return null;
        return await _context.Doctors.FirstOrDefaultAsync(d => d.UserId == userId);
    }

    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard()
    {
        var doctor = await GetCurrentDoctor();
        if (doctor == null) return BadRequest(new { message = "Doctor profile not found." });

        var today = DateTime.UtcNow.Date;
        var stats = new
        {
            doctor.Name,
            doctor.Specialty,
            doctor.ConsultationFee,
            doctor.IsApproved,
            UpcomingAppointments = await _context.ConsultationBookings
                .CountAsync(b => b.DoctorId == doctor.Id && b.BookingDate >= today && b.BookingStatus == "Scheduled"),
            CompletedAppointments = await _context.ConsultationBookings
                .CountAsync(b => b.DoctorId == doctor.Id && b.BookingStatus == "Completed"),
            TotalEarnings = await _context.ConsultationBookings
                .Where(b => b.DoctorId == doctor.Id && b.PaymentStatus == "Paid")
                .SumAsync(b => b.PaidAmount)
        };
        return Ok(stats);
    }

    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile()
    {
        var doctor = await GetCurrentDoctor();
        if (doctor == null) return BadRequest(new { message = "Doctor profile not found." });
        return Ok(doctor);
    }

    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateDoctorProfileDto dto)
    {
        var doctor = await GetCurrentDoctor();
        if (doctor == null) return BadRequest(new { message = "Doctor profile not found." });

        doctor.Name = dto.Name;
        doctor.Specialty = dto.Specialty;
        doctor.Qualifications = dto.Qualifications;
        doctor.ExperienceYears = dto.ExperienceYears;
        doctor.Languages = dto.Languages;
        doctor.ConsultationFee = dto.ConsultationFee;
        if (!string.IsNullOrEmpty(dto.ImageUrl)) doctor.ImageUrl = dto.ImageUrl;

        await _context.SaveChangesAsync();
        return Ok(new { message = "Profile updated." });
    }

    [HttpGet("appointments")]
    public async Task<IActionResult> GetAppointments([FromQuery] string? status)
    {
        var doctor = await GetCurrentDoctor();
        if (doctor == null) return BadRequest(new { message = "Doctor profile not found." });

        if (!doctor.IsApproved)
        {
            return BadRequest(new { message = "Your doctor account is pending admin approval." });
        }

        var query = _context.ConsultationBookings
            .Include(b => b.User)
            .Where(b => b.DoctorId == doctor.Id);

        if (!string.IsNullOrEmpty(status))
            query = query.Where(b => b.BookingStatus == status);

        var appointments = await query
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
                CustomerEmail = b.User != null ? b.User.Email : "N/A"
            })
            .ToListAsync();

        return Ok(appointments);
    }

    [HttpPut("appointments/{id}/status")]
    public async Task<IActionResult> UpdateAppointmentStatus(Guid id, [FromBody] UpdateBookingStatusDto dto)
    {
        var doctor = await GetCurrentDoctor();
        if (doctor == null) return BadRequest(new { message = "Doctor profile not found." });

        if (!doctor.IsApproved)
        {
            return BadRequest(new { message = "Your doctor account is pending admin approval." });
        }

        var booking = await _context.ConsultationBookings
            .FirstOrDefaultAsync(b => b.Id == id && b.DoctorId == doctor.Id);

        if (booking == null) return NotFound(new { message = "Appointment not found." });

        booking.BookingStatus = dto.Status;
        await _context.SaveChangesAsync();
        return Ok(new { message = "Appointment status updated." });
    }
}
