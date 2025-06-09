using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PoultrySlaughterPOS.Data;

namespace PoultrySlaughterPOS.Services
{
    public interface IDatabaseService
    {
        Task<bool> InitializeDatabaseAsync();
        Task<bool> TestConnectionAsync();
        Task<string> GetConnectionStatusAsync();
    }

    public class DatabaseService : IDatabaseService
    {
        private readonly PoultryDbContext _context;
        private readonly ILogger<DatabaseService> _logger;

        public DatabaseService(PoultryDbContext context, ILogger<DatabaseService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<bool> InitializeDatabaseAsync()
        {
            try
            {
                _logger.LogInformation("Starting database initialization...");

                // Create database if it doesn't exist
                var created = await _context.Database.EnsureCreatedAsync();

                if (created)
                {
                    _logger.LogInformation("Database created successfully.");
                }
                else
                {
                    _logger.LogInformation("Database already exists.");
                }

                // Test the connection
                var canConnect = await _context.Database.CanConnectAsync();

                if (canConnect)
                {
                    _logger.LogInformation("Database connection successful.");

                    // Log table counts
                    var customerCount = await _context.Customers.CountAsync();
                    var truckCount = await _context.Trucks.CountAsync();
                    var invoiceCount = await _context.Invoices.CountAsync();
                    var paymentCount = await _context.Payments.CountAsync();

                    _logger.LogInformation("Database statistics - Customers: {CustomerCount}, Trucks: {TruckCount}, Invoices: {InvoiceCount}, Payments: {PaymentCount}",
                        customerCount, truckCount, invoiceCount, paymentCount);

                    return true;
                }
                else
                {
                    _logger.LogError("Cannot connect to database.");
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Database initialization failed: {Message}", ex.Message);
                return false;
            }
        }

        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                _logger.LogInformation("Testing database connection...");
                var canConnect = await _context.Database.CanConnectAsync();

                if (canConnect)
                {
                    _logger.LogInformation("Database connection test successful.");
                    return true;
                }
                else
                {
                    _logger.LogWarning("Database connection test failed.");
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Database connection test error: {Message}", ex.Message);
                return false;
            }
        }

        public async Task<string> GetConnectionStatusAsync()
        {
            try
            {
                var connectionString = _context.Database.GetConnectionString();
                var canConnect = await _context.Database.CanConnectAsync();

                if (canConnect)
                {
                    var customerCount = await _context.Customers.CountAsync();
                    return $"Connected to: {connectionString}\nStatus: Connected\nCustomers: {customerCount}";
                }
                else
                {
                    return $"Connection String: {connectionString}\nStatus: Cannot Connect";
                }
            }
            catch (Exception ex)
            {
                return $"Status: Error - {ex.Message}";
            }
        }
    }
}