using Microsoft.EntityFrameworkCore;
using pharmEasyClone_backend.Models;

namespace pharmEasyClone_backend.Data;

public static class DataSeeder
{
    public static void SeedData(ApplicationDbContext context)
    {
        // Check if we already have products. If yes, skip seeding.
        if (context.Products.Any()) return;

        // 1. Create a Default Vendor
        var vendor = new Vendor
        {
            BusinessName = "Mumbai Central Pharmacy",
            LicenseNumber = "MH-PHARM-2026-001",
            IsApproved = true
        };
        context.Vendors.Add(vendor);
        context.SaveChanges();

        // 2. Create Products (Matching your UI screenshots)
        var products = new List<Product>
        {
            new Product { Name = "Shelcal 500mg Strip Of 15 Tablets", Description = "Calcium supplement", Category = "Medicine", ImageUrl = "https://via.placeholder.com/150", RequiresPrescription = false },
            new Product { Name = "Baidyanath Asli Ayurved Isabgol", Description = "Digestive health", Category = "Healthcare", ImageUrl = "https://via.placeholder.com/150", RequiresPrescription = false },
            new Product { Name = "Dr. Morepen Bg-03 Glucometer Kit", Description = "Blood glucose monitor", Category = "Healthcare Devices", ImageUrl = "https://via.placeholder.com/150", RequiresPrescription = false },
            new Product { Name = "Evion 400mg Capsule", Description = "Vitamin E supplement", Category = "Vitamins", ImageUrl = "https://via.placeholder.com/150", RequiresPrescription = false },
            new Product { Name = "Amoxyclav 625 Tablet", Description = "Antibiotic", Category = "Medicine", ImageUrl = "https://via.placeholder.com/150", RequiresPrescription = true }
        };
        context.Products.AddRange(products);
        context.SaveChanges();

        // 3. Link Products to Vendor Inventory with Prices
        var random = new Random();
        foreach (var product in products)
        {
            var basePrice = random.Next(50, 800);
            context.VendorInventories.Add(new VendorInventory
            {
                VendorId = vendor.Id,
                ProductId = product.Id,
                Price = basePrice,
                DiscountPercentage = random.Next(5, 25), // 5% to 25% off
                StockCount = random.Next(10, 100)
            });
        }
        context.SaveChanges();
    }
}