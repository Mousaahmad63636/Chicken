using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PoultrySlaughterPOS.Data;
using PoultrySlaughterPOS.Extensions;
using PoultrySlaughterPOS.Repositories;
using PoultrySlaughterPOS.Services;
using PoultrySlaughterPOS.Services.Repositories;
using PoultrySlaughterPOS.Services.Repositories.Implementations;
using PoultrySlaughterPOS.ViewModels;
using PoultrySlaughterPOS.Views;
using System;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Markup;

namespace PoultrySlaughterPOS
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IConfiguration _configuration;

        public IServiceProvider ServiceProvider => _serviceProvider;

        public App()
        {
            // Set a consistent culture for the application
            System.Threading.Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            System.Threading.Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;

            _configuration = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            var services = new ServiceCollection();
            ConfigureServices(services);
            _serviceProvider = services.BuildServiceProvider();

            // Configure the service provider using existing extension method
            this.ConfigureServiceProvider(_serviceProvider);
        }

        private void ConfigureServices(IServiceCollection services)
        {
            // Core Services
            services.AddSingleton<IConfiguration>(_configuration);

            // Database Context - Use Scoped only, no DbContextPool to avoid DI issues
            services.AddDbContext<PoultryDbContext>(options =>
                options.UseSqlServer(_configuration.GetConnectionString("DefaultConnection"))
                .EnableSensitiveDataLogging()
                .EnableDetailedErrors(), ServiceLifetime.Scoped);

            // DbContext Factory - Use Scoped instead of Singleton to avoid DI conflicts
            services.AddScoped<IDbContextFactory<PoultryDbContext>>(provider =>
            {
                var connectionString = _configuration.GetConnectionString("DefaultConnection");
                var options = new DbContextOptionsBuilder<PoultryDbContext>()
                    .UseSqlServer(connectionString)
                    .EnableSensitiveDataLogging()
                    .EnableDetailedErrors()
                    .Options;
                return new RuntimePoultryDbContextFactory(options);
            });

            // Unit of Work - Register with proper dependencies
            services.AddScoped<IUnitOfWork>(provider =>
            {
                var context = provider.GetRequiredService<PoultryDbContext>();
                var contextFactory = provider.GetRequiredService<IDbContextFactory<PoultryDbContext>>();
                var loggerFactory = provider.GetRequiredService<ILoggerFactory>();
                return new UnitOfWork(context, contextFactory, loggerFactory);
            });

            // Business Services (only include existing services)
            services.AddScoped<IErrorHandlingService, ErrorHandlingService>();
            services.AddScoped<ITransactionProcessingService, TransactionProcessingService>();
            services.AddScoped<ITruckLoadingService, TruckLoadingService>();
            services.AddScoped<IPOSService, POSService>();

            // View Models (only include existing ViewModels)
            services.AddScoped<TruckLoadingViewModel>();
            services.AddScoped<POSViewModel>();
            services.AddScoped<CustomerAccountsViewModel>();
            services.AddScoped<TransactionHistoryViewModel>();

            // Views (register according to their actual constructor patterns)
            services.AddTransient<MainWindow>();

            // TruckLoadingView expects (TruckLoadingViewModel, ILogger<TruckLoadingView>)
            services.AddTransient<TruckLoadingView>(provider =>
                new TruckLoadingView(
                    provider.GetRequiredService<TruckLoadingViewModel>(),
                    provider.GetRequiredService<ILogger<TruckLoadingView>>()));

            // Other views use parameterless constructors and SetViewModel pattern
            services.AddTransient<POSView>();
            services.AddTransient<CustomerAccountsView>();
            services.AddTransient<TransactionHistoryView>();

            // Logging
            services.AddLogging(builder =>
            {
                builder.AddConsole();
                builder.AddDebug();
                builder.SetMinimumLevel(LogLevel.Information);
            });
        }

        protected override async void OnStartup(StartupEventArgs e)
        {
            // CRITICAL: Call InitializeComponent() first
            InitializeComponent();

            // Set default culture for all UI threads
            FrameworkElement.LanguageProperty.OverrideMetadata(
                typeof(FrameworkElement),
                new FrameworkPropertyMetadata(
                    XmlLanguage.GetLanguage(CultureInfo.InvariantCulture.IetfLanguageTag)));

            base.OnStartup(e);

            try
            {
                // Create log directory
                var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
                Directory.CreateDirectory(logPath);
                File.WriteAllText(Path.Combine(logPath, "startup.log"), $"Application starting at {DateTime.Now}...");

                // Initialize database in a separate thread to keep UI responsive
                await Task.Run(async () =>
                {
                    try
                    {
                        using var scope = this.CreateScope();
                        var context = scope.ServiceProvider.GetRequiredService<PoultryDbContext>();

                        // Ensure database exists and is up to date
                        await context.Database.EnsureCreatedAsync();

                        // Initialize database
                        DatabaseInitializer.Initialize(context);

                        File.AppendAllText(Path.Combine(logPath, "startup.log"),
                            $"\nDatabase initialized successfully at {DateTime.Now}");
                    }
                    catch (Exception dbEx)
                    {
                        File.AppendAllText(Path.Combine(logPath, "startup.log"),
                            $"\nDatabase error at {DateTime.Now}: {dbEx.Message}");

                        // Use Dispatcher to show message box from background thread
                        Dispatcher.Invoke(() =>
                        {
                            MessageBox.Show(
                                $"Database initialization error: {dbEx.Message}\n\nPlease ensure SQL Server is installed and accessible with the provided credentials.",
                                "Database Error",
                                MessageBoxButton.OK,
                                MessageBoxImage.Error);
                            Shutdown();
                        });
                        return;
                    }
                });

                // Show the main window
                Dispatcher.Invoke(() =>
                {
                    try
                    {
                        var mainWindow = this.GetRequiredService<MainWindow>();
                        mainWindow.Show();

                        File.AppendAllText(Path.Combine(logPath, "startup.log"),
                            $"\nMain window displayed successfully at {DateTime.Now}");
                    }
                    catch (Exception mainEx)
                    {
                        File.AppendAllText(Path.Combine(logPath, "startup.log"),
                            $"\nMain window error at {DateTime.Now}: {mainEx.Message}");
                        throw;
                    }
                });
            }
            catch (Exception ex)
            {
                var errorMessage = $"An error occurred while starting the application: {ex.Message}\n\n" +
                                  "Please ensure:\n" +
                                  "1. SQL Server is installed and accessible\n" +
                                  "2. .NET 8.0 Desktop Runtime is installed\n" +
                                  "3. You have necessary permissions to access the application folder";

                MessageBox.Show(errorMessage, "Startup Error", MessageBoxButton.OK, MessageBoxImage.Error);

                try
                {
                    var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
                    File.AppendAllText(Path.Combine(logPath, "error.log"),
                        $"\n[{DateTime.Now}] Fatal startup error:\n{ex}\n");
                }
                catch
                {
                    // If we can't log the error, just shutdown
                }

                Shutdown();
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            base.OnExit(e);

            // Use existing extension method for proper disposal
            this.DisposeServiceProvider();
        }
    }
}