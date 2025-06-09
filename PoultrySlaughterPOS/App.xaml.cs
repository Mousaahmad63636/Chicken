using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PoultrySlaughterPOS.Controls;
using PoultrySlaughterPOS.Data;
using PoultrySlaughterPOS.Extensions;
using PoultrySlaughterPOS.Repositories;
using PoultrySlaughterPOS.Services;
using PoultrySlaughterPOS.Services.Repositories;
using PoultrySlaughterPOS.Services.Repositories.Implementations;
using PoultrySlaughterPOS.ViewModels;
using PoultrySlaughterPOS.Views;
using PoultrySlaughterPOS.Views.Dialogs;
using PoultrySlaughterPOS.Converters;
using Serilog;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;

namespace PoultrySlaughterPOS
{
    public partial class App : Application
    {
        #region Private Fields

        private ServiceProvider? _serviceProvider;
        private readonly object _lockObject = new object();

        #endregion

        #region Application Lifecycle

        protected override async void OnStartup(StartupEventArgs e)
        {
            try
            {
                ConfigureLogging();

                Log.Information("=== Poultry Slaughter POS Application Starting ===");
                Log.Information("Startup initiated with complete customer management integration");

                var configuration = BuildConfiguration();

                _serviceProvider = BuildServiceProvider(configuration);

                this.ConfigureServiceProvider(_serviceProvider);

                Log.Information("Service provider configured successfully");

                await ShowMainWindowAsync();

                Log.Information("=== Poultry Slaughter POS Application Started Successfully ===");
                base.OnStartup(e);
            }
            catch (Exception ex)
            {
                HandleCriticalStartupError(ex);
            }
        }

