using Microsoft.EntityFrameworkCore;
using PoultrySlaughterPOS.Models;

namespace PoultrySlaughterPOS.Data
{
    public class PoultryDbContext : DbContext
    {
        public PoultryDbContext(DbContextOptions<PoultryDbContext> options) : base(options)
        {
        }

        public DbSet<Customer> Customers { get; set; }
        public DbSet<Truck> Trucks { get; set; }
        public DbSet<Invoice> Invoices { get; set; }
        public DbSet<Payment> Payments { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Customer configuration
            modelBuilder.Entity<Customer>(entity =>
            {
                entity.HasKey(e => e.CustomerId);
                entity.HasIndex(e => e.CustomerName);
                entity.Property(e => e.TotalDebt).HasPrecision(18, 2);
            });

            // Truck configuration
            modelBuilder.Entity<Truck>(entity =>
            {
                entity.HasKey(e => e.TruckId);
                entity.HasIndex(e => e.TruckNumber).IsUnique();
            });

            // Invoice configuration
            modelBuilder.Entity<Invoice>(entity =>
            {
                entity.HasKey(e => e.InvoiceId);
                entity.HasIndex(e => e.InvoiceNumber).IsUnique();

                // Configure decimal precision
                entity.Property(e => e.GrossWeight).HasPrecision(18, 2);
                entity.Property(e => e.CagesWeight).HasPrecision(18, 2);
                entity.Property(e => e.NetWeight).HasPrecision(18, 2);
                entity.Property(e => e.UnitPrice).HasPrecision(18, 2);
                entity.Property(e => e.TotalAmount).HasPrecision(18, 2);
                entity.Property(e => e.DiscountPercentage).HasPrecision(5, 2);
                entity.Property(e => e.FinalAmount).HasPrecision(18, 2);
                entity.Property(e => e.PreviousBalance).HasPrecision(18, 2);
                entity.Property(e => e.CurrentBalance).HasPrecision(18, 2);

                // Relationships
                entity.HasOne(e => e.Customer)
                      .WithMany(c => c.Invoices)
                      .HasForeignKey(e => e.CustomerId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Truck)
                      .WithMany(t => t.Invoices)
                      .HasForeignKey(e => e.TruckId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // Payment configuration
            modelBuilder.Entity<Payment>(entity =>
            {
                entity.HasKey(e => e.PaymentId);
                entity.Property(e => e.Amount).HasPrecision(18, 2);

                // Relationships
                entity.HasOne(e => e.Customer)
                      .WithMany(c => c.Payments)
                      .HasForeignKey(e => e.CustomerId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Invoice)
                      .WithMany(i => i.Payments)
                      .HasForeignKey(e => e.InvoiceId)
                      .OnDelete(DeleteBehavior.SetNull);
            });

            // Seed data
            SeedInitialData(modelBuilder);
        }

        private void SeedInitialData(ModelBuilder modelBuilder)
        {
            // Seed some initial trucks
            modelBuilder.Entity<Truck>().HasData(
                new Truck { TruckId = 1, TruckNumber = "T001", DriverName = "Driver 1", CreatedDate = DateTime.Now },
                new Truck { TruckId = 2, TruckNumber = "T002", DriverName = "Driver 2", CreatedDate = DateTime.Now }
            );

            // Seed a test customer
            modelBuilder.Entity<Customer>().HasData(
                new Customer { CustomerId = 1, CustomerName = "Test Customer", TotalDebt = 0, CreatedDate = DateTime.Now }
            );
        }
    }
}