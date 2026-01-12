using Microsoft.Extensions.Logging;
using Popup.Updater.Core.Configuration;
using Popup.Updater.Core.Interfaces;
using Popup.Updater.Core.Models;

namespace Popup.Updater.Core.Services;

/// <summary>
/// Service to check for application updates
/// </summary>
public sealed class UpdateChecker : IUpdateChecker
{
    private readonly UpdateConfiguration _config;
    private readonly IUpdateSource _updateSource;
    private readonly ILogger<UpdateChecker> _logger;

    public UpdateChecker(
        UpdateConfiguration config,
        IUpdateSource updateSource,
        ILogger<UpdateChecker> logger)
    {
        _config = config;
        _updateSource = updateSource;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<UpdateInfo?> IsThereAnUpdateAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Checking for updates for {AppName} version {Version}",
                _config.Application.Name,
                _config.Application.CurrentVersion);

            var updateInfo = await _updateSource.CheckForUpdateAsync(
                _config.Application.CurrentVersion,
                cancellationToken);

            if (updateInfo != null)
            {
                _logger.LogInformation("Update available: {Version}", updateInfo.Version);
            }
            else
            {
                _logger.LogInformation("No update available");
            }

            return updateInfo;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking for updates");
            return null;
        }
    }
}