# ⚠️ Disclaimer: Project Under Development

**This project is still heavily under development. Use at your own risk! The author takes no responsibility for any damages, data loss, or other issues caused by using this project in any environment.**

# SyncToyNext

SyncToyNext is a flexible, cross-platform file and folder synchronization tool for .NET. It supports local and network (UNC/SAMBA) paths, and can synchronize to both regular directories and zip files. SyncToyNext can run as a console application or as a Windows service, making it suitable for both interactive and background/service scenarios.

## Features
- Synchronize files and folders between any two locations (including network shares)
- Supports recursive sync, folder structure preservation, and all file types
- Configurable overwrite behavior: Never, Only if newer, or Always
- Sync to regular folders or directly into zip archives
- Watch for changes and auto-sync using FileSystemWatcher
- Manual sync trigger for any profile or arbitrary source/destination
- JSON-based configuration with support for multiple sync profiles
- Centralized logging to both file and console
- Runs as a console app or Windows service
- Produces a standalone, self-contained executable (no .NET runtime required)

## Getting Started

### Prerequisites
- Windows (for service mode or standalone executable)
- [.NET 9.0 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/9.0) (only required for building from source)

### Building the Project

Open a terminal in the project root and run:

```
dotnet build SyncToyNext.slnx
```

### Publishing a Standalone Executable

To create a single-file, self-contained executable (no .NET runtime required):

```
dotnet publish SyncToyNext.Client -c Release
```

The executable will be in:
```
SyncToyNext.Client/bin/Release/net9.0/win-x64/publish/
```

### Running the Console Application

```
dotnet run --project SyncToyNext.Client [--config <configfile.json>] [--service]
```
Or, if using the published standalone executable:
```
SyncToyNext.Client.exe [--config <configfile.json>] [--service]
```

#### Command Line Arguments
- `--config <file>`: Path to a custom JSON configuration file. If omitted, the default config in the application directory is used.
- `--service`: Run as a Windows service (Windows only). If omitted, runs as a console app.
- `--strict`: Enable strict file integrity checking (SHA-256 hash comparison).
- `--recover`: Force a full sync on startup, as if the previous shutdown was unclean (useful for recovery or manual override).

### Service Coordination (Windows)

When running the tool manually from the command line on Windows, SyncToyNext will automatically check if the SyncToyNext Windows service is running. If it is, the service will be gracefully stopped before the manual sync begins. If the service cannot be stopped, the application will exit to prevent conflicting sync actions. After the manual sync completes, the service will be restarted if it was running before.

#### Console Controls
- When running as a console app, press `q` to quit and gracefully shut down all watchers and sync operations.

### Running as a Windows Service
1. Build and publish the project as above.
2. Install the service using standard Windows service management tools (e.g., `sc.exe create SyncToyNext binPath= "C:\Path\To\SyncToyNext.Client.exe --service [--config <configfile.json>]"` or PowerShell).
3. Start the service from the Services control panel or with `sc start SyncToyNext`.

### Running as a Linux Systemd Service

1. Publish the app as a self-contained executable for Linux:

```
dotnet publish SyncToyNext.Client -c Release -r linux-x64 --self-contained true
```

2. Copy the published files to your target Linux machine, e.g. `/opt/synctoynext/`.

3. Create a systemd service file, e.g. `/etc/systemd/system/synctoynext.service`:

```
[Unit]
Description=SyncToyNext File Synchronization Service
After=network.target

[Service]
Type=simple
ExecStart=/opt/synctoynext/SyncToyNext.Client --config /opt/synctoynext/SyncToyNext.config.json
WorkingDirectory=/opt/synctoynext/
Restart=on-failure
User=synctoynext

[Install]
WantedBy=multi-user.target
```

4. Reload systemd and enable/start the service:

```
sudo systemctl daemon-reload
sudo systemctl enable synctoynext
sudo systemctl start synctoynext
```

- The service will now run in the background and start automatically on boot.
- Logs can be viewed with `journalctl -u synctoynext`.

### Running as a macOS Launchd Service

1. Publish the app as a self-contained executable for macOS:

```
dotnet publish SyncToyNext.Client -c Release -r osx-x64 --self-contained true
```
(or use `osx-arm64` for Apple Silicon)

2. Copy the published files to your target Mac, e.g. `/usr/local/synctoynext/`.

