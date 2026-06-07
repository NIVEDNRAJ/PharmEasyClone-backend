namespace pharmEasyClone_backend.Dtos;

public class UpdateInventoryDto
{
    public decimal Price { get; set; }
    public decimal DiscountPercentage { get; set; }
    public int StockCount { get; set; }
}

public class UpdateVendorProfileDto
{
    public string BusinessName { get; set; } = string.Empty;
    public string LicenseNumber { get; set; } = string.Empty;
}
