using System;
using System.Collections.Generic;
using System.Text;

namespace Popup.Updater.Core.Configuration;

/// <summary>
/// Main configuration for the update system
/// </summary>
public sealed class UpdateConfiguration
{
    /// <summary>
    /// Update source configuration (GitHub, custom server, etc.)
    /// </summary>
    public required UpdateSourceConfiguration UpdateSource { get; init; }

    /// <summary>
    /// Application information to be updated
    /// </summary>
    public required ApplicationConfiguration Application { get; init; }

    /// <summary>
    /// Updater configuration
    /// </summary>
    public required UpdaterConfiguration Updater { get; init; }
}

/// <summary>
/// Update source configuration
/// </summary>
public sealed class UpdateSourceConfiguration
{
    /// <summary>
    /// Source type (GitHub, Custom, etc.)
    /// </summary>
    public required string Type { get; init; }

    /// <summary>
    /// GitHub repository owner
    /// </summary>
    public required string Owner { get; init; }

    /// <summary>
    /// GitHub repository name
    /// </summary>
    public required string Repository { get; init; }

    /// <summary>
    /// Include pre-releases (beta, alpha, etc.)
    /// </summary>
    public bool PreRelease { get; init; }

    /// <summary>
    /// Asset filename pattern: {AppName}-v{Version}-{Platform}-{Architecture}.zip
    /// </summary>
    public required string AssetNamePattern { get; init; }
}

/// <summary>
/// Application configuration
/// </summary>
public sealed class ApplicationConfiguration
{
    /// <summary>
    /// Application name
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Executable name (e.g., Popup.exe)
    /// </summary>
    public required string ExecutableName { get; init; }

    /// <summary>
    /// Current application version
    /// </summary>
    public required string CurrentVersion { get; init; }

    /// <summary>
    /// Installation path (null = auto-detected)
    /// </summary>
    public string? InstallPath { get; init; }
}

/// <summary>
/// Updater configuration
/// </summary>
public sealed class UpdaterConfiguration
{
    /// <summary>
    /// Launcher executable name (e.g., Popup.Updater.Launcher.exe)
    /// </summary>
    public required string LauncherExecutable { get; init; }

    /// <summary>
    /// Temporary download folder
    /// </summary>
    public required string TempDownloadPath { get; init; }

    /// <summary>
    /// Request administrator rights if needed
    /// </summary>
    public bool RequestElevation { get; init; } = true;
}