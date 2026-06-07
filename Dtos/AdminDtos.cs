namespace pharmEasyClone_backend.Dtos;

public class UpdateOrderStatusDto
{
    public string Status { get; set; } = string.Empty;
}

public class UpdateCouponDto
{
    public string Code { get; set; } = string.Empty;
    public decimal DiscountAmount { get; set; }
    public bool IsActive { get; set; } = true;
}
