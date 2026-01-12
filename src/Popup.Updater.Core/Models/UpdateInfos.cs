using System;
using System.Collections.Generic;
using System.Text;

namespace Popup.Updater.Core.Models;

/// <summary>
/// Information about an available update
/// </summary>
public sealed class UpdateInfo
{
    /// <summary>
    /// Available version
    /// </summary>
    public required string Version { get; init; }

    /// <summary>
    /// Package download URL
    /// </summary>
    public required string DownloadUrl { get; init; }

    /// <summary>
    /// File size in bytes
    /// </summary>
    public long FileSize { get; init; }

    /// <summary>
    /// Release notes (changelog)
    /// </summary>
    public string? ReleaseNotes { get; init; }

    /// <summary>
    /// Publication date
    /// </summary>
    public DateTimeOffset PublishedAt { get; init; }

    /// <summary>
    /// Is this a pre-release (beta, alpha, etc.)
    /// </summary>
    public bool IsPreRelease { get; init; }
}