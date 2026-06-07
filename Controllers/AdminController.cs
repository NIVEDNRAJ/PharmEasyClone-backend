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
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public AdminController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard()
    {
        var stats = new
        {
            TotalUsers = await _context.Users.CountAsync(),
            TotalOrders = await _context.Orders.CountAsync(),
            TotalRevenue = await _context.Orders.Where(o => o.PaymentStatus == "Paid").SumAsync(o => o.PaidAmount),
            PendingProducts = await _context.Products.CountAsync(p => !p.IsApproved),
            PendingVendors = await _context.Vendors.CountAsync(v => !v.IsApproved),
            PendingDoctors = await _context.Doctors.CountAsync(d => !d.IsApproved),
            PendingPrescriptions = await _context.Prescriptions.CountAsync(p => p.Status == "Pending"),
            TotalConsultations = await _context.ConsultationBookings.CountAsync(),
            TotalLabBookings = await _context.LabTestBookings.CountAsync()
        };
        return Ok(stats);
    }

    [HttpGet("users")]
    public async Task<IActionResult> GetUsers()
    {
        var users = await _context.Users
            .OrderByDescending(u => u.CreatedAt)
            .Select(u => new
            {
                u.Id,
                u.Email,
                u.FullName,
                u.Role,
                u.CreatedAt
            })
            .ToListAsync();
        return Ok(users);
    }

    [HttpGet("vendors/pending")]
    public async Task<IActionResult> GetPendingVendors()
    {
        var vendors = await _context.Vendors
            .Where(v => !v.IsApproved)
            .Include(v => v.User)
            .Select(v => new
            {
                v.Id,
                v.BusinessName,
                v.LicenseNumber,
                v.IsApproved,
                Email = v.User != null ? v.User.Email : "N/A",
                OwnerName = v.User != null ? v.User.FullName : "N/A"
            })
            .ToListAsync();
        return Ok(vendors);
    }

    [HttpPost("vendors/{id}/approve")]
    public async Task<IActionResult> ApproveVendor(Guid id)
    {
        var vendor = await _context.Vendors.FindAsync(id);
        if (vendor == null) return NotFound(new { message = "Vendor not found." });

        vendor.IsApproved = true;
        await _context.SaveChangesAsync();
        return Ok(new { message = "Vendor approved successfully." });
    }

    [HttpPost("vendors/{id}/reject")]
    public async Task<IActionResult> RejectVendor(Guid id)
    {
        var vendor = await _context.Vendors.FindAsync(id);
        if (vendor == null) return NotFound(new { message = "Vendor not found." });

        _context.Vendors.Remove(vendor);
        await _context.SaveChangesAsync();
        return Ok(new { message = "Vendor rejected and removed." });
    }

    [HttpGet("doctors/pending")]
    public async Task<IActionResult> GetPendingDoctors()
    {
        var doctors = await _context.Doctors
            .Where(d => !d.IsApproved)
            .Include(d => d.User)
            .Select(d => new
            {
                d.Id,
                d.Name,
                d.Specialty,
                d.Qualifications,
                d.ExperienceYears,
                d.Languages,
                d.ConsultationFee,
                d.IsApproved,
                Email = d.User != null ? d.User.Email : "N/A",
                OwnerName = d.User != null ? d.User.FullName : "N/A"
            })
            .ToListAsync();
        return Ok(doctors);
    }

    [HttpPost("doctors/{id}/approve")]
    public async Task<IActionResult> ApproveDoctor(Guid id)
    {
        var doctor = await _context.Doctors.FindAsync(id);
        if (doctor == null) return NotFound(new { message = "Doctor not found." });

        doctor.IsApproved = true;
        await _context.SaveChangesAsync();
        return Ok(new { message = "Doctor approved successfully." });
    }

    [HttpPost("doctors/{id}/reject")]
    public async Task<IActionResult> RejectDoctor(Guid id)
    {
        var doctor = await _context.Doctors.FindAsync(id);
        if (doctor == null) return NotFound(new { message = "Doctor not found." });

        _context.Doctors.Remove(doctor);
        await _context.SaveChangesAsync();
        return Ok(new { message = "Doctor rejected and removed." });
    }

    [HttpGet("orders")]
    public async Task<IActionResult> GetAllOrders()
    {
        var orders = await _context.Orders
            .Include(o => o.OrderItems)
            .ThenInclude(oi => oi.Product)
            .Include(o => o.User)
            .OrderByDescending(o => o.CreatedAt)
            .Select(o => new
            {
                o.Id,
                CustomerEmail = o.User != null ? o.User.Email : "N/A",
                o.PatientName,
                o.DeliveryAddress,
                o.Pincode,
                o.TotalMrp,
                o.DiscountAmount,
                o.PaidAmount,
                o.PaymentStatus,
                o.OrderStatus,
                o.CreatedAt,
                ItemCount = o.OrderItems.Count
            })
            .ToListAsync();
        return Ok(orders);
    }

    [HttpPut("orders/{id}/status")]
    public async Task<IActionResult> UpdateOrderStatus(Guid id, [FromBody] UpdateOrderStatusDto dto)
    {
        var order = await _context.Orders.FindAsync(id);
        if (order == null) return NotFound(new { message = "Order not found." });

        order.OrderStatus = dto.Status;
        await _context.SaveChangesAsync();
        return Ok(new { message = "Order status updated.", orderId = id, status = dto.Status });
    }

    [HttpGet("coupons")]
    public async Task<IActionResult> GetCoupons()
    {
        var coupons = await _context.Coupons.ToListAsync();
        return Ok(coupons);
    }

    [HttpPost("coupons")]
    public async Task<IActionResult> CreateCoupon([FromBody] UpdateCouponDto dto)
    {
        var existing = await _context.Coupons.FirstOrDefaultAsync(c => c.Code.ToLower() == dto.Code.ToLower());
        if (existing != null) return BadRequest(new { message = "Coupon code already exists." });

        var coupon = new Coupon
        {
            Code = dto.Code.ToUpper(),
            DiscountAmount = dto.DiscountAmount,
            IsActive = dto.IsActive
        };
        await _context.Coupons.AddAsync(coupon);
        await _context.SaveChangesAsync();
        return Ok(new { message = "Coupon created.", couponId = coupon.Id });
    }

    [HttpPut("coupons/{id}")]
    public async Task<IActionResult> UpdateCoupon(Guid id, [FromBody] UpdateCouponDto dto)
    {
        var coupon = await _context.Coupons.FindAsync(id);
        if (coupon == null) return NotFound(new { message = "Coupon not found." });

        coupon.Code = dto.Code.ToUpper();
        coupon.DiscountAmount = dto.DiscountAmount;
        coupon.IsActive = dto.IsActive;
        await _context.SaveChangesAsync();
        return Ok(new { message = "Coupon updated." });
    }

    [HttpGet("prescriptions")]
    public async Task<IActionResult> GetPrescriptions()
    {
        var prescriptions = await _context.Prescriptions
            .Include(p => p.User)
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
                p.CreatedAt,
                CustomerEmail = p.User != null ? p.User.Email : "N/A"
            })
            .ToListAsync();
        return Ok(prescriptions);
    }

    [HttpPut("prescriptions/{id}/status")]
    public async Task<IActionResult> UpdatePrescriptionStatus(Guid id, [FromBody] UpdatePrescriptionStatusDto dto)
    {
        var prescription = await _context.Prescriptions.FindAsync(id);
        if (prescription == null) return NotFound(new { message = "Prescription not found." });

        prescription.Status = dto.Status;
        await _context.SaveChangesAsync();
        return Ok(new { message = "Prescription status updated." });
    }
}
