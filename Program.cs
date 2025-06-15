using System;
using System.Windows.Forms;
using System.Threading;
using System.Globalization;
using System.IO;
using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using PricisApp.Interfaces;
using PricisApp.Repositories;
using PricisApp.Services;
using PricisApp.Models;
using FluentMigrator.Runner;
using Microsoft.Extensions.Logging;
using Serilog;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using MaterialSkin;
using MaterialSkin.Controls;
using PricisApp.ViewModels;
using Sentry;
using Microsoft.AppCenter;
using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;
using PricisApp.Core.Entities;
using PricisApp.UI;
using System.Runtime.Versioning;

namespace PricisApp;

internal static class Program
{
    private static ServiceProvider? _serviceProvider;
    private static Serilog.ILogger? _serilogLogger;
    private static IConfiguration? _configuration;
    private static AppSettings? _appSettings;

    public static AppSettings AppSettings => _appSettings ?? throw new InvalidOperationException("Application settings not initialized");
    public static IConfiguration Configuration => _configuration ?? throw new InvalidOperationException("Configuration not initialized");

    [STAThread]
    static void Main(string[] args)
    {
        // Add startup logging
        StartupDebug.LogStartup("Application starting");
        
        // Check if running in console mode
        if (args.Length > 0 && args[0] == "--console")
        {
            StartupDebug.LogStartup("Running in console mode");
            RunConsoleMode();
            return;
        }
        
        // Check if running in diagnostic mode
        if (args.Length > 0 && args[0] == "--diagnose")
        {
            StartupDebug.LogStartup("Running in diagnostic mode");
            RunDiagnosticModeAsync().GetAwaiter().GetResult();
            return;
        }
        
        try
        {
            StartupDebug.LogStartup("PricisApp starting normal mode");
            Console.WriteLine("PricisApp starting...");
            
            // Load configuration first
            _configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile("appsettings.user.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables() // Use this instead of AddInMemoryCollection
                .Build();
            
            Console.WriteLine("Configuration loaded");

            // Bind configuration to settings object
            _appSettings = new AppSettings();
            _configuration.Bind(_appSettings);
            
            Console.WriteLine("Settings bound to object");
            
            // Ensure Logs directory exists
            try
            {
                Directory.CreateDirectory("Logs");
                Console.WriteLine("Logs directory created");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating Logs directory: {ex.Message}");
                // Continue execution even if we can't create the logs directory
            }

            // Configure Serilog from configuration
            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(_configuration)
                .CreateLogger();
            
            Console.WriteLine("Serilog configured");
            
            // Initialize Sentry if DSN is configured
            if (!string.IsNullOrEmpty(_appSettings.Sentry?.Dsn))
            {
                SentrySdk.Init(options =>
                {
                    options.Dsn = _appSettings.Sentry.Dsn;
                    options.Debug = _appSettings.Sentry.Debug;
                    options.SendDefaultPii = _appSettings.Sentry.SendDefaultPii;
                    options.AttachStacktrace = _appSettings.Sentry.AttachStacktrace;
                    options.Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";
                    
                    // Add null check for MainModule to prevent null reference exceptions
                    var mainModule = Process.GetCurrentProcess().MainModule;
                    options.Release = mainModule != null 
                        ? $"pricisapp@{FileVersionInfo.GetVersionInfo(mainModule.FileName).ProductVersion}" 
                        : "pricisapp@1.0.0";
                });
                
                Console.WriteLine("Sentry crash reporting initialized");
            }
            
            // Initialize AppCenter if SecretKey is configured
            if (!string.IsNullOrEmpty(_appSettings.AppCenter?.SecretKey))
            {
                var services = new List<Type>();
                
                if (_appSettings.AppCenter.EnableAnalytics)
                    services.Add(typeof(Analytics));
                    
                if (_appSettings.AppCenter.EnableCrashes)
                    services.Add(typeof(Crashes));
                    
                if (services.Count > 0)
                {
                    AppCenter.Start(_appSettings.AppCenter.SecretKey, services.ToArray());
                    Console.WriteLine("AppCenter initialized with services");
                }
            }

            // Set up global exception handlers
            SetupExceptionHandling();
            Console.WriteLine("Exception handling configured");

            // Configure services
            ConfigureServices();
            Console.WriteLine("Services configured");
            
            // Get config service to read theme
            var configService = _serviceProvider!.GetRequiredService<IConfigurationService>();
            var theme = configService.GetDefaultTheme();
            Console.WriteLine($"Theme loaded: {theme}");

            // Initialize MaterialSkinManager
            var materialSkinManager = MaterialSkinManager.Instance;
            materialSkinManager.Theme = theme.Equals("Dark", StringComparison.OrdinalIgnoreCase)
                ? MaterialSkinManager.Themes.DARK
                : MaterialSkinManager.Themes.LIGHT;
            materialSkinManager.ColorScheme = new ColorScheme(
                Primary.BlueGrey800, 
                Primary.BlueGrey900, 
                Primary.BlueGrey500, 
                Accent.LightBlue200, 
                TextShade.WHITE
            );
            Console.WriteLine("MaterialSkinManager initialized");

            // Set culture to invariant for consistent number formatting
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;

            // Fix database if needed
            try
            {
                Console.WriteLine("Checking if database needs to be fixed...");
                DatabaseFix.FixDatabaseAsync().GetAwaiter().GetResult();
                Console.WriteLine("Database fix completed successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR during database fix: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
                
                // Show error message to user
                MessageBox.Show(
                    $"There was a problem with the database: {ex.Message}\n\nThe application will continue to start, but may not function correctly.",
                    "Database Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }

            // Enable visual styles
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Set default font from configuration
            try
            {
                if (_appSettings.UI?.DefaultFont != null)
                {
                    Application.SetDefaultFont(new System.Drawing.Font(
                        _appSettings.UI.DefaultFont.Name, 
                        _appSettings.UI.DefaultFont.Size));
                    Console.WriteLine("UI settings applied");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error setting default font: {ex.Message}");
                // Continue without custom font
            }

            // Create main form with DI
            try
            {
                var mainForm = _serviceProvider!.GetRequiredService<Form1>();
                Console.WriteLine("Main form created");

                // Run the application
                Console.WriteLine("Starting application...");
                Application.Run(mainForm);
            }
            catch (Exception ex)
            {
                StartupDebug.LogStartup($"ERROR: Error creating or running main form: {ex.Message}");
                StartupDebug.LogStartup($"Stack trace: {ex.StackTrace}");
                Console.WriteLine($"Error creating or running main form: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
                try
                {
                    MessageBox.Show(
                        $"Error starting application: {ex.Message}\n\n{ex.StackTrace}",
                        "Application Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
                catch (Exception msgEx)
                {
                    StartupDebug.LogStartup($"ERROR: Failed to show error message box: {msgEx.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            StartupDebug.LogStartup($"FATAL ERROR: {ex.Message}");
            StartupDebug.LogStartup($"Stack trace: {ex.StackTrace}");
            Console.WriteLine($"FATAL ERROR: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
            try
            {
                Log.Fatal(ex, "Application terminated unexpectedly");
            }
            catch (Exception logEx)
            {
                StartupDebug.LogStartup($"ERROR: Failed to log fatal error: {logEx.Message}");
            }
            try
            {
                ShowFatalErrorMessage(ex);
            }
            catch (Exception msgEx)
            {
                StartupDebug.LogStartup($"ERROR: Failed to show fatal error message: {msgEx.Message}");
            }
        }
        finally
        {
            // Dispose of services when application exits
            _serviceProvider?.Dispose();
            
            // Ensure all Sentry events are sent
            if (!string.IsNullOrEmpty(_appSettings?.Sentry?.Dsn))
            {
                SentrySdk.FlushAsync(TimeSpan.FromSeconds(2)).GetAwaiter().GetResult();
            }
            
            // Flush and close Serilog
            Log.CloseAndFlush();
        }
    }

    private static void SetupExceptionHandling()
    {
        // Handle exceptions in all threads
        Application.ThreadException += (sender, e) => 
        {
            Log.Error(e.Exception, "Thread exception occurred");
            HandleException(e.Exception, "Thread Exception");
        };

        // Handle exceptions in the AppDomain
        AppDomain.CurrentDomain.UnhandledException += (sender, e) => 
        {
            var exception = e.ExceptionObject as Exception;
            Log.Fatal(exception, "Unhandled AppDomain exception occurred");
            
            if (e.IsTerminating)
            {
                Log.Fatal("Application is terminating due to unhandled exception");
                ShowFatalErrorMessage(exception);
            }
            else
            {
                HandleException(exception, "AppDomain Exception");
            }
        };

        // Set the unhandled exception mode to catch all Windows Forms errors
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
    }

    [SupportedOSPlatform("windows")]
    private static void HandleException(Exception? exception, string source)
    {
        try
        {
            var errorMessage = $"An error occurred in {source}:\n\n{exception?.Message}";
            Log.Error(exception, errorMessage);

            // Capture exception in Sentry if configured
            if (!string.IsNullOrEmpty(_appSettings?.Sentry?.Dsn))
            {
                SentrySdk.CaptureException(exception);
            }
            
            // Track exception in AppCenter if configured
            if (!string.IsNullOrEmpty(_appSettings?.AppCenter?.SecretKey) && 
                _appSettings?.AppCenter?.EnableCrashes == true)
            {
                Crashes.TrackError(exception);
            }

            // Show a user-friendly error message
            MessageBox.Show(
                errorMessage,
                "Application Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        catch (Exception ex)
        {
            // If we can't handle the exception gracefully, at least log it
            Log.Fatal(ex, "Error in exception handler");
        }
    }

    [SupportedOSPlatform("windows")]
    private static void ShowFatalErrorMessage(Exception? exception)
    {
        try
        {
            // Capture fatal exception in Sentry if configured
            if (!string.IsNullOrEmpty(_appSettings?.Sentry?.Dsn))
            {
                SentrySdk.CaptureException(exception);
                // Ensure all events are sent before the application exits
                SentrySdk.FlushAsync(TimeSpan.FromSeconds(2)).GetAwaiter().GetResult();
            }
            
            // Track fatal exception in AppCenter if configured
            if (!string.IsNullOrEmpty(_appSettings?.AppCenter?.SecretKey) && 
                _appSettings?.AppCenter?.EnableCrashes == true)
            {
                Crashes.TrackError(exception);
            }

            var message = "A fatal error has occurred and the application needs to close.\n\n" +
                          $"Error details: {exception?.Message}\n\n" +
                          "Please check the log files for more information.";

            MessageBox.Show(
                message,
                "Fatal Application Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        catch
        {
            // Last resort if even showing a message box fails
        }
    }

    private static void ConfigureServices()
    {
        Console.WriteLine("Starting ConfigureServices...");
        var services = new ServiceCollection();

        // Add configuration
        services.AddSingleton(_configuration!);
        services.AddSingleton(_appSettings!);
        Console.WriteLine("Added configuration to services");

        // Configure memory cache with custom options for better performance
        services.AddMemoryCache(options => 
        {
            // options.SizeLimit = 1024; // Set a reasonable size limit for the cache (DISABLED to fix error)
            options.ExpirationScanFrequency = TimeSpan.FromMinutes(5); // Scan for expired items every 5 minutes
            options.CompactionPercentage = 0.2; // Remove 20% of entries when size limit is reached
        });
        Console.WriteLine("Configured memory cache with optimized settings");

        // Register database helper as singleton
        services.AddSingleton<DatabaseHelper>(provider => {
            Console.WriteLine("Creating DatabaseHelper...");
            var config = provider.GetRequiredService<IConfiguration>();
            var appSettings = provider.GetRequiredService<AppSettings>();
            var dbHelper = new DatabaseHelper(config, appSettings);
            Console.WriteLine("DatabaseHelper created");
            return dbHelper;
        });

        // Register repositories
        services.AddScoped<ICategoryRepository>(provider => {
            Console.WriteLine("Creating CategoryRepository...");
            var dbHelper = provider.GetRequiredService<DatabaseHelper>();
            return new CategoryRepository(dbHelper.Connection);
        });
        
        services.AddScoped<ITaskRepository>(provider => {
            Console.WriteLine("Creating TaskRepository...");
            var dbHelper = provider.GetRequiredService<DatabaseHelper>();
            var categoryRepo = provider.GetRequiredService<ICategoryRepository>();
            return new TaskRepository(dbHelper.Connection);
        });

        // Register services
        services.AddScoped<ITaskService, TaskService>();
        services.AddScoped<ISessionService, SessionService>();
        services.AddSingleton<IConfigurationService, ConfigurationService>();
        Console.WriteLine("Added repositories and services");

        // Register main form
        services.AddTransient<Form1>(provider =>
        {
            Console.WriteLine("Creating Form1...");
            var dbHelper = provider.GetRequiredService<DatabaseHelper>();
            var taskService = provider.GetRequiredService<ITaskService>();
            var sessionService = provider.GetRequiredService<ISessionService>();
            var categoryRepo = provider.GetRequiredService<ICategoryRepository>();
            var configService = provider.GetRequiredService<IConfigurationService>();
            var form = new Form1(dbHelper, taskService, sessionService, categoryRepo, configService, provider);
            Console.WriteLine("Form1 created");
            return form;
        });

        // Register Dashboard form and viewmodel
        services.AddTransient<DashboardViewModel>();
        services.AddTransient<UI.DashboardForm>(provider => {
            Console.WriteLine("Creating DashboardForm...");
            var viewModel = provider.GetRequiredService<DashboardViewModel>();
            return new UI.DashboardForm(viewModel);
        });
        Console.WriteLine("Added forms to services");

        // Register Serilog as the logging provider
        _serilogLogger = Log.Logger;
        services.AddLogging(loggingBuilder =>
        {
            loggingBuilder.ClearProviders();
            loggingBuilder.AddSerilog(_serilogLogger, dispose: false);
        });
        Console.WriteLine("Added logging to services");

        // Register DatabaseHealthCheck
        services.AddTransient<PricisApp.Infrastructure.HealthChecks.DatabaseHealthCheck>();

        // Register health checks
        services.AddHealthChecks()
            .AddCheck<PricisApp.Infrastructure.HealthChecks.DatabaseHealthCheck>("database_health_check");
        Console.WriteLine("Registered health checks");

        // Register optimized TaskService with caching
        services.AddSingleton<TaskService>(sp =>
        {
            Console.WriteLine("Creating TaskService singleton with optimized caching...");
            var db = sp.GetRequiredService<DatabaseHelper>();
            var cache = sp.GetRequiredService<IMemoryCache>();
            var config = sp.GetRequiredService<IConfiguration>();
            var logger = sp.GetRequiredService<ILogger<TaskService>>();
            var taskService = new TaskService(db, cache, config, logger);
            
            // Configure advanced caching options
            var cacheEntryOptions = new MemoryCacheEntryOptions()
                .SetSlidingExpiration(TimeSpan.FromMinutes(5))
                .SetAbsoluteExpiration(TimeSpan.FromHours(1))
                .SetPriority(CacheItemPriority.High)
                .SetSize(1); // Relative size is 1 unit
                
            // Add this options object to the cache so TaskService can use it
            cache.Set("DefaultCacheOptions", cacheEntryOptions);
            
            return taskService;
        });
        Console.WriteLine("Building service provider...");
        _serviceProvider = services.BuildServiceProvider();
        Console.WriteLine("Service provider built");
    }

    public static IServiceProvider CreateServices(string connectionString)
    {
        Console.WriteLine("Creating migration services with connection string...");
        var services = new ServiceCollection();
        services.AddFluentMigratorCore()
            .ConfigureRunner(rb => rb
                .AddSQLite()
                .WithGlobalConnectionString(connectionString)
                .ScanIn(typeof(Program).Assembly).For.Migrations())
            .AddLogging(lb => lb.AddFluentMigratorConsole());
        services.AddMemoryCache(options => 
        {
            // options.SizeLimit = 1024;
            options.ExpirationScanFrequency = TimeSpan.FromMinutes(5);
            options.CompactionPercentage = 0.2;
        });
        // Register configuration and logging for migration services
        services.AddSingleton<IConfiguration>(_ => new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile("appsettings.user.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables()
            .Build());
        services.AddSingleton<TaskService>(sp =>
        {
            Console.WriteLine("Creating TaskService for migrations...");
            var db = sp.GetRequiredService<DatabaseHelper>();
            var cache = sp.GetRequiredService<IMemoryCache>();
            var config = sp.GetRequiredService<IConfiguration>();
            var logger = sp.GetRequiredService<ILogger<TaskService>>();
            return new TaskService(db, cache, config, logger);
        });
        Console.WriteLine("Building migration service provider...");
        return services.BuildServiceProvider(false);
    }

    private static void RunConsoleMode()
    {
        Console.WriteLine("Running in console mode...");
        
        try
        {
            // Load configuration
            _configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile("appsettings.user.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables() // Use this instead of AddInMemoryCollection
                .Build();
            
            Console.WriteLine("Configuration loaded");

            // Bind configuration to settings object
            _appSettings = new AppSettings();
            _configuration.Bind(_appSettings);
            
            Console.WriteLine("Settings bound to object");
            
            // Ensure Logs directory exists
            try
            {
                Directory.CreateDirectory("Logs");
                Console.WriteLine("Logs directory created");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating Logs directory: {ex.Message}");
                // Continue execution even if we can't create the logs directory
            }

            // Configure Serilog from configuration
            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(_configuration)
                .CreateLogger();
            
            Console.WriteLine("Serilog configured");
            
            // Configure services
            var services = new ServiceCollection();
            services.AddSingleton(_configuration);
            services.AddSingleton(_appSettings);
            
            // Add memory cache with optimized settings
            services.AddMemoryCache(options => 
            {
                // options.SizeLimit = 1024;
                options.ExpirationScanFrequency = TimeSpan.FromMinutes(5);
                options.CompactionPercentage = 0.2;
            });
            
            // Register database helper as singleton
            services.AddSingleton<DatabaseHelper>(provider => {
                Console.WriteLine("Creating DatabaseHelper...");
                var config = provider.GetRequiredService<IConfiguration>();
                var appSettings = provider.GetRequiredService<AppSettings>();
                var dbHelper = new DatabaseHelper(config, appSettings);
                Console.WriteLine("DatabaseHelper created");
                return dbHelper;
            });
            
            _serviceProvider = services.BuildServiceProvider();
            Console.WriteLine("Services configured");
            
            // Get the database helper
            var dbHelper = _serviceProvider.GetRequiredService<DatabaseHelper>();
            Console.WriteLine("Database helper retrieved");
            
            // Test database connection
            var connection = dbHelper.Connection;
            Console.WriteLine($"Database connection state: {connection.State}");
            
            Console.WriteLine("Console mode completed successfully");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"FATAL ERROR: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
        }
        finally
        {
            _serviceProvider?.Dispose();
            Log.CloseAndFlush();
        }
    }

    private static async Task RunDiagnosticModeAsync()
    {
        Console.WriteLine("Running in diagnostic mode...");
        
        try
        {
            // Check current directory
            Console.WriteLine($"Current directory: {Directory.GetCurrentDirectory()}");
            
            // Check if Logs directory exists
            var logsDir = Path.Combine(Directory.GetCurrentDirectory(), "Logs");
            Console.WriteLine($"Logs directory exists: {Directory.Exists(logsDir)}");
            
            // Try to create Logs directory
            try
            {
                Directory.CreateDirectory(logsDir);
                Console.WriteLine("Logs directory created/confirmed");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating Logs directory: {ex.Message}");
            }
            
            // Load configuration
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile("appsettings.user.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables() // Use this instead of AddInMemoryCollection
                .Build();
            
            Console.WriteLine("Configuration loaded");
            
            // Check database settings
            var dbFileName = configuration["Database:FileName"] ?? "TimeTracking.db";
            Console.WriteLine($"Database filename from config: {dbFileName}");
            
            // Check database file
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var dbPath = Path.Combine(localAppData, dbFileName);
            Console.WriteLine($"Database path: {dbPath}");
            Console.WriteLine($"Database exists: {File.Exists(dbPath)}");
            
            // Try to fix database
            Console.WriteLine("Attempting to fix database...");
            await DatabaseFix.FixDatabaseAsync();
            
            // Check for running instances of the application
            Console.WriteLine("\nChecking for running instances of the application...");
            var currentProcess = Process.GetCurrentProcess();
            var processes = Process.GetProcessesByName(currentProcess.ProcessName);
            
            Console.WriteLine($"Found {processes.Length} instance(s) of {currentProcess.ProcessName}");
            foreach (var process in processes)
            {
                if (process.Id != currentProcess.Id)
                {
                    Console.WriteLine($"  - Process ID: {process.Id}, Started: {process.StartTime}");
                    
                    // Ask if user wants to terminate the process
                    Console.Write("Do you want to terminate this process? (y/n): ");
                    var response = Console.ReadLine()?.ToLower();
                    if (response == "y" || response == "yes")
                    {
                        try
                        {
                            process.Kill();
                            Console.WriteLine("Process terminated.");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error terminating process: {ex.Message}");
                        }
                    }
                }
            }
            
            // Run database optimization
            Console.WriteLine("\nRunning database optimization...");
            try 
            {
                var services = new ServiceCollection();
                services.AddSingleton(configuration);
                var appSettings = new AppSettings();
                configuration.Bind(appSettings);
                services.AddSingleton(appSettings);
                services.AddSingleton<DatabaseHelper>();
                var serviceProvider = services.BuildServiceProvider();
                
                var dbHelper = serviceProvider.GetRequiredService<DatabaseHelper>();
                using var connection = dbHelper.Connection;
                connection.Open();
                using var cmd = connection.CreateCommand();
                
                // Run SQLite VACUUM to reclaim space and defragment
                cmd.CommandText = "VACUUM;";
                await cmd.ExecuteNonQueryAsync();
                Console.WriteLine("Database VACUUM completed successfully");
                
                // Run ANALYZE to update statistics
                cmd.CommandText = "ANALYZE;";
                await cmd.ExecuteNonQueryAsync();
                Console.WriteLine("Database ANALYZE completed successfully");
                
                serviceProvider.Dispose();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error optimizing database: {ex.Message}");
            }
            
            Console.WriteLine("\nDiagnostic completed. Press any key to exit.");
            Console.ReadKey();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in diagnostic mode: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
            Console.WriteLine("\nPress any key to exit.");
            Console.ReadKey();
        }
    }
}
