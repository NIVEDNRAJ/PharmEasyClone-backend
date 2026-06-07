using Microsoft.EntityFrameworkCore;
using pharmEasyClone_backend.Models;

namespace pharmEasyClone_backend.Data;

public static class DataSeeder
{
    public static void SeedData(ApplicationDbContext context)
    {
        // 1. Create a Default Vendor & Products (E-Commerce)
        if (!context.Products.Any())
        {
            var vendor = new Vendor
            {
                BusinessName = "Mumbai Central Pharmacy",
                LicenseNumber = "MH-PHARM-2026-001",
                IsApproved = true
            };
            context.Vendors.Add(vendor);
            context.SaveChanges();

            var products = new List<Product>
            {
                new Product { Name = "Shelcal 500mg Strip Of 15 Tablets", Description = "Calcium supplement", Category = "Medicine", ImageUrl = "https://images.unsplash.com/photo-1584308666744-24d5c474f2ae?q=80&w=150&auto=format&fit=crop", RequiresPrescription = false },
                new Product { Name = "Baidyanath Asli Ayurved Isabgol", Description = "Digestive health", Category = "Healthcare", ImageUrl = "https://images.unsplash.com/photo-1607619056574-7b8d304a3b3a?q=80&w=150&auto=format&fit=crop", RequiresPrescription = false },
                new Product { Name = "Dr. Morepen Bg-03 Glucometer Kit", Description = "Blood glucose monitor", Category = "Healthcare Devices", ImageUrl = "https://images.unsplash.com/photo-1603398938378-e54eab446dde?q=80&w=150&auto=format&fit=crop", RequiresPrescription = false },
                new Product { Name = "Evion 400mg Capsule", Description = "Vitamin E supplement", Category = "Vitamins", ImageUrl = "https://images.unsplash.com/photo-1550572017-edd951b55104?q=80&w=150&auto=format&fit=crop", RequiresPrescription = false },
                new Product { Name = "Amoxyclav 625 Tablet", Description = "Antibiotic", Category = "Medicine", ImageUrl = "https://images.unsplash.com/photo-1471864190281-a93a3070b6de?q=80&w=150&auto=format&fit=crop", RequiresPrescription = true }
            };
            context.Products.AddRange(products);
            context.SaveChanges();

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

        // 2. Seed Doctors (Matching the 17 Select Concern Screen Categories)
        if (!context.Doctors.Any())
        {
            var doctors = new List<Doctor>
            {
                new Doctor 
                { 
                    Name = "Dr Ankit Jain", 
                    Specialty = "General Health", 
                    Qualifications = "11 Years Exp - MD PAEDIATRICS, MBBS", 
                    ExperienceYears = 11, 
                    Languages = "Hindi, English", 
                    ConsultationFee = 499.00M, 
                    ImageUrl = "https://images.unsplash.com/photo-1612349317150-e413f6a5b16d?q=80&w=250&auto=format&fit=crop",
                    IsApproved = true
                },
                new Doctor 
                { 
                    Name = "Dr Preeti Sharma", 
                    Specialty = "Women's Health", 
                    Qualifications = "8 Years Exp - MD - Obstetrics & Gynaecology, MBBS", 
                    ExperienceYears = 8, 
                    Languages = "Hindi, English, Punjabi", 
                    ConsultationFee = 599.00M, 
                    ImageUrl = "https://images.unsplash.com/photo-1594824813573-246434de83fb?q=80&w=250&auto=format&fit=crop",
                    IsApproved = true
                },
                new Doctor 
                { 
                    Name = "Dr Rajesh Patel", 
                    Specialty = "Heart Health", 
                    Qualifications = "15 Years Exp - DM - Cardiology, MD - Medicine, MBBS", 
                    ExperienceYears = 15, 
                    Languages = "English, Gujarati", 
                    ConsultationFee = 799.00M, 
                    ImageUrl = "https://images.unsplash.com/photo-1622253692010-333f2da6031d?q=80&w=250&auto=format&fit=crop",
                    IsApproved = true
                },
                new Doctor 
                { 
                    Name = "Dr Amit Verma", 
                    Specialty = "Skin and Hair Health", 
                    Qualifications = "6 Years Exp - DDVL (Dermatology), MBBS", 
                    ExperienceYears = 6, 
                    Languages = "Hindi, English", 
                    ConsultationFee = 399.00M, 
                    ImageUrl = "https://images.unsplash.com/photo-1537368910025-700350fe46c7?q=80&w=250&auto=format&fit=crop",
                    IsApproved = true
                },
                new Doctor 
                { 
                    Name = "Dr Sunita Rao", 
                    Specialty = "Child Care", 
                    Qualifications = "10 Years Exp - DCH (Pediatrics), MBBS", 
                    ExperienceYears = 10, 
                    Languages = "Hindi, English, Kannada", 
                    ConsultationFee = 449.00M, 
                    ImageUrl = "https://images.unsplash.com/photo-1559839734-2b71ea197ec2?q=80&w=250&auto=format&fit=crop",
                    IsApproved = true
                },
                new Doctor 
                { 
                    Name = "Dr Vikram Malhotra", 
                    Specialty = "Sexual Health", 
                    Qualifications = "12 Years Exp - MD - Psychiatry, Sexologist", 
                    ExperienceYears = 12, 
                    Languages = "Hindi, English", 
                    ConsultationFee = 699.00M, 
                    ImageUrl = "https://images.unsplash.com/photo-1582750433449-64c676f49f40?q=80&w=250&auto=format&fit=crop",
                    IsApproved = true
                },
                new Doctor 
                { 
                    Name = "Dr Sanjay Gupta", 
                    Specialty = "Digestive and Liver Health", 
                    Qualifications = "14 Years Exp - MD - Gastroenterology, MBBS", 
                    ExperienceYears = 14, 
                    Languages = "Hindi, English, Bengali", 
                    ConsultationFee = 549.00M, 
                    ImageUrl = "https://images.unsplash.com/photo-1559839734-2b71ea197ec2?q=80&w=250&auto=format&fit=crop",
                    IsApproved = true
                },
                new Doctor 
                { 
                    Name = "Dr Nidhi Saxena", 
                    Specialty = "Blood Sugar Health", 
                    Qualifications = "9 Years Exp - MD - Endocrinology, MBBS", 
                    ExperienceYears = 9, 
                    Languages = "Hindi, English", 
                    ConsultationFee = 499.00M, 
                    ImageUrl = "https://images.unsplash.com/photo-1594824813573-246434de83fb?q=80&w=250&auto=format&fit=crop",
                    IsApproved = true
                },
                new Doctor 
                { 
                    Name = "Dr Ramesh Kumar", 
                    Specialty = "Bone and Joint Health", 
                    Qualifications = "16 Years Exp - MS - Orthopaedics, MBBS", 
                    ExperienceYears = 16, 
                    Languages = "Hindi, English, Tamil", 
                    ConsultationFee = 649.00M, 
                    ImageUrl = "https://images.unsplash.com/photo-1622253692010-333f2da6031d?q=80&w=250&auto=format&fit=crop",
                    IsApproved = true
                },
                new Doctor 
                { 
                    Name = "Dr Anil Kapoor", 
                    Specialty = "Pain Management", 
                    Qualifications = "13 Years Exp - MD - Anaesthesiology & Pain Specialist", 
                    ExperienceYears = 13, 
                    Languages = "Hindi, English", 
                    ConsultationFee = 500.00M, 
                    ImageUrl = "https://images.unsplash.com/photo-1537368910025-700350fe46c7?q=80&w=250&auto=format&fit=crop",
                    IsApproved = true
                },
                new Doctor 
                { 
                    Name = "Dr Shalini Sen", 
                    Specialty = "Eye Care", 
                    Qualifications = "7 Years Exp - MS - Ophthalmology, MBBS", 
                    ExperienceYears = 7, 
                    Languages = "Hindi, English", 
                    ConsultationFee = 399.00M, 
                    ImageUrl = "https://images.unsplash.com/photo-1594824813573-246434de83fb?q=80&w=250&auto=format&fit=crop",
                    IsApproved = true
                },
                new Doctor 
                { 
                    Name = "Dr K. P. Singh", 
                    Specialty = "Elder Care", 
                    Qualifications = "20 Years Exp - MD - Geriatrics, MBBS", 
                    ExperienceYears = 20, 
                    Languages = "Hindi, English", 
                    ConsultationFee = 599.00M, 
                    ImageUrl = "https://images.unsplash.com/photo-1612349317150-e413f6a5b16d?q=80&w=250&auto=format&fit=crop",
                    IsApproved = true
                }
            };
            context.Doctors.AddRange(doctors);
            context.SaveChanges();
        }

        // 3. Seed Coupons
        if (!context.Coupons.Any())
        {
            var coupons = new List<Coupon>
            {
                new Coupon { Code = "DOCTOR150", DiscountAmount = 150.00M, IsActive = true }
            };
            context.Coupons.AddRange(coupons);
            context.SaveChanges();
        }

        // 4. Seed Admin User
        if (!context.Users.Any(u => u.Email == "admin@pharmeasy.com"))
        {
            var adminUser = new User
            {
                Email = "admin@pharmeasy.com",
                FullName = "PharmEasy Admin",
                Role = "Admin"
            };
            context.Users.Add(adminUser);
            context.SaveChanges();
        }

        // 5. Seed Lab Tests
        if (!context.LabTests.Any())
        {
            var labTests = new List<LabTest>
            {
                new LabTest { Name = "Healthy 2026 Full Body Checkup", Description = "Diagnostic tool for screening and monitoring of your health", Category = "Full Body", Mrp = 3599, DiscountedPrice = 1649, DiscountPercentage = 54 },
                new LabTest { Name = "Diabetes Care", Description = "Preventive care package for diabetics", Category = "Diabetes", Mrp = 1399, DiscountedPrice = 849, DiscountPercentage = 39 },
                new LabTest { Name = "Basic Health Checkup", Description = "Assesses health of 47 essential body parameters", Category = "Full Body", Mrp = 2249, DiscountedPrice = 1049, DiscountPercentage = 53 },
                new LabTest { Name = "Aarogyam Full Body Checkup with Vitamins", Description = "Comprehensive health screening with vitamin profile", Category = "Full Body", Mrp = 4599, DiscountedPrice = 2599, DiscountPercentage = 43 },
                new LabTest { Name = "Thyroid Profile Test", Description = "Complete thyroid function assessment", Category = "Thyroid", Mrp = 599, DiscountedPrice = 399, DiscountPercentage = 33 },
                new LabTest { Name = "Lipid Profile Test", Description = "Cholesterol and lipid panel screening", Category = "Heart", Mrp = 699, DiscountedPrice = 449, DiscountPercentage = 36 },
                new LabTest { Name = "Liver Function Test", Description = "Comprehensive liver health assessment", Category = "Liver", Mrp = 799, DiscountedPrice = 499, DiscountPercentage = 38 },
                new LabTest { Name = "Vitamin D Test", Description = "Vitamin D deficiency screening", Category = "Vitamins", Mrp = 1299, DiscountedPrice = 799, DiscountPercentage = 38 }
            };
            context.LabTests.AddRange(labTests);
            context.SaveChanges();
        }
    }
}