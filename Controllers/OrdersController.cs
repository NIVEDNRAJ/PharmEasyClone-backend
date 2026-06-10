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
[Authorize]
public class OrdersController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly IEmailService _emailService;

    public OrdersController(ApplicationDbContext context, IConfiguration configuration, IEmailService emailService)
    {
        _context = context;
        _configuration = configuration;
        _emailService = emailService;
    }

    // POST: api/orders/place
    [HttpPost("place")]
    public async Task<IActionResult> PlaceOrder([FromBody] PlaceOrderDto dto)
    {
        // 1. Get Logged in User ID
        var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out Guid userId))
        {
            return Unauthorized(new { message = "Unauthorized access." });
        }

        if (dto.Items == null || dto.Items.Count == 0)
        {
            return BadRequest(new { message = "Cart is empty." });
        }

        // 2. Map and calculate totals
        decimal totalMrp = 0;
        decimal discountAmount = 0;
        var orderItems = new List<OrderItem>();

        foreach (var cartItem in dto.Items)
        {
            var product = await _context.Products
                .Include(p => p.Inventories)
                .FirstOrDefaultAsync(p => p.Id == cartItem.ProductId);

            if (product == null)
            {
                return BadRequest(new { message = $"Product with ID {cartItem.ProductId} not found." });
            }

            // Get the cheapest vendor price and discount
            decimal itemPrice = 0;
            decimal itemDiscountPercent = 0;

            if (product.Inventories.Any())
            {
                var bestInventory = product.Inventories.OrderBy(i => i.Price).First();
                itemPrice = bestInventory.Price;
                itemDiscountPercent = bestInventory.DiscountPercentage;
            }

            decimal itemMrp = itemPrice / (1 - (itemDiscountPercent / 100));
            totalMrp += itemMrp * cartItem.Quantity;
            discountAmount += (itemMrp - itemPrice) * cartItem.Quantity;

            orderItems.Add(new OrderItem
            {
                ProductId = cartItem.ProductId,
                Quantity = cartItem.Quantity,
                Price = itemPrice,
                DiscountPercentage = itemDiscountPercent
            });
        }

        decimal paidAmount = totalMrp - discountAmount;
        if (paidAmount < 0) paidAmount = 0;

        // 3. Save order
        var order = new Order
        {
            UserId = userId,
            PatientName = dto.PatientName,
            DeliveryAddress = dto.DeliveryAddress,
            Pincode = dto.Pincode,
            TotalMrp = totalMrp,
            DiscountAmount = discountAmount,
            PaidAmount = paidAmount,
            PaymentStatus = "Pending",
            OrderStatus = "Processing",
            CreatedAt = DateTime.UtcNow,
            OrderItems = orderItems
        };

        await _context.Orders.AddAsync(order);
        await _context.SaveChangesAsync();

        // 4. Razorpay order creation
        string razorpayOrderId = "";
        var razorpayKeyId = _configuration["Razorpay:KeyId"];
        var razorpayKeySecret = _configuration["Razorpay:KeySecret"];

        try
        {
            if (string.IsNullOrEmpty(razorpayKeyId) || razorpayKeyId.Contains("YOUR_"))
            {
                razorpayOrderId = "order_mock_" + Guid.NewGuid().ToString().Substring(0, 14).Replace("-", "");
            }
            else
            {
                var client = new Razorpay.Api.RazorpayClient(razorpayKeyId, razorpayKeySecret);
                var options = new Dictionary<string, object>
                {
                    { "amount", (int)(paidAmount * 100) },
                    { "currency", "INR" },
                    { "receipt", order.Id.ToString() }
                };
                var razorOrder = client.Order.Create(options);
                razorpayOrderId = razorOrder["id"].ToString();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[OrdersController Razorpay Error]: {ex.Message}\n{ex.StackTrace}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"[Inner Exception]: {ex.InnerException.Message}");
            }
            razorpayOrderId = "order_mock_" + Guid.NewGuid().ToString().Substring(0, 14).Replace("-", "");
        }

        order.RazorpayOrderId = razorpayOrderId;
        _context.Entry(order).State = EntityState.Modified;
        await _context.SaveChangesAsync();

        return Ok(new
        {
            orderId = order.Id,
            paidAmount = order.PaidAmount,
            razorpayOrderId = razorpayOrderId,
            razorpayKeyId = razorpayKeyId ?? "rzp_test_mock"
        });
    }

    // POST: api/orders/confirm
    [HttpPost("confirm")]
    public async Task<IActionResult> ConfirmOrder([FromBody] ConfirmOrderPaymentDto dto)
    {
        var order = await _context.Orders
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
            .Include(o => o.User)
            .FirstOrDefaultAsync(o => o.Id == dto.OrderId);

        if (order == null)
        {
            return BadRequest(new { message = "Order not found." });
        }

        // Validate signature
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
            order.PaymentStatus = "Failed";
            await _context.SaveChangesAsync();
            return BadRequest(new { message = "Payment signature verification failed." });
        }

        // Update Order Status
        order.PaymentStatus = "Paid";
        order.RazorpayPaymentId = dto.RazorpayPaymentId;

        // Deduct inventory stock
        foreach (var item in order.OrderItems)
        {
            var inventory = await _context.VendorInventories
                .FirstOrDefaultAsync(vi => vi.ProductId == item.ProductId);
            if (inventory != null)
            {
                inventory.StockCount -= item.Quantity;
                if (inventory.StockCount < 0) inventory.StockCount = 0;
            }
        }

        await _context.SaveChangesAsync();

        // Send Confirmation Email
        if (order.User != null)
        {
            var itemLines = new List<string>();
            foreach (var item in order.OrderItems)
            {
                var prodName = item.Product != null ? item.Product.Name : "Medicine Item";
                itemLines.Add($"- {prodName} (Qty: {item.Quantity})");
            }
            string formattedItems = string.Join("\n", itemLines);

            await _emailService.SendMedicineOrderConfirmationEmailAsync(
                order.User.Email,
                order.PatientName,
                order.Id.ToString(),
                order.DeliveryAddress,
                order.Pincode,
                order.PaidAmount,
                formattedItems
            );
        }

        return Ok(new { message = "Order placed successfully.", orderId = order.Id });
    }

    // GET: api/orders/history
    [HttpGet("history")]
    public async Task<IActionResult> GetOrderHistory()
    {
        var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out Guid userId))
        {
            return Unauthorized(new { message = "Unauthorized access." });
        }

        var orders = await _context.Orders
            .Include(o => o.OrderItems)
            .ThenInclude(oi => oi.Product)
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.CreatedAt)
            .Select(o => new
            {
                o.Id,
                o.PatientName,
                o.DeliveryAddress,
                o.Pincode,
                o.TotalMrp,
                o.DiscountAmount,
                o.PaidAmount,
                o.PaymentStatus,
                o.OrderStatus,
                o.CreatedAt,
                Items = o.OrderItems.Select(oi => new {
                    ProductId = oi.ProductId,
                    ProductName = oi.Product != null ? oi.Product.Name : "Medicine Item",
                    ProductImageUrl = oi.Product != null ? oi.Product.ImageUrl : null,
                    Quantity = oi.Quantity,
                    Price = oi.Price
                })
            })
            .ToListAsync();

        return Ok(orders);
    }
}
