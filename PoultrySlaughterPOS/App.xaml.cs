using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PoultrySlaughterPOS.Data;
using PoultrySlaughterPOS.Services;
using PoultrySlaughterPOS.Services.Repositories;
using PoultrySlaughterPOS.Services.Repositories.Implementations;
using PoultrySlaughterPOS.Repositories;
using PoultrySlaughterPOS.ViewModels;
using PoultrySlaughterPOS.Extensions;
using System.IO;
using System.Windows;
using PoultrySlaughterPOS.Services.Repositories.Interfaces;

namespace PoultrySlaughterPOS
{
    public partial class App : Application
    {
        private IHost? _host;

        protected override async void OnStartup(StartupEventArgs e)
        {
            try
            {
                // Create host with dependency injection
                _host = CreateHost();

                // Configure application extensions
                this.ConfigureServiceProvider(_host);

                // Start the host
                await _host.StartAsync();

                // Initialize database
                await InitializeDatabaseAsync();

                // Show main window
                var mainWindow = _host.Services.GetRequiredService<MainWindow>();
                mainWindow.Show();

                base.OnStartup(e);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Application startup failed: {ex.Message}", "Startup Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown();
            }
        }

        protected override async void OnExit(ExitEventArgs e)
        {
            if (_host != null)
            {
                await _host.StopAsync();
                _host.Dispose();
            }
            this.DisposeServiceProvider();
            base.OnExit(e);
        }

        private IHost CreateHost()
        {
            return Host.CreateDefaultBuilder()
                .ConfigureAppConfiguration((context, config) =>
                {
                    config.SetBasePath(Directory.GetCurrentDirectory());
                    config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                })
                .ConfigureServices((context, services) =>
                {
                    var configuration = context.Configuration;

                    // Add logging
                    services.AddLogging(builder =>
                    {
                        builder.AddConsole();
                        builder.AddDebug();
                    });

                    // Add Entity Framework - DbContextFactory for thread-safe operations
                    services.AddDbContextFactory<PoultryDbContext>(options =>
                        options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

                    // Add scoped DbContext for repositories that need direct DbContext access
                    services.AddScoped<PoultryDbContext>(provider =>
                    {
                        var factory = provider.GetRequiredService<IDbContextFactory<PoultryDbContext>>();
                        return factory.CreateDbContext();
                    });

                    // Register Repository Pattern Services
                    services.AddScoped<IUnitOfWork, UnitOfWork>();

                    // Register Individual Repositories with correct constructor patterns
                    // Repositories using PoultryDbContext directly:
                    services.AddScoped<ICustomerRepository, PoultrySlaughterPOS.Services.Repositories.CustomerRepository>();
                    services.AddScoped<ITruckLoadRepository, PoultrySlaughterPOS.Services.Repositories.TruckLoadRepository>();
                    services.AddScoped<IDailyReconciliationRepository, PoultrySlaughterPOS.Services.Repositories.DailyReconciliationRepository>();
                    services.AddScoped<IAuditLogRepository, PoultrySlaughterPOS.Services.Repositories.AuditLogRepository>();

                    // Repositories using IDbContextFactory:
                    services.AddScoped<ITruckRepository, PoultrySlaughterPOS.Repositories.TruckRepository>();
                    services.AddScoped<IInvoiceRepository, PoultrySlaughterPOS.Repositories.InvoiceRepository>();
                    services.AddScoped<IPaymentRepository, PoultrySlaughterPOS.Services.Repositories.Implementations.PaymentRepository>();

                    // Register Business Services
                    services.AddScoped<IDatabaseService, DatabaseService>();
                    services.AddScoped<IDatabaseInitializationService, DatabaseInitializationService>();
                    services.AddScoped<IErrorHandlingService, SimpleErrorHandlingService>();
                    services.AddScoped<IPOSService, POSService>();
                    services.AddScoped<ITransactionProcessingService, TransactionProcessingService>();
                    services.AddScoped<ITruckLoadingService, TruckLoadingService>();

                    // Register ViewModels - let DI container resolve dependencies
                    services.AddTransient<POSViewModel>();
                    services.AddTransient<CustomerAccountsViewModel>();
                    services.AddTransient<TruckLoadingViewModel>();
                    services.AddTransient<TransactionHistoryViewModel>();
                    services.AddTransient<DailyReconciliationViewModel>();
                    services.AddTransient<AddCustomerDialogViewModel>();
                    services.AddTransient<PaymentDialogViewModel>();

                    // Register Windows and Views
                    services.AddTransient<MainWindow>();

                    // Register additional services as needed
                    // services.AddSingleton<IDialogService, DialogService>();
                    // services.AddSingleton<IMessageBoxService, MessageBoxService>();
                })
                .Build();
        }

        private async Task InitializeDatabaseAsync()
        {
            try
            {
                var databaseService = _host!.Services.GetRequiredService<IDatabaseInitializationService>();
                var logger = _host.Services.GetRequiredService<ILogger<App>>();

                logger.LogInformation("Starting database initialization...");

                await databaseService.InitializeAsync();
                logger.LogInformation("Database initialization completed successfully.");
            }
            catch (Exception ex)
            {
                var logger = _host?.Services.GetService<ILogger<App>>();
                logger?.LogError(ex, "Database initialization failed: {Message}", ex.Message);

                MessageBox.Show($"Database initialization error: {ex.Message}", "Database Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}