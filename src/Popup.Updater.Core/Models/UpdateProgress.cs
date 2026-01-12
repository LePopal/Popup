using System;
using System.Collections.Generic;
using System.Text;

namespace Popup.Updater.Core.Models;

/// <summary>
/// Download/installation progress information
/// </summary>
public sealed class UpdateProgress
{
    /// <summary>
    /// Progress percentage (0-100)
    /// </summary>
    public int Percentage { get; init; }

    /// <summary>
    /// Downloaded bytes
    /// </summary>
    public long BytesDownloaded { get; init; }

    /// <summary>
    /// Total size in bytes
    /// </summary>
    public long TotalBytes { get; init; }

    /// <summary>
    /// Current step (Checking, Downloading, Installing, etc.)
    /// </summary>
    public required UpdateStep CurrentStep { get; init; }

    /// <summary>
    /// Descriptive message
    /// </summary>
    public string? Message { get; init; }
}

/// <summary>
/// Update process steps
/// </summary>
public enum UpdateStep
{
    Checking,
    Downloading,
    Extracting,
    Installing,
    Restarting,
    Completed,
    Failed
}