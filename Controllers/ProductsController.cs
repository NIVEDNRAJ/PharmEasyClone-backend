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
public class ProductsController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public ProductsController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: api/products/trending
    [HttpGet("trending")]
    public async Task<IActionResult> GetTrendingProducts()
    {
        // Only return approved products
        var products = await _context.Products
            .Where(p => p.IsApproved)
            .Include(p => p.Inventories)
            .Select(p => new
            {
                p.Id,
                p.Name,
                p.ImageUrl,
                p.Category,
                p.RequiresPrescription,
                BestPrice = p.Inventories.Any() ? p.Inventories.Min(i => i.Price) : 0,
                MaxDiscount = p.Inventories.Any() ? p.Inventories.Max(i => i.DiscountPercentage) : 0
            })
            .Take(10)
            .ToListAsync();

        return Ok(products);
    }

    // GET: api/products/categories
    [HttpGet("categories")]
    public async Task<IActionResult> GetCategories()
    {
        var categories = await _context.Products
            .Where(p => p.IsApproved)
            .Select(p => p.Category)
            .Distinct()
            .ToListAsync();

        return Ok(categories);
    }

    // GET: api/products
    [HttpGet]
    public async Task<IActionResult> GetProducts([FromQuery] string? searchTerm, [FromQuery] string? category)
    {
        // Only return approved products
        var query = _context.Products.Where(p => p.IsApproved).Include(p => p.Inventories).AsQueryable();

        if (!string.IsNullOrEmpty(searchTerm))
        {
            var searchLower = searchTerm.ToLower();
            query = query.Where(p => p.Name.ToLower().Contains(searchLower) || p.Description.ToLower().Contains(searchLower));
        }

        if (!string.IsNullOrEmpty(category))
        {
            query = query.Where(p => p.Category.ToLower() == category.ToLower());
        }

        var products = await query
            .Select(p => new
            {
                p.Id,
                p.Name,
                p.ImageUrl,
                p.Category,
                p.RequiresPrescription,
                BestPrice = p.Inventories.Any() ? p.Inventories.Min(i => i.Price) : 0,
                MaxDiscount = p.Inventories.Any() ? p.Inventories.Max(i => i.DiscountPercentage) : 0
            })
            .ToListAsync();

        return Ok(products);
    }

    // GET: api/products/{id}
    [HttpGet("{id}")]
    public async Task<IActionResult> GetProductById(Guid id)
    {
        var product = await _context.Products
            .Where(p => p.IsApproved)
            .Include(p => p.Inventories)
            .ThenInclude(i => i.Vendor)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (product == null)
        {
            return NotFound(new { message = "Product not found." });
        }

        return Ok(new
        {
            product.Id,
            product.Name,
            product.Description,
            product.ImageUrl,
            product.Category,
            product.RequiresPrescription,
            BestPrice = product.Inventories.Any() ? product.Inventories.Min(i => i.Price) : 0,
            MaxDiscount = product.Inventories.Any() ? product.Inventories.Max(i => i.DiscountPercentage) : 0,
            Inventories = product.Inventories.Select(i => new {
                VendorId = i.VendorId,
                VendorName = i.Vendor != null ? i.Vendor.BusinessName : "Mumbai Central Pharmacy",
                Price = i.Price,
                DiscountPercentage = i.DiscountPercentage,
                StockCount = i.StockCount
            })
        });
    }

    // ==========================================
    // ROLE-BASED ACCESS: VENDOR UPLOADS
    // ==========================================

    // POST: api/products/vendor
    [HttpPost("vendor")]
    [Authorize(Roles = "Vendor")]
    public async Task<IActionResult> CreateProductByVendor([FromBody] VendorAddProductDto dto)
    {
        var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdString, out Guid userId))
        {
            return Unauthorized(new { message = "Unauthorized access." });
        }

        var vendor = await _context.Vendors.FirstOrDefaultAsync(v => v.UserId == userId);
        if (vendor == null)
        {
            return BadRequest(new { message = "Vendor profile not found." });
        }

        if (!vendor.IsApproved)
        {
            return BadRequest(new { message = "Your vendor account is pending admin approval. You cannot upload products yet." });
        }

        var product = new Product
        {
            Name = dto.Name,
            Description = dto.Description,
            Category = dto.Category,
            ImageUrl = string.IsNullOrEmpty(dto.ImageUrl) ? "https://images.unsplash.com/photo-1584308666744-24d5c474f2ae?q=80&w=150&auto=format&fit=crop" : dto.ImageUrl,
            RequiresPrescription = dto.RequiresPrescription,
            IsApproved = false, // Must be approved by Admin
            CreatedByVendorId = vendor.Id
        };

        await _context.Products.AddAsync(product);
        await _context.SaveChangesAsync();

        var inventory = new VendorInventory
        {
            VendorId = vendor.Id,
            ProductId = product.Id,
            Price = dto.Price,
            DiscountPercentage = dto.DiscountPercentage,
            StockCount = dto.StockCount
        };

        await _context.VendorInventories.AddAsync(inventory);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Product uploaded successfully. Pending Admin approval.", productId = product.Id });
    }

    // GET: api/products/vendor
    [HttpGet("vendor")]
    [Authorize(Roles = "Vendor")]
    public async Task<IActionResult> GetVendorProducts()
    {
        var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdString, out Guid userId))
        {
            return Unauthorized(new { message = "Unauthorized access." });
        }

        var vendor = await _context.Vendors.FirstOrDefaultAsync(v => v.UserId == userId);
        if (vendor == null)
        {
            return BadRequest(new { message = "Vendor profile not found." });
        }

        var products = await _context.Products
            .Where(p => p.CreatedByVendorId == vendor.Id)
            .Include(p => p.Inventories)
            .Select(p => new
            {
                p.Id,
                p.Name,
                p.ImageUrl,
                p.Category,
                p.IsApproved,
                Price = p.Inventories.Any() ? p.Inventories.First().Price : 0,
                StockCount = p.Inventories.Any() ? p.Inventories.First().StockCount : 0
            })
            .ToListAsync();

        return Ok(products);
    }

    // ==========================================
    // ROLE-BASED ACCESS: ADMIN APPROVALS
    // ==========================================

    // GET: api/products/pending
    [HttpGet("pending")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetPendingProducts()
    {
        var products = await _context.Products
            .Where(p => !p.IsApproved)
            .Select(p => new
            {
                p.Id,
                p.Name,
                p.Description,
                p.Category,
                p.ImageUrl,
                VendorName = _context.Vendors
                    .Where(v => v.Id == p.CreatedByVendorId)
                    .Select(v => v.BusinessName)
                    .FirstOrDefault() ?? "Unknown Vendor",
                Price = p.Inventories.Any() ? p.Inventories.First().Price : 0
            })
            .ToListAsync();

        return Ok(products);
    }

    // POST: api/products/{id}/approve
    [HttpPost("{id}/approve")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ApproveProduct(Guid id)
    {
        var product = await _context.Products.FindAsync(id);
        if (product == null)
        {
            return NotFound(new { message = "Product not found." });
        }

        product.IsApproved = true;
        _context.Entry(product).State = EntityState.Modified;
        await _context.SaveChangesAsync();

        return Ok(new { message = "Product approved successfully and is now live." });
    }

    [HttpPost("{id}/reject")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> RejectProduct(Guid id)
    {
        var product = await _context.Products
            .Include(p => p.Inventories)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (product == null)
            return NotFound(new { message = "Product not found." });

        _context.VendorInventories.RemoveRange(product.Inventories);
        _context.Products.Remove(product);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Product rejected and removed." });
    }
}