# PricisApp

A time tracking application built with .NET and Windows Forms.

## Configuration

PricisApp now uses `appsettings.json` for configuration. This allows for easier customization without recompiling the application.

### Configuration Structure

The configuration is structured as follows:

```json
{
  "Database": {
    "FileName": "TimeTracking.db",
    "ConnectionString": {
      "Mode": "ReadWriteCreate",
      "Cache": "Shared",
      "Pooling": true
    }
  },
  "Logging": {
    "MinimumLevel": "Debug",
    "FilePath": "Logs/pricisapp-{Date}.log",
    "RetainedFileCount": 7,
    "FileSizeLimitBytes": 10000000
  },
  "UI": {
    "DefaultTheme": "System Default",
    "AvailableThemes": [
      "System Default",
      "Light",
      "Dark",
      "Blue",
      "Green",
      "High Contrast"
    ],
    "DefaultFont": {
      "Name": "Segoe UI",
      "Size": 9.0
    }
  }
}
```

### Accessing Configuration

Configuration can be accessed in two ways:

1. **Using IConfigurationService**

   The recommended way to access configuration is through the `IConfigurationService` interface:

   ```csharp
   public class MyClass
   {
       private readonly IConfigurationService _configService;

       public MyClass(IConfigurationService configService)
       {
           _configService = configService;
           
           // Access configuration values
           string dbFileName = _configService.GetDatabaseFileName();
           string defaultTheme = _configService.GetDefaultTheme();
           string[] themes = _configService.GetAvailableThemes();
           
           // Get any value by key
           int retryCount = _configService.GetValue<int>("SomeSection:RetryCount", 3);
       }
   }
   ```

2. **Using IConfiguration and AppSettings**

   For more direct access, you can use the `IConfiguration` and `AppSettings` classes:

   ```csharp
   public class MyClass
   {
       private readonly IConfiguration _configuration;
       private readonly AppSettings _appSettings;

       public MyClass(IConfiguration configuration, AppSettings appSettings)
       {
           _configuration = configuration;
           _appSettings = appSettings;
           
           // Access configuration values
           string dbFileName = _appSettings.Database.FileName;
           string defaultTheme = _appSettings.UI.DefaultTheme;
           
           // Get any value by key
           int retryCount = _configuration.GetValue<int>("SomeSection:RetryCount", 3);
       }
   }
   ```

### Customizing Configuration

To customize the application's configuration:

1. Edit the `appsettings.json` file in the application directory
2. Restart the application for changes to take effect

### User-Specific Settings

User-specific settings (like the selected theme) are still stored in the user's local application data folder for backward compatibility, but the application now uses the configuration service as the primary source of configuration values. 