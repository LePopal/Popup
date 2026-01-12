using System;
using System.Collections.Generic;
using System.Text;

using System.Runtime.InteropServices;
using Popup.Updater.Core.Interfaces;

namespace Popup.Updater.Core.Services;

/// <summary>
/// Platform-specific operations helper implementation
/// </summary>
public sealed class PlatformHelper : IPlatformHelper
{
    /// <inheritdoc />
    public string GetPlatform()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return "win";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return "osx";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            return "linux";

        throw new PlatformNotSupportedException("Unsupported platform");
    }

    /// <inheritdoc />
    public string GetArchitecture()
    {
        return RuntimeInformation.ProcessArchitecture switch
        {
            Architecture.X64 => "x64",
            Architecture.Arm64 => "arm64",
            Architecture.X86 => "x86",
            Architecture.Arm => "arm",
            _ => throw new PlatformNotSupportedException($"Unsupported architecture: {RuntimeInformation.ProcessArchitecture}")
        };
    }

    /// <inheritdoc />
    public bool NeedsElevation(string installPath)
    {
        try
        {
            // Try to create a test file in the install directory
            var testFile = Path.Combine(installPath, $".write_test_{Guid.NewGuid()}.tmp");
            File.WriteAllText(testFile, "test");
            File.Delete(testFile);
            return false;
        }
        catch
        {
            return true;
        }
    }

    /// <inheritdoc />
    public bool RequestElevation(string executablePath, string arguments)
    {
        try
        {
            var startInfo = GetPlatform() switch
            {
                "win" => new System.Diagnostics.ProcessStartInfo
                {
                    FileName = executablePath,
                    Arguments = arguments,
                    UseShellExecute = true,
                    Verb = "runas" // Request UAC elevation
                },
                "osx" => new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "osascript",
                    Arguments = $"-e 'do shell script \"{executablePath} {arguments}\" with administrator privileges'",
                    UseShellExecute = false
                },
                "linux" => new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "pkexec",
                    Arguments = $"{executablePath} {arguments}",
                    UseShellExecute = false
                },
                _ => throw new PlatformNotSupportedException($"Unsupported platform: {GetPlatform()}")
            };

            System.Diagnostics.Process.Start(startInfo);
            return true;
        }
        catch
        {
            return false;
        }
    }
}