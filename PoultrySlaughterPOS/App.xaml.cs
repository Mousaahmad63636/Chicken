using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PoultrySlaughterPOS.Data;
using PoultrySlaughterPOS.Services;
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
                // Create host with dependency injection
                _host = CreateHost();

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

                    // Add Entity Framework
                    services.AddDbContext<PoultryDbContext>(options =>
                        options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

                    // Add services
                    services.AddScoped<IDatabaseService, DatabaseService>();

                    // Add windows
                    services.AddTransient<MainWindow>();
                })
                .Build();
        }

        private async Task InitializeDatabaseAsync()
        {
            try
            {
                var databaseService = _host!.Services.GetRequiredService<IDatabaseService>();
                var logger = _host.Services.GetRequiredService<ILogger<App>>();

                logger.LogInformation("Starting database initialization...");

                var success = await databaseService.InitializeDatabaseAsync();

                if (success)
                {
                    logger.LogInformation("Database initialization completed successfully.");
                }
                else
                {
                    logger.LogError("Database initialization failed.");
                    MessageBox.Show("Database initialization failed. Please check your SQL Server Express installation.",
                        "Database Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Database initialization error: {ex.Message}", "Database Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}