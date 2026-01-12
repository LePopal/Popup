using Popup.Updater.Launcher.Platform;
using Popup.Updater.Launcher.Services;
using Serilog;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Popup.Updater.Launcher;

internal class Program
{
    static async Task<int> Main(string[] args)
    {
        // Configure Serilog
        var logPath = Path.Combine(Path.GetTempPath(), "Popup", "Logs", "updater-.log");
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console()
            .WriteTo.File(
                logPath,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();

        try
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0";
            Log.Information("Popup Updater Launcher v{Version}", version);

            // Parse command line arguments
            var arguments = ParseArguments(args);

            if (!ValidateArguments(arguments, out var error))
            {
                Log.Error("Invalid arguments: {Error}", error);
                Console.WriteLine($"ERROR: {error}");
                Console.WriteLine();
                PrintUsage();
                return 1;
            }

            // Check if we need elevation on Windows
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                if (!WindowsElevationHelper.IsRunningAsAdministrator())
                {
                    Log.Warning("Not running as administrator. Update may fail if UAC elevation was not granted.");
                }
            }

            // Execute the update
            var executor = new UpdateExecutor(
                int.Parse(arguments["pid"]!),
                arguments["package"]!,
                arguments["install"]!,
                arguments["executable"]!);

            var success = await executor.ExecuteAsync();

            if (!success)
            {
                Log.Error("Update failed");
                Console.WriteLine();
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey();
                return 1;
            }

            Log.Information("Update completed successfully");

            // Small delay before exiting
            await Task.Delay(2000);
            return 0;
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Unhandled exception in updater");
            Console.WriteLine($"FATAL ERROR: {ex.Message}");
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
            return 1;
        }
        finally
        {
            await Log.CloseAndFlushAsync();
        }
    }

    private static Dictionary<string, string?> ParseArguments(string[] args)
    {
        var arguments = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            { "pid", null },
            { "package", null },
            { "install", null },
            { "executable", null }
        };

        for (var i = 0; i < args.Length; i++)
        {
            if (args[i].StartsWith("--"))
            {
                var key = args[i].TrimStart('-');
                if (i + 1 < args.Length && !args[i + 1].StartsWith("--"))
                {
                    arguments[key] = args[i + 1];
                    i++; // Skip next argument as it's the value
                }
            }
        }

        return arguments;
    }

    private static bool ValidateArguments(Dictionary<string, string?> arguments, out string error)
    {
        if (arguments["pid"] == null || !int.TryParse(arguments["pid"], out _))
        {
            error = "Invalid or missing --pid argument";
            return false;
        }

        if (string.IsNullOrWhiteSpace(arguments["package"]))
        {
            error = "Missing --package argument";
            return false;
        }

        if (string.IsNullOrWhiteSpace(arguments["install"]))
        {
            error = "Missing --install argument";
            return false;
        }

        if (string.IsNullOrWhiteSpace(arguments["executable"]))
        {
            error = "Missing --executable argument";
            return false;
        }

        error = string.Empty;
        return true;
    }

    private static void PrintUsage()
    {
        Console.WriteLine("Usage:");
        Console.WriteLine("  Popup.Updater.Launcher.exe --pid <process_id> --package <zip_path> --install <install_dir> --executable <exe_name>");
        Console.WriteLine();
        Console.WriteLine("Arguments:");
        Console.WriteLine("  --pid         Process ID of the application to update");
        Console.WriteLine("  --package     Path to the update package (zip file)");
        Console.WriteLine("  --install     Installation directory");
        Console.WriteLine("  --executable  Executable filename to restart");
    }
}