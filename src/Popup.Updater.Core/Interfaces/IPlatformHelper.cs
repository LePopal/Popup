using System;
using System.Collections.Generic;
using System.Text;

namespace Popup.Updater.Core.Interfaces;

/// <summary>
/// Platform-specific operations helper
/// </summary>
public interface IPlatformHelper
{
    /// <summary>
    /// Get the current platform name (win, osx, linux)
    /// </summary>
    string GetPlatform();

    /// <summary>
    /// Get the current architecture (x64, arm64)
    /// </summary>
    string GetArchitecture();

    /// <summary>
    /// Check if the application needs elevation to write to its directory
    /// </summary>
    bool NeedsElevation(string installPath);

    /// <summary>
    /// Request administrator/sudo elevation
    /// </summary>
    /// <param name="executablePath">Path to the executable to run with elevation</param>
    /// <param name="arguments">Arguments to pass</param>
    /// <returns>True if elevation was granted</returns>
    bool RequestElevation(string executablePath, string arguments);
}