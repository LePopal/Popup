using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.Extensions.Logging;
using Octokit;
using Popup.Updater.Core.Configuration;
using Popup.Updater.Core.Interfaces;
using Popup.Updater.Core.Models;

namespace Popup.Updater.Core.Services;

/// <summary>
/// GitHub-based update source implementation
/// </summary>
public sealed class GitHubUpdateSource : IUpdateSource
{
    private readonly UpdateSourceConfiguration _config;
    private readonly IPlatformHelper _platformHelper;
    private readonly ILogger<GitHubUpdateSource> _logger;
    private readonly GitHubClient _githubClient;

    public GitHubUpdateSource(
        UpdateSourceConfiguration config,
        IPlatformHelper platformHelper,
        ILogger<GitHubUpdateSource> logger)
    {
        _config = config;
        _platformHelper = platformHelper;
        _logger = logger;

        _githubClient = new GitHubClient(new ProductHeaderValue("Popup-Updater"));
    }

    /// <inheritdoc />
    public async Task<UpdateInfo?> CheckForUpdateAsync(string currentVersion, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Checking for updates on GitHub: {Owner}/{Repository}", _config.Owner, _config.Repository);

            // Get all releases
            var releases = await _githubClient.Repository.Release.GetAll(_config.Owner, _config.Repository);

            // Filter based on pre-release preference
            var eligibleReleases = releases.Where(r => _config.PreRelease || !r.Prerelease).ToList();

            if (!eligibleReleases.Any())
            {
                _logger.LogInformation("No releases found");
                return null;
            }

            // Get the latest release
            var latestRelease = eligibleReleases.First();
            var latestVersion = CleanVersion(latestRelease.TagName);

            _logger.LogInformation("Latest version: {LatestVersion}, Current version: {CurrentVersion}", latestVersion, currentVersion);

            // Compare versions
            if (!IsNewerVersion(currentVersion, latestVersion))
            {
                _logger.LogInformation("Already up to date");
                return null;
            }

            // Find the appropriate asset for the current platform
            var platform = _platformHelper.GetPlatform();
            var architecture = _platformHelper.GetArchitecture();
            var assetName = BuildAssetName(latestVersion, platform, architecture);

            var asset = latestRelease.Assets.FirstOrDefault(a =>
                a.Name.Equals(assetName, StringComparison.OrdinalIgnoreCase));

            if (asset == null)
            {
                _logger.LogWarning("No asset found for platform {Platform}-{Architecture}", platform, architecture);
                return null;
            }

            _logger.LogInformation("Update available: {Version}", latestVersion);

            return new UpdateInfo
            {
                Version = latestVersion,
                DownloadUrl = asset.BrowserDownloadUrl,
                FileSize = asset.Size,
                ReleaseNotes = latestRelease.Body,
                PublishedAt = latestRelease.PublishedAt ?? latestRelease.CreatedAt,
                IsPreRelease = latestRelease.Prerelease
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking for updates");
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<string> DownloadUpdateAsync(
        UpdateInfo updateInfo,
        string destinationPath,
        IProgress<UpdateProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Downloading update from {Url}", updateInfo.DownloadUrl);

            // Ensure destination directory exists
            var directory = Path.GetDirectoryName(destinationPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            using var httpClient = new HttpClient();
            using var response = await httpClient.GetAsync(updateInfo.DownloadUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            response.EnsureSuccessStatusCode();

            var totalBytes = response.Content.Headers.ContentLength ?? updateInfo.FileSize;

            using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var fileStream = new FileStream(destinationPath, System.IO.FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);

            var buffer = new byte[8192];
            long totalBytesRead = 0;
            int bytesRead;

            while ((bytesRead = await contentStream.ReadAsync(buffer, cancellationToken)) > 0)
            {
                await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
                totalBytesRead += bytesRead;

                progress?.Report(new UpdateProgress
                {
                    Percentage = (int)((totalBytesRead * 100) / totalBytes),
                    BytesDownloaded = totalBytesRead,
                    TotalBytes = totalBytes,
                    CurrentStep = UpdateStep.Downloading,
                    Message = $"Downloading: {totalBytesRead / 1024 / 1024} MB / {totalBytes / 1024 / 1024} MB"
                });
            }

            _logger.LogInformation("Download completed: {Path}", destinationPath);
            return destinationPath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading update");
            throw;
        }
    }

    private string BuildAssetName(string version, string platform, string architecture)
    {
        return _config.AssetNamePattern
            .Replace("{AppName}", "Popup", StringComparison.OrdinalIgnoreCase)
            .Replace("{Version}", version, StringComparison.OrdinalIgnoreCase)
            .Replace("{Platform}", platform, StringComparison.OrdinalIgnoreCase)
            .Replace("{Architecture}", architecture, StringComparison.OrdinalIgnoreCase);
    }

    private static string CleanVersion(string version)
    {
        // Remove 'v' prefix if present
        return version.TrimStart('v', 'V');
    }

    private static bool IsNewerVersion(string currentVersion, string latestVersion)
    {
        var current = Version.Parse(CleanVersion(currentVersion));
        var latest = Version.Parse(CleanVersion(latestVersion));
        return latest > current;
    }
}