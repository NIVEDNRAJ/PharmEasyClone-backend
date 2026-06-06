using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using pharmEasyClone_backend.Data;

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
        // Fetch products along with their cheapest available inventory pricing
        var products = await _context.Products
            .Include(p => p.Inventories)
            .Select(p => new
            {
                p.Id,
                p.Name,
                p.ImageUrl,
                p.Category,
                p.RequiresPrescription,
                // Get the best price available from all vendors
                BestPrice = p.Inventories.Any() ? p.Inventories.Min(i => i.Price) : 0,
                MaxDiscount = p.Inventories.Any() ? p.Inventories.Max(i => i.DiscountPercentage) : 0
            })
            .Take(10) // Limit to top 10 for the UI carousel
            .ToListAsync();

        return Ok(products);
    }

    // GET: api/products/categories
    [HttpGet("categories")]
    public async Task<IActionResult> GetCategories()
    {
        // Extract distinct categories dynamically from the products table
        var categories = await _context.Products
            .Select(p => p.Category)
            .Distinct()
            .ToListAsync();

        return Ok(categories);
    }
}