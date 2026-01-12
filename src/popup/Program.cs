using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Popup.Updater.Core.Configuration;
using Popup.Updater.Core.Interfaces;
using Popup.Updater.Core.Services;
using Serilog;
using System.Text.Json;

namespace Popup;

internal class Program
{
    static async Task Main(string[] args)
    {
        // Configure Serilog
        var logPath = Path.Combine(AppContext.BaseDirectory, "Logs", "popup-.log");
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console()
            .WriteTo.File(
                logPath,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 30,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();

        try
        {
            Log.Information("=== Popup Application Starting ===");

            // Load configuration
            var config = LoadConfiguration();

            // Setup dependency injection
            var services = new ServiceCollection();
            ConfigureServices(services, config);

            var serviceProvider = services.BuildServiceProvider();

            // Get the update checker
            var updateChecker = serviceProvider.GetRequiredService<IUpdateChecker>();
            var updateLauncher = serviceProvider.GetRequiredService<IUpdateLauncher>();

            // Check for updates
            Log.Information("Checking for updates...");
            Console.WriteLine("Checking for updates...");

            var updateInfo = await updateChecker.IsThereAnUpdateAsync();

            if (updateInfo != null)
            {
                Console.WriteLine();
                Console.WriteLine($"🎉 New version available: {updateInfo.Version}");
                Console.WriteLine($"Current version: {config.Application.CurrentVersion}");
                Console.WriteLine($"Size: {updateInfo.FileSize / 1024 / 1024} MB");
                Console.WriteLine($"Published: {updateInfo.PublishedAt:yyyy-MM-dd}");

                if (!string.IsNullOrWhiteSpace(updateInfo.ReleaseNotes))
                {
                    Console.WriteLine();
                    Console.WriteLine("Release notes:");
                    Console.WriteLine(updateInfo.ReleaseNotes);
                }

                Console.WriteLine();
                Console.Write("Do you want to update? (y/n): ");
                var response = Console.ReadLine();

                if (response?.Trim().ToLowerInvariant() == "y")
                {
                    Console.WriteLine();
                    Console.WriteLine("Starting update...");
                    Log.Information("User accepted update to version {Version}", updateInfo.Version);

                    var progress = new Progress<Updater.Core.Models.UpdateProgress>(p =>
                    {
                        var message = $"[{p.CurrentStep}] {p.Percentage}% - {p.Message}";
                        Console.WriteLine(message);
                        Log.Information("Update progress: {Message}", message);
                    });

                    await updateLauncher.StartUpdateAsync(updateInfo, progress);

                    // The application will exit here if update starts successfully
                }
                else
                {
                    Log.Information("User declined update");
                    Console.WriteLine("Update cancelled.");
                }
            }
            else
            {
                Console.WriteLine("✓ You are running the latest version!");
                Log.Information("No updates available");
            }

            Console.WriteLine();
            Console.WriteLine("Application running... Press any key to exit.");
            Console.ReadKey();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Application crashed");
            Console.WriteLine($"ERROR: {ex.Message}");
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }
        finally
        {
            await Log.CloseAndFlushAsync();
        }
    }

    private static UpdateConfiguration LoadConfiguration()
    {
        var configPath = Path.Combine(AppContext.BaseDirectory, "updateconfig.json");

        if (!File.Exists(configPath))
        {
            Log.Fatal("Configuration file not found at {Path}", configPath);
            throw new FileNotFoundException($"Configuration file not found: {configPath}");
        }

        var json = File.ReadAllText(configPath);
        var config = JsonSerializer.Deserialize<UpdateConfiguration>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (config == null)
        {
            Log.Fatal("Failed to deserialize configuration");
            throw new InvalidOperationException("Failed to deserialize configuration");
        }

        Log.Information("Configuration loaded successfully");
        return config;
    }

    private static void ConfigureServices(IServiceCollection services, UpdateConfiguration config)
    {
        // Logging avec Serilog
        services.AddLogging(builder =>
        {
            builder.ClearProviders();
            builder.AddSerilog();
        });

        // Configuration
        services.AddSingleton(config);
        services.AddSingleton(config.UpdateSource);
        services.AddSingleton(config.Application);
        services.AddSingleton(config.Updater);

        // Services
        services.AddSingleton<IPlatformHelper, PlatformHelper>();
        services.AddSingleton<IUpdateSource, GitHubUpdateSource>();
        services.AddSingleton<IUpdateChecker, UpdateChecker>();
        services.AddSingleton<IUpdateLauncher, UpdateLauncher>();
    }
}