3. Create a launchd plist file, e.g. `/Library/LaunchDaemons/com.synctoynext.service.plist`:

```
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
    <key>Label</key>
    <string>com.synctoynext.service</string>
    <key>ProgramArguments</key>
    <array>
        <string>/usr/local/synctoynext/SyncToyNext.Client</string>
        <string>--config</string>
        <string>/usr/local/synctoynext/SyncToyNext.config.json</string>
    </array>
    <key>RunAtLoad</key>
    <true/>
    <key>KeepAlive</key>
    <true/>
    <key>WorkingDirectory</key>
    <string>/usr/local/synctoynext/</string>
    <key>StandardOutPath</key>
    <string>/usr/local/synctoynext/synctoynext.log</string>
    <key>StandardErrorPath</key>
    <string>/usr/local/synctoynext/synctoynext.err.log</string>
</dict>
</plist>
```

4. Load and start the service:

```
sudo launchctl load /Library/LaunchDaemons/com.synctoynext.service.plist
sudo launchctl start com.synctoynext.service
```

- The service will now run in the background and start automatically on boot.
- Logs can be viewed in the specified log files.

### Running with Docker

You can run SyncToyNext as a Docker container on Linux. This is useful for server or NAS environments.

#### Build the Docker image

```
docker build -t synctoynext .
```

#### Run the container

Mount a host directory for configuration and data:

```
docker run --rm -v /path/to/config:/data \
  -v /path/to/source:/source \
  -v /path/to/destination:/destination \
  synctoynext --config /data/SyncToyNext.config.json
```

- Replace `/path/to/config`, `/path/to/source`, and `/path/to/destination` with your actual host paths.
- The `--config` argument should point to the config file inside the container (e.g., `/data/SyncToyNext.config.json`).
- You can mount as many volumes as needed for your sync profiles.

#### Notes
- The container runs as a foreground process and can be managed with Docker tools.
- Logs are written to the console and to log files in the source directory (if mounted).
- You can pass any supported command line arguments to the container.

## Configuration

The configuration is stored in a JSON file (default: `SyncToyNext.config.json` in the app directory). It contains an array of sync profiles. Example:

```json
{
  "Profiles": [
    {
      "Id": "DocumentsBackup",
      "SourcePath": "C:/Users/John/Documents",
      "DestinationPath": "D:/Backups/Documents.zip",
      "DestinationIsZip": true,
      "SyncInterval": "Realtime",
      "OverwriteOption": "OnlyOverwriteIfNewer"
    },
    {
      "Id": "PhotosSync",
      "SourcePath": "C:/Users/John/Pictures",
      "DestinationPath": "//NAS/PhotosBackup",
      "DestinationIsZip": false,
      "SyncInterval": "Hourly",
      "OverwriteOption": "AlwaysOverwrite"
    },
    {
      "Id": "LinuxToZip",
      "SourcePath": "/data/source",
      "DestinationPath": "/data/backups/source-backup.zip",
      "DestinationIsZip": true,
      "SyncInterval": "Daily"
      // OverwriteOption omitted, will default to OnlyOverwriteIfNewer
    },
    {
      "Id": "NetworkShareToFolder",
      "SourcePath": "/mnt/nas/share",
      "DestinationPath": "/data/localcopy",
      "DestinationIsZip": false,
      "SyncInterval": "AtShutdown",
      "OverwriteOption": "OnlyOverwriteIfNewer"
    }
  ]
}
```

- `Id`: Unique name for the sync profile
- `SourcePath`: Path to the source directory
- `DestinationPath`: Path to the destination directory or zip file
- `DestinationIsZip`: Set to `true` to sync into a zip file, `false` for a regular folder
- `SyncInterval`: When to synchronize this profile. See below for options.
- `OverwriteOption`: Controls when files in the destination are overwritten. Options:
  - `OnlyOverwriteIfNewer` (default): Only overwrite if the source file is newer, or if file size/hash differs (see strict mode).
  - `AlwaysOverwrite`: Always overwrite the destination file, regardless of timestamps, size, or hash.

#### OverwriteOption Details

- If omitted, `OverwriteOption` defaults to `OnlyOverwriteIfNewer` for backward compatibility.
- Use `AlwaysOverwrite` for scenarios where you want to guarantee the destination always matches the source, even if timestamps or sizes are identical.
- Use `OnlyOverwriteIfNewer` for typical backup/mirror scenarios to avoid unnecessary writes.

