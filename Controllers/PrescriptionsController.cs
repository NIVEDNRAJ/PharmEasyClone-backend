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
public class PrescriptionsController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public PrescriptionsController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpPost("upload")]
    [Authorize]
    public async Task<IActionResult> UploadPrescription([FromBody] UploadPrescriptionDto dto)
    {
        var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdString, out Guid userId))
            return Unauthorized(new { message = "Unauthorized access." });

        var prescription = new Prescription
        {
            UserId = userId,
            PatientName = dto.PatientName,
            DeliveryAddress = dto.DeliveryAddress,
            Pincode = dto.Pincode,
            ImageUrl = dto.ImageUrl ?? "https://images.unsplash.com/photo-1584308666744-24d5c474f2ae?q=80&w=400&auto=format&fit=crop",
            Notes = dto.Notes,
            Status = "Pending"
        };

        await _context.Prescriptions.AddAsync(prescription);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Prescription uploaded successfully. Our pharmacist will verify and call you to confirm medicines.",
            prescriptionId = prescription.Id
        });
    }

    [HttpGet("my")]
    [Authorize]
    public async Task<IActionResult> GetMyPrescriptions()
    {
        var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdString, out Guid userId))
            return Unauthorized(new { message = "Unauthorized access." });

        var prescriptions = await _context.Prescriptions
            .Where(p => p.UserId == userId)
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new
            {
                p.Id,
                p.PatientName,
                p.DeliveryAddress,
                p.Pincode,
                p.ImageUrl,
                p.Notes,
                p.Status,
                p.CreatedAt
            })
            .ToListAsync();

        return Ok(prescriptions);
    }
}
