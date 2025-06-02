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

## Getting Started

### Prerequisites
- [.NET 9.0 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/9.0)
- Windows (for service mode); Linux/Mac supported for console mode

### Building the Project

Open a terminal in the project root and run:

```
dotnet build SyncToyNext.slnx
```

### Running the Console Application

```
dotnet run --project SyncToyNext.Client [--config <configfile.json>] [--service]
```

#### Command Line Arguments
- `--config <file>`: Path to a custom JSON configuration file. If omitted, the default config in the application directory is used.
- `--service`: Run as a Windows service (Windows only). If omitted, runs as a console app.

#### Console Controls
- When running as a console app, press `q` to quit and gracefully shut down all watchers and sync operations.

### Running as a Windows Service
1. Build the project and install the service using standard Windows service management tools (e.g., `sc.exe` or PowerShell).
2. Pass the `--config` argument if you want to use a custom configuration file.

### Configuration
The configuration is stored in a JSON file (default: `SyncToyNext.config.json` in the app directory). It contains an array of sync profiles. Example:

```json
{
  "Profiles": [
    {
      "Id": "DocumentsBackup",
      "SourcePath": "C:/Users/John/Documents",
      "DestinationPath": "D:/Backups/Documents.zip",
      "DestinationIsZip": true
    },
    {
      "Id": "PhotosSync",
      "SourcePath": "C:/Users/John/Pictures",
      "DestinationPath": "//NAS/PhotosBackup",
      "DestinationIsZip": false
    }
  ]
}
```

- `Id`: Unique name for the sync profile
- `SourcePath`: Path to the source directory
- `DestinationPath`: Path to the destination directory or zip file
- `DestinationIsZip`: Set to `true` to sync into a zip file, `false` for a regular folder

### Logging
- All sync activity and errors are logged to both the console and a log file in the source directory (e.g., `SyncToyNext_YYYYMMDD.log`).

## License

This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for details.

## Contributing

Contributions, bug reports, and feature requests are welcome! Please open an issue or submit a pull request.

## Authors

- Laurens (2025)

---

For more information, see the source code and comments in each class.
