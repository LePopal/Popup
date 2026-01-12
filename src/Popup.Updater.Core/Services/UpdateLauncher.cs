using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Popup.Updater.Core.Configuration;
using Popup.Updater.Core.Interfaces;
using Popup.Updater.Core.Models;

namespace Popup.Updater.Core.Services;

/// <summary>
/// Service to launch the update process
/// </summary>
public sealed class UpdateLauncher : IUpdateLauncher
{
    private readonly UpdateConfiguration _config;
    private readonly IUpdateSource _updateSource;
    private readonly IPlatformHelper _platformHelper;
    private readonly ILogger<UpdateLauncher> _logger;

    public UpdateLauncher(
        UpdateConfiguration config,
        IUpdateSource updateSource,
        IPlatformHelper platformHelper,
        ILogger<UpdateLauncher> logger)
    {
        _config = config;
        _updateSource = updateSource;
        _platformHelper = platformHelper;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<bool> StartUpdateAsync(
        UpdateInfo updateInfo,
        IProgress<UpdateProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting update process for version {Version}", updateInfo.Version);

            // Step 1: Download the update
            progress?.Report(new UpdateProgress
            {
                Percentage = 0,
                BytesDownloaded = 0,
                TotalBytes = updateInfo.FileSize,
                CurrentStep = UpdateStep.Downloading,
                Message = "Preparing download..."
            });

            var tempPath = Path.Combine(_config.Updater.TempDownloadPath, $"update-{updateInfo.Version}.zip");

            // Ensure temp directory exists
            Directory.CreateDirectory(_config.Updater.TempDownloadPath);

            var downloadedFile = await _updateSource.DownloadUpdateAsync(
                updateInfo,
                tempPath,
                progress,
                cancellationToken);

            _logger.LogInformation("Update downloaded to {Path}", downloadedFile);

            // Step 2: Launch the updater executable
            var currentProcess = Process.GetCurrentProcess();
            var installPath = _config.Application.InstallPath ??
                              Path.GetDirectoryName(currentProcess.MainModule?.FileName) ??
                              Environment.CurrentDirectory;

            var launcherPath = Path.Combine(installPath, _config.Updater.LauncherExecutable);

            if (!File.Exists(launcherPath))
            {
                _logger.LogError("Updater launcher not found at {Path}", launcherPath);
                return false;
            }

            // Build launcher arguments
            var arguments = BuildLauncherArguments(
                currentProcess.Id,
                downloadedFile,
                installPath,
                _config.Application.ExecutableName);

            _logger.LogInformation("Launching updater: {Launcher} {Arguments}", launcherPath, arguments);

            // Check if elevation is needed
            var needsElevation = _config.Updater.RequestElevation &&
                                _platformHelper.NeedsElevation(installPath);

            if (needsElevation)
            {
                _logger.LogInformation("Requesting elevation for update");
                _platformHelper.RequestElevation(launcherPath, arguments);
            }
            else
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = launcherPath,
                    Arguments = arguments,
                    UseShellExecute = true,
                    WorkingDirectory = installPath
                };

                Process.Start(startInfo);
            }

            _logger.LogInformation("Updater launched successfully. Application will now exit.");

            // Step 3: Exit the current application
            // The updater will wait for this process to exit, then apply the update
            Environment.Exit(0);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting update process");
            progress?.Report(new UpdateProgress
            {
                Percentage = 0,
                BytesDownloaded = 0,
                TotalBytes = 0,
                CurrentStep = UpdateStep.Failed,
                Message = $"Update failed: {ex.Message}"
            });
            return false;
        }
    }

    private static string BuildLauncherArguments(
        int processPid,
        string updatePackagePath,
        string installPath,
        string executableName)
    {
        return $"--pid {processPid} --package \"{updatePackagePath}\" --install \"{installPath}\" --executable \"{executableName}\"";
    }
}