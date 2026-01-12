using System.Runtime.InteropServices;
using System.Security.Principal;

namespace Popup.Updater.Launcher.Platform;

/// <summary>
/// Windows-specific elevation helper
/// </summary>
public static class WindowsElevationHelper
{
    /// <summary>
    /// Check if the current process is running with administrator privileges
    /// </summary>
    public static bool IsRunningAsAdministrator()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return false;

        try
        {
            using var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
        catch
        {
            return false;
        }
    }
}