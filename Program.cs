using System;
using System.Windows.Forms;
using System.Threading;
using System.Globalization;
using System.IO;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using PricisApp.Interfaces;
using PricisApp.Repositories;
using PricisApp.Services;
using PricisApp.Models;
using FluentMigrator.Runner;
using Microsoft.Extensions.Logging;
using Serilog;

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
    static void Main()
    {
        // Load configuration
        _configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();

        // Bind configuration to settings object
        _appSettings = new AppSettings();
        _configuration.Bind(_appSettings);

        // Initialize Serilog
        _serilogLogger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.RollingFile(
                pathFormat: _appSettings.Logging.FilePath,
                retainedFileCountLimit: _appSettings.Logging.RetainedFileCount,
                fileSizeLimitBytes: _appSettings.Logging.FileSizeLimitBytes,
                outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}"
            )
            .CreateLogger();
        Log.Logger = _serilogLogger;
        try
        {
            // Fix database schema first
            DatabaseFix.FixDatabaseAsync().GetAwaiter().GetResult();
            
            // Set culture to invariant for consistent number formatting
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;

            // Enable visual styles
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Set default font from configuration
            Application.SetDefaultFont(new System.Drawing.Font(
                _appSettings.UI.DefaultFont.Name, 
                _appSettings.UI.DefaultFont.Size));

            // Configure services
            ConfigureServices();

            // Create main form with DI
            var mainForm = _serviceProvider!.GetRequiredService<Form1>();

            // Run the application
            Application.Run(mainForm);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An unexpected error occurred in Main");
            MessageBox.Show(
                $"An unexpected error occurred: {ex.Message}\n\nStack Trace:\n{ex.StackTrace}",
                "Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        finally
        {
            // Dispose of services when application exits
            _serviceProvider?.Dispose();
            Log.CloseAndFlush();
        }
    }

    private static void ConfigureServices()
    {
        var services = new ServiceCollection();

        // Add configuration
        services.AddSingleton(_configuration!);
        services.AddSingleton(_appSettings!);

        // Register database helper as singleton
        services.AddSingleton<DatabaseHelper>(provider => {
            var config = provider.GetRequiredService<IConfiguration>();
            var appSettings = provider.GetRequiredService<AppSettings>();
            return new DatabaseHelper(config, appSettings);
        });

        // Register repositories
        services.AddScoped<ICategoryRepository>(provider => {
            var dbHelper = provider.GetRequiredService<DatabaseHelper>();
            return new CategoryRepository(dbHelper.Connection);
        });
        
        services.AddScoped<ITaskRepository>(provider => {
            var dbHelper = provider.GetRequiredService<DatabaseHelper>();
            var categoryRepo = provider.GetRequiredService<ICategoryRepository>();
            return new TaskRepository(dbHelper.Connection, categoryRepo);
        });

        // Register services
        services.AddScoped<ITaskService, TaskService>();
        services.AddScoped<ISessionService, SessionService>();
        services.AddSingleton<IConfigurationService, ConfigurationService>();

        // Register main form
        services.AddTransient<Form1>();

        // Register Serilog as the logging provider
        services.AddLogging(loggingBuilder =>
        {
            loggingBuilder.ClearProviders();
            loggingBuilder.AddSerilog(_serilogLogger, dispose: false);
        });

        _serviceProvider = services.BuildServiceProvider();
    }

    public static IServiceProvider CreateServices(string connectionString)
    {
        return new ServiceCollection()
            .AddFluentMigratorCore()
            .ConfigureRunner(rb => rb
                .AddSQLite()
                .WithGlobalConnectionString(connectionString)
                .ScanIn(typeof(Program).Assembly).For.Migrations())
            .AddLogging(lb => lb.AddFluentMigratorConsole())
            .BuildServiceProvider(false);
    }
}
