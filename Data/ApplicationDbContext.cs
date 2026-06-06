using Microsoft.EntityFrameworkCore;
using pharmEasyClone_backend.Models;

namespace pharmEasyClone_backend.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; } = null!;
        public DbSet<OtpVerification> OtpVerifications { get; set; } = null!;
        public DbSet<Vendor> Vendors { get; set; } = null!;
        public DbSet<Product> Products { get; set; } = null!;
        public DbSet<VendorInventory> VendorInventories { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure Composite Primary Key for Multi-Vendor Inventory Mapping
            modelBuilder.Entity<VendorInventory>()
                .HasKey(vi => new { vi.VendorId, vi.ProductId });

            modelBuilder.Entity<VendorInventory>()
                .HasOne(vi => vi.Vendor)
                .WithMany(v => v.Inventories)
                .HasForeignKey(vi => vi.VendorId);

            modelBuilder.Entity<VendorInventory>()
                .HasOne(vi => vi.Product)
                .WithMany(p => p.Inventories)
                .HasForeignKey(vi => vi.ProductId);
                
            // Configure precise precision for decimal fields
            modelBuilder.Entity<VendorInventory>()
                .Property(vi => vi.Price)
                .HasPrecision(18, 2);
                
            modelBuilder.Entity<VendorInventory>()
                .Property(vi => vi.DiscountPercentage)
                .HasPrecision(5, 2);
        }
    }
}