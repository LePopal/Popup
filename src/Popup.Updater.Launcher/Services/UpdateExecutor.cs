using System.Diagnostics;
using System.IO.Compression;
using Serilog;

namespace Popup.Updater.Launcher.Services;

/// <summary>
/// Executes the update process
/// </summary>
public sealed class UpdateExecutor
{
    private readonly int _targetProcessId;
    private readonly string _updatePackagePath;
    private readonly string _installPath;
    private readonly string _executableName;

    public UpdateExecutor(
        int targetProcessId,
        string updatePackagePath,
        string installPath,
        string executableName)
    {
        _targetProcessId = targetProcessId;
        _updatePackagePath = updatePackagePath;
        _installPath = installPath;
        _executableName = executableName;
    }

    /// <summary>
    /// Execute the full update process
    /// </summary>
    public async Task<bool> ExecuteAsync()
    {
        try
        {
            Log.Information("=== Starting Update Process ===");
            Log.Information("Target Process ID: {ProcessId}", _targetProcessId);
            Log.Information("Update Package: {PackagePath}", _updatePackagePath);
            Log.Information("Install Path: {InstallPath}", _installPath);
            Log.Information("Executable: {Executable}", _executableName);

            // Step 1: Wait for the main application to close
            Log.Information("Waiting for application to close...");
            if (!await WaitForProcessToExitAsync(_targetProcessId, TimeSpan.FromSeconds(30)))
            {
                Log.Warning("Application did not close gracefully within timeout, forcing update...");
            }

            // Step 2: Extract the update package
            Log.Information("Extracting update package...");
            await ExtractUpdatePackageAsync();

            // Step 3: Clean up
            Log.Information("Cleaning up temporary files...");
            CleanupUpdatePackage();

            // Step 4: Restart the application
            Log.Information("Restarting application...");
            RestartApplication();

            Log.Information("Update completed successfully!");
            return true;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Update failed");
            return false;
        }
    }

    private static async Task<bool> WaitForProcessToExitAsync(int processId, TimeSpan timeout)
    {
        try
        {
            var process = Process.GetProcessById(processId);
            Log.Debug("Waiting for process {ProcessId} ({ProcessName}) to exit", processId, process.ProcessName);
            return await Task.Run(() => process.WaitForExit((int)timeout.TotalMilliseconds));
        }
        catch (ArgumentException)
        {
            // Process already exited
            Log.Debug("Process {ProcessId} already exited", processId);
            return true;
        }
    }

    private async Task ExtractUpdatePackageAsync()
    {
        if (!File.Exists(_updatePackagePath))
        {
            throw new FileNotFoundException($"Update package not found: {_updatePackagePath}");
        }

        Log.Debug("Update package size: {Size} bytes", new FileInfo(_updatePackagePath).Length);

        // Create a temporary extraction directory
        var tempExtractPath = Path.Combine(Path.GetTempPath(), $"popup_update_{Guid.NewGuid()}");
        Directory.CreateDirectory(tempExtractPath);
        Log.Debug("Temporary extraction path: {TempPath}", tempExtractPath);

        try
        {
            // Extract the zip
            Log.Debug("Extracting ZIP archive...");
            await Task.Run(() => ZipFile.ExtractToDirectory(_updatePackagePath, tempExtractPath, overwriteFiles: true));
            Log.Debug("ZIP extraction completed");

            // Copy files to install directory, overwriting existing files
            Log.Debug("Copying files to installation directory...");
            await CopyDirectoryAsync(tempExtractPath, _installPath);
            Log.Information("Files copied successfully");
        }
        finally
        {
            // Clean up temp extraction directory
            if (Directory.Exists(tempExtractPath))
            {
                Log.Debug("Cleaning up temporary extraction directory");
                Directory.Delete(tempExtractPath, recursive: true);
            }
        }
    }

    private static async Task CopyDirectoryAsync(string sourceDir, string destDir)
    {
        var dir = new DirectoryInfo(sourceDir);
        if (!dir.Exists)
        {
            throw new DirectoryNotFoundException($"Source directory not found: {sourceDir}");
        }

        // Ensure destination directory exists
        Directory.CreateDirectory(destDir);

        // Copy all files
        foreach (var file in dir.GetFiles())
        {
            var targetPath = Path.Combine(destDir, file.Name);

            // Retry logic for locked files
            var maxRetries = 3;
            for (var i = 0; i < maxRetries; i++)
            {
                try
                {
                    file.CopyTo(targetPath, overwrite: true);
                    Log.Debug("Copied: {FileName}", file.Name);
                    break;
                }
                catch (IOException ex) when (i < maxRetries - 1)
                {
                    Log.Warning("Retry {Retry}/{MaxRetries} copying {FileName}: {Error}", i + 1, maxRetries, file.Name, ex.Message);
                    await Task.Delay(1000);
                }
            }
        }

        // Recursively copy subdirectories
        foreach (var subDir in dir.GetDirectories())
        {
            var targetSubDir = Path.Combine(destDir, subDir.Name);
            await CopyDirectoryAsync(subDir.FullName, targetSubDir);
        }
    }

    private void CleanupUpdatePackage()
    {
        try
        {
            if (File.Exists(_updatePackagePath))
            {
                File.Delete(_updatePackagePath);
                Log.Debug("Deleted update package: {Path}", _updatePackagePath);
            }

            // Clean up temp directory if empty
            var tempDir = Path.GetDirectoryName(_updatePackagePath);
            if (!string.IsNullOrEmpty(tempDir) &&
                Directory.Exists(tempDir) &&
                !Directory.EnumerateFileSystemEntries(tempDir).Any())
            {
                Directory.Delete(tempDir);
                Log.Debug("Deleted empty temp directory: {Path}", tempDir);
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Could not clean up update package");
        }
    }

    private void RestartApplication()
    {
        var executablePath = Path.Combine(_installPath, _executableName);

        if (!File.Exists(executablePath))
        {
            Log.Warning("Executable not found at {Path}", executablePath);
            return;
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = executablePath,
            UseShellExecute = true,
            WorkingDirectory = _installPath
        };

        Log.Information("Starting application: {Executable}", executablePath);
        Process.Start(startInfo);
    }
}