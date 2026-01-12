using System;
using System.Collections.Generic;
using System.Text;

using Popup.Updater.Core.Models;

namespace Popup.Updater.Core.Interfaces;

/// <summary>
/// Interface for update sources (GitHub, custom server, etc.)
/// </summary>
public interface IUpdateSource
{
    /// <summary>
    /// Check if an update is available
    /// </summary>
    /// <param name="currentVersion">Current application version</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Update information if available, null otherwise</returns>
    Task<UpdateInfo?> CheckForUpdateAsync(string currentVersion, CancellationToken cancellationToken = default);

    /// <summary>
    /// Download the update package
    /// </summary>
    /// <param name="updateInfo">Update information</param>
    /// <param name="destinationPath">Download destination path</param>
    /// <param name="progress">Progress reporter</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Path to the downloaded file</returns>
    Task<string> DownloadUpdateAsync(
        UpdateInfo updateInfo,
        string destinationPath,
        IProgress<UpdateProgress>? progress = null,
        CancellationToken cancellationToken = default);
}