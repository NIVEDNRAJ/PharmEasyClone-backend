using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using pharmEasyClone_backend.Data;
using pharmEasyClone_backend.Dtos;

namespace pharmEasyClone_backend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Vendor")]
public class VendorPortalController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public VendorPortalController(ApplicationDbContext context)
    {
        _context = context;
    }

    private async Task<Models.Vendor?> GetCurrentVendor()
    {
        var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdString, out Guid userId)) return null;
        return await _context.Vendors.FirstOrDefaultAsync(v => v.UserId == userId);
    }

    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard()
    {
        var vendor = await GetCurrentVendor();
        if (vendor == null) return BadRequest(new { message = "Vendor profile not found." });

        var productIds = await _context.Products
            .Where(p => p.CreatedByVendorId == vendor.Id)
            .Select(p => p.Id)
            .ToListAsync();

        var stats = new
        {
            vendor.BusinessName,
            vendor.LicenseNumber,
            vendor.IsApproved,
            TotalProducts = productIds.Count,
            ApprovedProducts = await _context.Products.CountAsync(p => p.CreatedByVendorId == vendor.Id && p.IsApproved),
            PendingProducts = await _context.Products.CountAsync(p => p.CreatedByVendorId == vendor.Id && !p.IsApproved),
            TotalStock = await _context.VendorInventories
                .Where(vi => vi.VendorId == vendor.Id)
                .SumAsync(vi => vi.StockCount)
        };
        return Ok(stats);
    }

    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile()
    {
        var vendor = await GetCurrentVendor();
        if (vendor == null) return BadRequest(new { message = "Vendor profile not found." });

        return Ok(new
        {
            vendor.Id,
            vendor.BusinessName,
            vendor.LicenseNumber,
            vendor.IsApproved
        });
    }

    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateVendorProfileDto dto)
    {
        var vendor = await GetCurrentVendor();
        if (vendor == null) return BadRequest(new { message = "Vendor profile not found." });

        vendor.BusinessName = dto.BusinessName;
        vendor.LicenseNumber = dto.LicenseNumber;
        await _context.SaveChangesAsync();
        return Ok(new { message = "Profile updated." });
    }

    [HttpPut("inventory/{productId}")]
    public async Task<IActionResult> UpdateInventory(Guid productId, [FromBody] UpdateInventoryDto dto)
    {
        var vendor = await GetCurrentVendor();
        if (vendor == null) return BadRequest(new { message = "Vendor profile not found." });

        var inventory = await _context.VendorInventories
            .FirstOrDefaultAsync(vi => vi.VendorId == vendor.Id && vi.ProductId == productId);

        if (inventory == null) return NotFound(new { message = "Inventory not found." });

        inventory.Price = dto.Price;
        inventory.DiscountPercentage = dto.DiscountPercentage;
        inventory.StockCount = dto.StockCount;
        await _context.SaveChangesAsync();
        return Ok(new { message = "Inventory updated." });
    }

    [HttpGet("orders")]
    public async Task<IActionResult> GetVendorOrders()
    {
        var vendor = await GetCurrentVendor();
        if (vendor == null) return BadRequest(new { message = "Vendor profile not found." });

        var vendorProductIds = await _context.Products
            .Where(p => p.CreatedByVendorId == vendor.Id)
            .Select(p => p.Id)
            .ToListAsync();

        var orders = await _context.Orders
            .Include(o => o.OrderItems)
            .ThenInclude(oi => oi.Product)
            .Include(o => o.User)
            .Where(o => o.OrderItems.Any(oi => vendorProductIds.Contains(oi.ProductId)))
            .OrderByDescending(o => o.CreatedAt)
            .Select(o => new
            {
                o.Id,
                CustomerEmail = o.User != null ? o.User.Email : "N/A",
                o.PatientName,
                o.DeliveryAddress,
                o.Pincode,
                o.PaidAmount,
                o.PaymentStatus,
                o.OrderStatus,
                o.CreatedAt,
                Items = o.OrderItems
                    .Where(oi => vendorProductIds.Contains(oi.ProductId))
                    .Select(oi => new
                    {
                        ProductName = oi.Product != null ? oi.Product.Name : "Product",
                        oi.Quantity,
                        oi.Price
                    })
            })
            .ToListAsync();

        return Ok(orders);
    }
}
