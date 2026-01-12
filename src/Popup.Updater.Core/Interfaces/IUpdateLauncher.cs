using System;
using System.Collections.Generic;
using System.Text;

using Popup.Updater.Core.Models;

namespace Popup.Updater.Core.Interfaces;

/// <summary>
/// Service to launch the update process
/// </summary>
public interface IUpdateLauncher
{
    /// <summary>
    /// Start the update process (downloads, launches updater, and closes the app)
    /// </summary>
    /// <param name="updateInfo">Update information</param>
    /// <param name="progress">Progress reporter</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if the update process started successfully</returns>
    Task<bool> StartUpdateAsync(
        UpdateInfo updateInfo,
        IProgress<UpdateProgress>? progress = null,
        CancellationToken cancellationToken = default);
}