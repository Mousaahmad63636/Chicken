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

namespace PoultrySlaughterPOS
{
    public partial class App : Application
    {
        private IHost? _host;

        protected override async void OnStartup(StartupEventArgs e)
        {
            try
            {
                _host = CreateHost();
                this.ConfigureServiceProvider(_host);
                await _host.StartAsync();
                await InitializeDatabaseAsync();
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

                    services.AddLogging(builder =>
                    {
                        builder.AddConsole();
                        builder.AddDebug();
                    });

                    services.AddDbContextFactory<PoultryDbContext>(options =>
                        options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

                    services.AddScoped<IUnitOfWork, UnitOfWork>();

                    services.AddScoped<ICustomerRepository, PoultrySlaughterPOS.Services.Repositories.CustomerRepository>();
                    services.AddScoped<ITruckLoadRepository, PoultrySlaughterPOS.Services.Repositories.TruckLoadRepository>();
                    services.AddScoped<IDailyReconciliationRepository, PoultrySlaughterPOS.Services.Repositories.DailyReconciliationRepository>();
                    services.AddScoped<IAuditLogRepository, PoultrySlaughterPOS.Services.Repositories.AuditLogRepository>();
                    services.AddScoped<ITruckRepository, PoultrySlaughterPOS.Repositories.TruckRepository>();
                    services.AddScoped<IInvoiceRepository, PoultrySlaughterPOS.Repositories.InvoiceRepository>();
                    services.AddScoped<IPaymentRepository, PoultrySlaughterPOS.Services.Repositories.Implementations.PaymentRepository>();

                    services.AddScoped<IDatabaseService, DatabaseService>();
                    services.AddScoped<IDatabaseInitializationService, DatabaseInitializationService>();
                    services.AddScoped<IErrorHandlingService, ErrorHandlingService>();
                    services.AddScoped<IPOSService, POSService>();
                    services.AddScoped<ITransactionProcessingService, TransactionProcessingService>();
                    services.AddScoped<ITruckLoadingService, TruckLoadingService>();

                    // UPDATED: Added TruckManagementViewModel registration
                    services.AddTransient<TruckManagementViewModel>();

                    // UPDATED: Added TruckManagementView registration  
                    services.AddTransient<PoultrySlaughterPOS.Views.TruckManagementView>();

                    services.AddTransient<POSViewModel>();
                    services.AddTransient<CustomerAccountsViewModel>();
                    services.AddTransient<TruckLoadingViewModel>();
                    services.AddTransient<TransactionHistoryViewModel>();
                    services.AddTransient<AddCustomerDialogViewModel>();
                    services.AddTransient<PaymentDialogViewModel>();

                    services.AddTransient<MainWindow>();
                    services.AddTransient<PoultrySlaughterPOS.Views.TruckLoadingView>();
                    services.AddTransient<PoultrySlaughterPOS.Views.POSView>();
                    services.AddTransient<PoultrySlaughterPOS.Views.CustomerAccountsView>();
                    services.AddTransient<PoultrySlaughterPOS.Views.TransactionHistoryView>();
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