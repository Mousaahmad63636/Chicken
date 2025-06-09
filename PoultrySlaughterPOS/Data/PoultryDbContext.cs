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
        public DbSet<TruckLoad> TruckLoads { get; set; }
        public DbSet<DailyReconciliation> DailyReconciliations { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }

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

            // TruckLoad configuration
            modelBuilder.Entity<TruckLoad>(entity =>
            {
                entity.HasKey(e => e.LoadId);
                entity.Property(e => e.TotalWeight).HasPrecision(10, 2);

                // Relationships
                entity.HasOne(e => e.Truck)
                      .WithMany(t => t.TruckLoads)
                      .HasForeignKey(e => e.TruckId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // DailyReconciliation configuration
            modelBuilder.Entity<DailyReconciliation>(entity =>
            {
                entity.HasKey(e => e.ReconciliationId);
                entity.Property(e => e.LoadWeight).HasPrecision(10, 2);
                entity.Property(e => e.SoldWeight).HasPrecision(10, 2);
                entity.Property(e => e.WastageWeight).HasPrecision(10, 2);
                entity.Property(e => e.WastagePercentage).HasPrecision(5, 2);

                // Relationships
                entity.HasOne(e => e.Truck)
                      .WithMany(t => t.DailyReconciliations)
                      .HasForeignKey(e => e.TruckId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // AuditLog configuration
            modelBuilder.Entity<AuditLog>(entity =>
            {
                entity.HasKey(e => e.AuditId);
                entity.HasIndex(e => e.TableName);
                entity.HasIndex(e => e.CreatedDate);
            });
        }
    }
}