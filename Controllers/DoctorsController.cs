using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using pharmEasyClone_backend.Data;

namespace pharmEasyClone_backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DoctorsController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public DoctorsController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: api/doctors
    [HttpGet]
    public async Task<IActionResult> GetDoctors([FromQuery] string? specialty)
    {
        var query = _context.Doctors.Where(d => d.IsApproved).AsQueryable();

        if (!string.IsNullOrEmpty(specialty))
        {
            query = query.Where(d => d.Specialty.ToLower() == specialty.ToLower());
        }

        var doctors = await query.ToListAsync();
        return Ok(doctors);
    }

    // GET: api/doctors/specialties
    [HttpGet("specialties")]
    public async Task<IActionResult> GetSpecialties()
    {
        var specialties = await _context.Doctors
            .Where(d => d.IsApproved)
            .Select(d => d.Specialty)
            .Distinct()
            .ToListAsync();
            
        return Ok(specialties);
    }

    // GET: api/doctors/{id}
    [HttpGet("{id}")]
    public async Task<IActionResult> GetDoctorById(Guid id)
    {
        var doctor = await _context.Doctors.FindAsync(id);
        if (doctor == null)
        {
            return NotFound(new { message = "Doctor not found." });
        }
        return Ok(doctor);
    }
}