#### Profile Validation

- Each profile must have a non-empty `Id`, `SourcePath`, and `DestinationPath`.
- All `Id` values must be unique.
- `SourcePath` must exist as a directory.
- `DestinationPath` must exist as a directory (or, for zip destinations, its parent directory must exist).
- If any of these checks fail, the application will exit with a clear error message explaining what needs to be corrected.

#### Missing Config File

- If no config file is provided and none is found at the default location, the application will print a helpful error and exit.

### SyncInterval Options

- `Realtime`: Synchronize immediately when a file change is detected (default).
- `Hourly`: Synchronize all detected changes once per hour.
- `Daily`: Synchronize all detected changes once per day.
- `AtShutdown`: Only synchronize pending changes when the application is shutting down.

Choose the interval that best fits your use case. For example, use `Realtime` for fast mirroring, or `AtShutdown` for batch-style syncs at the end of a session.

```jsonc
{
  "Profiles": [
    {
      "Id": "DocumentsBackup",
      "SourcePath": "C:/Users/John/Documents",
      "DestinationPath": "D:/Backups/Documents.zip",
      "DestinationIsZip": true,
      "SyncInterval": "Realtime",
      "OverwriteOption": "OnlyOverwriteIfNewer"
    },
    // ... more profiles ...
  ]
}
```


#### Profile Options

- `Id` (string, required): Unique name for the sync profile.
- `SourcePath` (string, required): Path to the source directory.
- `DestinationPath` (string, required): Path to the destination directory or zip file.
- `DestinationIsZip` (bool, required): Set to `true` to sync into a zip file, `false` for a regular folder.
- `Mode` (enum, optional): Synchronization mode. Determines how files are compared and copied. Options (case-insensitive):
  - `Incremental` (default): Only new or changed files are copied from source to destination.
  - `Full`: All files from the source are copied to the destination, overwriting existing files according to the OverwriteOption.
- `SyncInterval` (enum, required): When to synchronize this profile. Options (case-insensitive):
  - `Realtime` (default): Sync immediately on file change.
  - `Hourly`: Sync all detected changes once per hour.
  - `Daily`: Sync all detected changes once per day.
  - `AtShutdown`: Only sync pending changes when the app is shutting down.
- `OverwriteOption` (enum, optional): Controls when files in the destination are overwritten. Options (case-insensitive):
  - `OnlyOverwriteIfNewer` (default): Only overwrite if the source file is newer, or if file size/hash differs (see strict mode).
  - `AlwaysOverwrite`: Always overwrite the destination file, regardless of timestamps, size, or hash.

#### Additional Features & Notes

- **Case-insensitive enums:** `SyncInterval` and `OverwriteOption` values are case-insensitive (e.g., `realtime`, `REALTIME`, `Realtime` are all valid).
- **Strict file integrity:** Use the `--strict` command line flag to enable SHA-256 hash comparison for all files (not just timestamps/size).
- **Log file exclusion:** All log files are written to a dedicated `synclogs` subfolder next to your config or source directory, and are automatically excluded from sync operations to prevent sync loops.
- **Service/CLI coordination:** On Windows, running the CLI will pause the service to prevent conflicts, and resume it after manual sync completes.
- **Graceful shutdown:** The app ensures all sync operations and file watchers are stopped cleanly on exit (including Ctrl+C or `q` in console mode).

#### Profile Validation

- Each profile must have a non-empty `Id`, `SourcePath`, and `DestinationPath`.
- All `Id` values must be unique.
- `SourcePath` must exist as a directory.
- `DestinationPath` must exist as a directory (or, for zip destinations, its parent directory must exist).
- If any of these checks fail, the application will exit with a clear error message explaining what needs to be corrected.

#### Missing Config File

- If no config file is provided and none is found at the default location, the application will print a helpful error and exit.


## Troubleshooting

- Ensure all file and folder paths in your config are accessible and have the correct permissions.
- Check the logs for any error messages or clues.
- For Windows service issues, use the Event Viewer to check for application errors.
- For Linux, check the status with `systemctl status synctoynext` and view logs with `journalctl -u synctoynext`.
- For macOS, check the specified log files or use `log show --predicate 'process == "SyncToyNext.Client"' --info` to view logs.
