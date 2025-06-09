using Microsoft.EntityFrameworkCore;
using PoultrySlaughterPOS.Models;
using System.Reflection;

namespace PoultrySlaughterPOS.Data
{
    public class PoultryDbContext : DbContext
    {
        public PoultryDbContext(DbContextOptions<PoultryDbContext> options) : base(options)
        {
        }

        // DbSets for all entities
        public DbSet<Truck> Trucks => Set<Truck>();
        public DbSet<TruckLoad> TruckLoads => Set<TruckLoad>();
        public DbSet<Customer> Customers => Set<Customer>();
        public DbSet<Invoice> Invoices => Set<Invoice>();
        public DbSet<Payment> Payments => Set<Payment>();
        public DbSet<DailyReconciliation> DailyReconciliations => Set<DailyReconciliation>();
        public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure entity relationships and constraints
            ConfigureEntities(modelBuilder);

            // No seed data - clean database start
        }

        private void ConfigureEntities(ModelBuilder modelBuilder)
        {
            // Truck Configuration
            modelBuilder.Entity<Truck>(entity =>
            {
                entity.HasKey(e => e.TruckId);
                entity.HasIndex(e => e.TruckNumber).IsUnique();
                entity.Property(e => e.TruckNumber).IsRequired().HasMaxLength(50);
                entity.Property(e => e.DriverName).IsRequired().HasMaxLength(100);
            });

            // TruckLoad Configuration
            modelBuilder.Entity<TruckLoad>(entity =>
            {
                entity.HasKey(e => e.LoadId);
                entity.HasOne(e => e.Truck)
                      .WithMany(t => t.TruckLoads)
                      .HasForeignKey(e => e.TruckId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.Property(e => e.TotalWeight).HasPrecision(10, 2);
                entity.Property(e => e.Status).HasDefaultValue("LOADED");
            });

            // Customer Configuration
            modelBuilder.Entity<Customer>(entity =>
            {
                entity.HasKey(e => e.CustomerId);
                entity.HasIndex(e => e.CustomerName);
                entity.Property(e => e.CustomerName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.PhoneNumber).HasMaxLength(20);
                entity.Property(e => e.Address).HasMaxLength(200);
                entity.Property(e => e.TotalDebt).HasPrecision(12, 2);
            });

            // Invoice Configuration
            modelBuilder.Entity<Invoice>(entity =>
            {
                entity.HasKey(e => e.InvoiceId);

                entity.HasOne(e => e.Customer)
                      .WithMany(c => c.Invoices)
                      .HasForeignKey(e => e.CustomerId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Truck)
                      .WithMany(t => t.Invoices)
                      .HasForeignKey(e => e.TruckId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.Property(e => e.GrossWeight).HasPrecision(10, 2);
                entity.Property(e => e.CagesWeight).HasPrecision(10, 2);
                entity.Property(e => e.NetWeight).HasPrecision(10, 2);
                entity.Property(e => e.UnitPrice).HasPrecision(8, 2);
                entity.Property(e => e.TotalAmount).HasPrecision(12, 2);
                entity.Property(e => e.DiscountPercentage).HasPrecision(5, 2);
                entity.Property(e => e.FinalAmount).HasPrecision(12, 2);
                entity.Property(e => e.PreviousBalance).HasPrecision(12, 2);
                entity.Property(e => e.CurrentBalance).HasPrecision(12, 2);
            });

            // Payment Configuration
            modelBuilder.Entity<Payment>(entity =>
            {
                entity.HasKey(e => e.PaymentId);

                entity.HasOne(e => e.Customer)
                      .WithMany(c => c.Payments)
                      .HasForeignKey(e => e.CustomerId)
                      .OnDelete(DeleteBehavior.SetNull);

                entity.Property(e => e.Amount).HasPrecision(12, 2);
                entity.Property(e => e.PaymentMethod).HasDefaultValue("CASH");
            });

            // DailyReconciliation Configuration
            modelBuilder.Entity<DailyReconciliation>(entity =>
            {
                entity.HasKey(e => e.ReconciliationId);

                entity.HasOne(e => e.Truck)
                      .WithMany(t => t.DailyReconciliations)
                      .HasForeignKey(e => e.TruckId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.Property(e => e.LoadWeight).HasPrecision(10, 2);
                entity.Property(e => e.SoldWeight).HasPrecision(10, 2);
                entity.Property(e => e.WastageWeight).HasPrecision(10, 2);
                entity.Property(e => e.WastagePercentage).HasPrecision(5, 2);
                entity.Property(e => e.Status).HasDefaultValue("PENDING");

                // Unique constraint for truck and date
                entity.HasIndex(e => new { e.TruckId, e.ReconciliationDate }).IsUnique();
            });

            // AuditLog Configuration
            modelBuilder.Entity<AuditLog>(entity =>
            {
                entity.HasKey(e => e.AuditId);
                entity.HasIndex(e => new { e.TableName, e.CreatedDate });
                entity.Property(e => e.TableName).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Operation).IsRequired().HasMaxLength(10);
            });
        }

        // Override SaveChanges to implement audit logging
        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            await AddAuditLogs();
            return await base.SaveChangesAsync(cancellationToken);
        }

        public override int SaveChanges()
        {
            AddAuditLogs().Wait();
            return base.SaveChanges();
        }

        private async Task AddAuditLogs()
        {
            var entries = ChangeTracker.Entries()
                .Where(e => e.Entity is not AuditLog && e.State != EntityState.Unchanged)
                .ToList();

            foreach (var entry in entries)
            {
                var auditLog = new AuditLog
                {
                    TableName = entry.Entity.GetType().Name,
                    Operation = entry.State.ToString(),
                    CreatedDate = DateTime.Now,
                    UserId = "SYSTEM" // You can modify this to get actual user
                };

                if (entry.State == EntityState.Modified)
                {
                    auditLog.OldValues = GetEntityValues(entry.OriginalValues);
                    auditLog.NewValues = GetEntityValues(entry.CurrentValues);
                }
                else if (entry.State == EntityState.Added)
                {
                    auditLog.NewValues = GetEntityValues(entry.CurrentValues);
                }
                else if (entry.State == EntityState.Deleted)
                {
                    auditLog.OldValues = GetEntityValues(entry.OriginalValues);
                }

                AuditLogs.Add(auditLog);
            }
        }

        private string GetEntityValues(Microsoft.EntityFrameworkCore.ChangeTracking.PropertyValues values)
        {
            try
            {
                var valueDict = new Dictionary<string, object?>();
                foreach (var property in values.Properties)
                {
                    valueDict[property.Name] = values[property];
                }
                return System.Text.Json.JsonSerializer.Serialize(valueDict);
            }
            catch (Exception ex)
            {
                return $"Serialization failed: {ex.Message}";
            }
        }

        public DbContextOptions<PoultryDbContext> GetConfiguration()
        {
            return (DbContextOptions<PoultryDbContext>)this.GetType()
                .GetProperty("ContextOptions")
                .GetValue(this);
        }
    }
}