        protected override async void OnExit(ExitEventArgs e)
        {
            try
            {
                Log.Information("=== Poultry Slaughter POS Application Shutdown Initiated ===");

                this.DisposeServiceProvider();

                base.OnExit(e);
                Log.Information("=== Poultry Slaughter POS Application Shutdown Completed ===");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error occurred during application shutdown");
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        #endregion

        #region Configuration Methods

        private static void ConfigureLogging()
        {
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console(
                    outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}")
                .WriteTo.File(
                    path: "logs/pos-application-.txt",
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 30,
                    outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} {Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}")
                .WriteTo.File(
                    path: "logs/errors/error-log-.txt",
                    rollingInterval: RollingInterval.Day,
                    restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Error,
                    retainedFileCountLimit: 90,
                    outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} {Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}")
                .WriteTo.File(
                    path: "logs/debug/debug-log-.txt",
                    rollingInterval: RollingInterval.Day,
                    restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Debug,
                    retainedFileCountLimit: 7,
                    outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} {Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}")
                .MinimumLevel.Information()
                .MinimumLevel.Override("Microsoft.EntityFrameworkCore", Serilog.Events.LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", Serilog.Events.LogEventLevel.Warning)
                .Enrich.WithThreadId()
                .Enrich.WithMachineName()
                .Enrich.WithEnvironmentName()
                .CreateLogger();

            Log.Information("Serilog logging infrastructure configured successfully");
        }

        private static IConfiguration BuildConfiguration()
        {
            var environment = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Production";

            return new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables("POULTRY_POS_")
                .Build();
        }

        private static ServiceProvider BuildServiceProvider(IConfiguration configuration)
        {
            var services = new ServiceCollection();

            ConfigureAllServices(services, configuration);

            var serviceProvider = services.BuildServiceProvider();
            Log.Information("Service registration completed - {ServiceCount} services configured", services.Count);

            return serviceProvider;
        }

        #endregion

        #region Service Configuration

        private static void ConfigureAllServices(IServiceCollection services, IConfiguration configuration)
        {
            services.AddSingleton(configuration);

            ConfigureDatabaseServices(services, configuration);

            ConfigureRepositoryServices(services);

            ConfigureBusinessServices(services);

            ConfigurePresentationServices(services);

            ConfigureCrossCuttingServices(services, configuration);

            Log.Information("Service configuration completed successfully with {ServiceCount} services registered (complete customer management integration)",
                           services.Count);
        }

        private static void ConfigureDatabaseServices(IServiceCollection services, IConfiguration configuration)
        {
            var useTransactionalOperations = configuration.GetValue<bool>("Database:UseExplicitTransactions", true);
            var enableRetryOnFailure = configuration.GetValue<bool>("Database:EnableRetryOnFailure", false);
            var connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("DefaultConnection connection string is required");

            Log.Information("Database configuration - UseExplicitTransactions: {UseTransactions}, EnableRetryOnFailure: {EnableRetry}",
                useTransactionalOperations, enableRetryOnFailure);

            services.AddDbContext<PoultryDbContext>(options =>
            {
                ConfigureDbContextOptions(options, connectionString, enableRetryOnFailure, useTransactionalOperations);
            }, ServiceLifetime.Scoped);

            services.AddSingleton<IDbContextFactory<PoultryDbContext>>(serviceProvider =>
            {
                return new PoultryDbContextFactory(connectionString, enableRetryOnFailure, useTransactionalOperations);
            });

            services.AddTransient<IDatabaseInitializationService, DatabaseInitializationService>();
        }

        private static void ConfigureRepositoryServices(IServiceCollection services)
        {
            services.AddScoped<ITruckRepository, TruckRepository>();
            services.AddScoped<ICustomerRepository, CustomerRepository>();
            services.AddScoped<IInvoiceRepository, InvoiceRepository>();
            services.AddScoped<IPaymentRepository, PaymentRepository>();
            services.AddScoped<ITruckLoadRepository, TruckLoadRepository>();
            services.AddScoped<IDailyReconciliationRepository, DailyReconciliationRepository>();
            services.AddScoped<IAuditLogRepository, AuditLogRepository>();
            services.AddScoped<ITransactionProcessingService, TransactionProcessingService>();

            services.AddScoped<IUnitOfWork>(serviceProvider =>
            {
                var context = serviceProvider.GetRequiredService<PoultryDbContext>();
                var contextFactory = serviceProvider.GetRequiredService<IDbContextFactory<PoultryDbContext>>();
                var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();

                return new UnitOfWork(context, contextFactory, loggerFactory);
            });

            Log.Debug("Repository layer configured with comprehensive data access patterns");
        }

        private static void ConfigureBusinessServices(IServiceCollection services)
        {
            services.AddScoped<IErrorHandlingService, ErrorHandlingService>();
            services.AddScoped<ITruckLoadingService, TruckLoadingService>();
            services.AddScoped<IPOSService, POSService>();

            Log.Debug("Business logic layer configured with comprehensive service patterns including Export and Printing services");
        }

        private static void ConfigurePresentationServices(IServiceCollection services)
        {
            services.AddTransient<TruckLoadingViewModel>();
            services.AddTransient<POSViewModel>();
            services.AddTransient<CustomerAccountsViewModel>();
            services.AddTransient<TransactionHistoryViewModel>();

            services.AddTransient<AddCustomerDialogViewModel>();

            services.AddTransient<TruckLoadingView>();
            services.AddTransient<POSView>();
            services.AddTransient<CustomerAccountsView>();
            services.AddTransient<TransactionHistoryView>();

            services.AddTransient<AddCustomerDialog>();

            services.AddTransient<CustomerListControl>();
            services.AddTransient<CustomerDetailsControl>();
            services.AddTransient<AccountStatementControl>();
            services.AddTransient<PaymentHistoryControl>();
            services.AddTransient<DebtManagementControl>();

            services.AddSingleton<InverseBooleanConverter>();

            services.AddTransient<MainWindow>();

            Log.Debug("Presentation layer configured with complete customer management integration");
        }

        private static void ConfigureCrossCuttingServices(IServiceCollection services, IConfiguration configuration)
        {
            services.AddLogging(builder =>
            {
                builder.AddSerilog();
                builder.SetMinimumLevel(LogLevel.Information);
                builder.AddFilter("Microsoft.EntityFrameworkCore.Database.Command", LogLevel.Warning);
                builder.AddFilter("Microsoft.EntityFrameworkCore.Infrastructure", LogLevel.Warning);
                builder.AddFilter("Microsoft.EntityFrameworkCore.Model.Validation", LogLevel.Warning);
            });

            services.AddMemoryCache(options =>
            {
                options.SizeLimit = 1000;
                options.CompactionPercentage = 0.25;
            });

            services.Configure<ApplicationSettings>(configuration.GetSection("Application"));
            services.Configure<DatabaseSettings>(configuration.GetSection("Database"));

            services.AddHttpClient("DefaultClient", client =>
            {
                client.Timeout = TimeSpan.FromSeconds(30);
            });

            Log.Debug("Cross-cutting concerns configured successfully");
        }

        private static void ConfigureDbContextOptions(DbContextOptionsBuilder options, string connectionString,
            bool enableRetryOnFailure, bool useTransactionalOperations)
        {
            options.UseSqlServer(connectionString, sqlOptions =>
            {
                if (enableRetryOnFailure && !useTransactionalOperations)
                {
                    sqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 3,
                        maxRetryDelay: TimeSpan.FromSeconds(5),
                        errorNumbersToAdd: new[] { -2, 1205, 1222 });

                    Log.Information("SQL Server retry strategy enabled (explicit transactions disabled)");
                }
                else if (useTransactionalOperations)
                {
                    Log.Information("SQL Server retry strategy disabled to support explicit transactions");
                }

                sqlOptions.CommandTimeout(120);
                sqlOptions.MigrationsAssembly("PoultrySlaughterPOS");
            });

            options.EnableSensitiveDataLogging(false);
            options.EnableServiceProviderCaching(true);
            options.EnableDetailedErrors(true);

            options.UseQueryTrackingBehavior(useTransactionalOperations ?
                QueryTrackingBehavior.TrackAll : QueryTrackingBehavior.NoTracking);

            options.LogTo(message => Log.Debug("EF Core: {Message}", message), LogLevel.Debug);
        }

        #endregion

        #region Initialization Methods

        private async Task InitializeDatabaseAsync()
        {
            try
            {
                using var scope = this.CreateScope();
                var dbInitService = scope.ServiceProvider.GetRequiredService<IDatabaseInitializationService>();

                Log.Information("Database initialization started");
                await dbInitService.InitializeAsync();
                Log.Information("Database initialization completed successfully");
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Critical failure during database initialization");
                throw new ApplicationException("Database initialization failed. Please check the connection string and ensure SQL Server is running.", ex);
            }
        }

        private async Task ShowMainWindowAsync()
        {
            try
            {
                var mainWindow = this.GetRequiredService<MainWindow>();

                MainWindow = mainWindow;

                mainWindow.Show();

                Log.Information("Main window created and displayed successfully");
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Critical failure creating main window");
                throw new ApplicationException("Failed to create main application window", ex);
            }
        }

        #endregion

        #region Error Handling

        private void HandleCriticalStartupError(Exception ex)
        {
            Log.Fatal(ex, "=== CRITICAL FAILURE DURING APPLICATION STARTUP ===");

            var errorMessage = ex switch
            {
                InvalidOperationException => "خطأ في إعدادات التطبيق أو قاعدة البيانات",
                UnauthorizedAccessException => "ليس لديك صلاحية لتشغيل التطبيق",
                FileNotFoundException => "ملفات التطبيق مفقودة أو تالفة",
                _ => "فشل حرج في تشغيل التطبيق"
            };

            MessageBox.Show(
                $"{errorMessage}\n\nتفاصيل الخطأ:\n{ex.Message}\n\nيرجى التواصل مع الدعم الفني.",
                "خطأ حرج في التطبيق",
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            try
            {
                _serviceProvider?.Dispose();
            }
            catch (Exception cleanupEx)
            {
                Log.Error(cleanupEx, "Error during emergency cleanup");
            }

            Environment.Exit(1);
        }

        #endregion
    }

    #region Supporting Classes

    public class PoultryDbContextFactory : IDbContextFactory<PoultryDbContext>
    {
        private readonly string _connectionString;
        private readonly DbContextOptions<PoultryDbContext> _options;
        private readonly bool _enableRetryOnFailure;
        private readonly bool _useTransactionalOperations;

        public PoultryDbContextFactory(string connectionString, bool enableRetryOnFailure = false, bool useTransactionalOperations = true)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
            _enableRetryOnFailure = enableRetryOnFailure;
            _useTransactionalOperations = useTransactionalOperations;
            _options = CreateDbContextOptions();
        }

        private DbContextOptions<PoultryDbContext> CreateDbContextOptions()
        {
            var optionsBuilder = new DbContextOptionsBuilder<PoultryDbContext>();

            optionsBuilder.UseSqlServer(_connectionString, sqlOptions =>
            {
                if (_enableRetryOnFailure && !_useTransactionalOperations)
                {
                    sqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 3,
                        maxRetryDelay: TimeSpan.FromSeconds(5),
                        errorNumbersToAdd: new[] { -2, 1205, 1222 });
                }

                sqlOptions.CommandTimeout(120);
                sqlOptions.MigrationsAssembly("PoultrySlaughterPOS");
            });

            optionsBuilder.EnableSensitiveDataLogging(false);
            optionsBuilder.EnableServiceProviderCaching(false);
            optionsBuilder.EnableDetailedErrors(true);

            optionsBuilder.UseQueryTrackingBehavior(_useTransactionalOperations ?
                QueryTrackingBehavior.TrackAll : QueryTrackingBehavior.NoTracking);

            return optionsBuilder.Options;
        }

        public PoultryDbContext CreateDbContext()
        {
            return new PoultryDbContext(_options);
        }

        public Task<PoultryDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(CreateDbContext());
        }
    }

    public class ApplicationSettings
    {
        public string Name { get; set; } = "Poultry Slaughter POS";
        public string Version { get; set; } = "1.0.0";
        public int MaxRetryAttempts { get; set; } = 3;
        public int CommandTimeoutSeconds { get; set; } = 120;
        public bool EnableDetailedLogging { get; set; } = false;
        public string Theme { get; set; } = "Light";
        public string Language { get; set; } = "ar-SA";
        public bool EnablePerformanceCounters { get; set; } = false;
        public int CacheExpirationMinutes { get; set; } = 30;
    }

    public class DatabaseSettings
    {
        public bool UseExplicitTransactions { get; set; } = true;
        public bool EnableRetryOnFailure { get; set; } = false;
        public int CommandTimeoutSeconds { get; set; } = 120;
        public int MaxRetryCount { get; set; } = 3;
        public int MaxRetryDelaySeconds { get; set; } = 5;
        public bool EnableDetailedErrors { get; set; } = true;
        public bool EnableSensitiveDataLogging { get; set; } = false;
    }

    #endregion
}