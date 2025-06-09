using Microsoft.EntityFrameworkCore;
using PoultrySlaughterPOS.Data;
using PoultrySlaughterPOS.Models;

namespace PoultrySlaughterPOS.Services
{
    public static class DatabaseInitializer
    {
        public static void Initialize(PoultryDbContext context)
        {
            try
            {
                // Ensure database is created
                context.Database.EnsureCreated();

                // Apply any pending migrations
                if (context.Database.GetPendingMigrations().Any())
                {
                    context.Database.Migrate();
                }

                // Save changes if any were made
                context.SaveChanges();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Database initialization failed: {ex.Message}", ex);
            }
        }

        public static void SeedDefaultData(PoultryDbContext context)
        {
            try
            {
                // Check if we already have data
                if (context.Trucks.Any() || context.Customers.Any())
                {
                    return; // Database has been seeded
                }

                // Only seed if specifically needed for testing/demo
                // This method can be called manually when needed

                context.SaveChanges();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Data seeding failed: {ex.Message}", ex);
            }
        }
    }
}