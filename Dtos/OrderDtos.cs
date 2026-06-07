using System.ComponentModel.DataAnnotations;

namespace pharmEasyClone_backend.Dtos;

public class PlaceOrderDto
{
    [Required]
    public string PatientName { get; set; } = string.Empty;

    [Required]
    public string DeliveryAddress { get; set; } = string.Empty;

    [Required]
    public string Pincode { get; set; } = string.Empty;

    [Required]
    public List<OrderItemDto> Items { get; set; } = new List<OrderItemDto>();
}

public class OrderItemDto
{
    [Required]
    public Guid ProductId { get; set; }

    [Required]
    public int Quantity { get; set; }
}

public class ConfirmOrderPaymentDto
{
    [Required]
    public Guid OrderId { get; set; }

    public string? RazorpayOrderId { get; set; }

    public string? RazorpayPaymentId { get; set; }

    public string? RazorpaySignature { get; set; }
}
