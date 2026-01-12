# Popup
Platform for Orchestrating POPAL Updates &amp; Patching.

A modern .NET 10 application with built-in auto-update functionality via GitHub Releases.

## Support This Project

If you find this project useful, consider supporting its development:

[![PayPal](https://img.shields.io/badge/PayPal-Donate-blue.svg?logo=paypal)](https://www.paypal.me/AxelPironio)

Your support helps maintain and improve this project. Thank you!

## Features

- **Multi-platform**: Windows, macOS, Linux
- **Auto-update**: Automatic updates via GitHub Releases
- **Reusable updater**: `Popup.Updater.Core` can be used in other projects
- **Elevation handling**: Automatic privilege elevation when needed
- **Structured logging**: Serilog for comprehensive logs

## Project Structure
```
Popup/
├── src/
│   ├── Popup.Updater.Core/         # Reusable update library
│   ├── Popup.Updater.Launcher/     # Update executor (separate process)
│   └── Popup/                      # Main application
└── tests/
    └── Popup.Updater.Core.Tests/   # Unit tests
```

## Getting Started

### Prerequisites

- .NET 10 SDK
- Visual Studio 2026 or later

### Building
```bash
dotnet restore
dotnet build
```

### Running
```bash
dotnet run --project src/Popup/Popup.csproj
```

## Configuration

The updater is configured via `updateconfig.json`:
```json
{
  "updateSource": {
    "type": "GitHub",
    "owner": "your-github-username",
    "repository": "popup",
    "preRelease": false,
    "assetNamePattern": "Popup-v{Version}-{Platform}-{Architecture}.zip"
  },
  "application": {
    "name": "Popup",
    "executableName": "Popup.exe",
    "currentVersion": "1.0.0",
    "installPath": null
  },
  "updater": {
    "launcherExecutable": "Popup.Updater.Launcher.exe",
    "tempDownloadPath": "./temp_updates",
    "requestElevation": true
  }
}
```

## Using the Updater in Your Own Project

### 1. Install the NuGet package (once published)
```bash
dotnet add package Popup.Updater.Core
```

### 2. Configure and use
```csharp
using Popup.Updater.Core;
using Popup.Updater.Core.Services;

// Load configuration
var config = LoadConfiguration();

// Setup services
var platformHelper = new PlatformHelper();
var updateSource = new GitHubUpdateSource(config.UpdateSource, platformHelper, logger);
var updateChecker = new UpdateChecker(config, updateSource, logger);

// Check for updates
var updateInfo = await updateChecker.IsThereAnUpdateAsync();

if (updateInfo != null)
{
    // Show update prompt to user
    var updateLauncher = new UpdateLauncher(config, updateSource, platformHelper, logger);
    await updateLauncher.StartUpdateAsync(updateInfo, progress);
}
```

## Release Process

### Creating a Release

Releases are automatically created via GitHub Actions when you push a tag:
```bash
git tag v1.0.0
git push origin v1.0.0
```

### Asset Naming Convention

Release assets must follow this pattern:
- Windows: `Popup-v1.0.0-win-x64.zip`
- macOS Intel: `Popup-v1.0.0-osx-x64.zip`
- macOS Apple Silicon: `Popup-v1.0.0-osx-arm64.zip`
- Linux: `Popup-v1.0.0-linux-x64.zip`

Each ZIP must contain:
- The application executable
- `Popup.Updater.Launcher.exe` (or equivalent)
- All dependencies
- `updateconfig.json`

## Logs

Logs are stored in:
- **Application**: `./Logs/popup-{date}.log`
- **Updater**: `%TEMP%/Popup/Logs/updater-{date}.log` (Windows) or `/tmp/Popup/Logs/updater-{date}.log` (Unix)

## Security

- Updates are downloaded over HTTPS
- Asset URLs come directly from GitHub API
- Optional signature verification (TODO)

## Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Submit a pull request

## License

GPL-3.0 license - see LICENSE file for details

## Roadmap

- [ ] Digital signature verification for updates
- [ ] Delta updates (only download changed files)
- [ ] Custom update sources (not just GitHub)
- [ ] UI progress window for updates
- [ ] Rollback capability