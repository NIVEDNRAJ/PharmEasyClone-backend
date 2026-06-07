using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using pharmEasyClone_backend.Data;

namespace pharmEasyClone_backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CouponsController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public CouponsController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: api/coupons/validate/{code}
    [HttpGet("validate/{code}")]
    public async Task<IActionResult> ValidateCoupon(string code)
    {
        var coupon = await _context.Coupons
            .FirstOrDefaultAsync(c => c.Code.ToLower() == code.ToLower() && c.IsActive);

        if (coupon == null)
        {
            return NotFound(new { message = "Invalid or inactive coupon code." });
        }

        return Ok(new
        {
            coupon.Code,
            coupon.DiscountAmount
        });
    }
}
