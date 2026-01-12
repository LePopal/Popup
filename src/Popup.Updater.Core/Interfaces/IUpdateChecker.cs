using System;
using System.Collections.Generic;
using System.Text;

using Popup.Updater.Core.Models;

namespace Popup.Updater.Core.Interfaces;

/// <summary>
/// Service to check for application updates
/// </summary>
public interface IUpdateChecker
{
    /// <summary>
    /// Check if an update is available
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Update information if available, null otherwise</returns>
    Task<UpdateInfo?> IsThereAnUpdateAsync(CancellationToken cancellationToken = default);
}