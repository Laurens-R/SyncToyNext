# Debugging SyncToyNext

This guide will help you get started with debugging SyncToyNext, including how to set up a debugging configuration file and useful tips for troubleshooting.

## 1. Setting Up a Debugging Configuration File

To avoid modifying your production configuration, create a dedicated test or debug config file. For example:

```
c:\Code\SyncToyNext\SyncToyNext.test.config.json
```

Example content:
```json
{
  "Profiles": [
    {
      "Id": "DebugProfile",
      "SourcePath": "C:/TestData/DebugSource",
      "DestinationPath": "C:/TestData/DebugDest",
      "DestinationIsZip": false,
      "SyncInterval": "Realtime",
      "OverwriteOption": "OnlyOverwriteIfNewer"
    }
  ]
}
```
- Make sure the `SourcePath` and `DestinationPath` directories exist before running.
- You can add or remove profiles as needed for your debugging scenario.

## 2. Launching in Debug Mode

- In Visual Studio or VS Code, set the working directory to the project root.
- Pass the debug config file as a command line argument:
  - Example: `--config SyncToyNext.test.config.json`
- You can also use other flags, such as `--strict` or `--recover`, for specific debugging scenarios.

## 3. Useful Debugging Tips

- **Breakpoints:** Set breakpoints in the synchronizer, watcher, or context classes to inspect sync logic.
- **Logging:** Check the log files generated in the source directory for detailed sync actions and errors.
- **Validation:** If the app exits with a validation error, check your config file for missing or incorrect fields (Id, SourcePath, DestinationPath, etc.).
- **Service Conflicts:** On Windows, ensure the SyncToyNext service is not running when debugging the command-line version. The app will attempt to stop the service automatically, but will exit if it cannot.
- **Missing Config:** If no config file is found, the app will print a helpful error and exit. Always specify a config file or place one at the default location.

## 4. Example VS Code launch.json

Add this to your `.vscode/launch.json` for easy debugging:
```json
{
  "version": "0.2.0",
  "configurations": [
    {
      "name": "Debug SyncToyNext",
      "type": "coreclr",
      "request": "launch",
      "program": "${workspaceFolder}/SyncToyNext.Client/bin/Debug/net9.0/win-x64/SyncToyNext.Client.exe",
      "args": ["--config", "<yourdebugconfighere>.json"],
      "cwd": "${workspaceFolder}",
      "console": "internalConsole"
    }
  ]
}
```

## 5. Troubleshooting

- **Profile Validation:** The app will exit with a clear error if any profile is missing required fields, has a duplicate Id, or references non-existent paths.
- **File/Folder Permissions:** Ensure you have read/write permissions for all source and destination paths.
- **Service Mode:** If running as a service, logs and errors may appear in the Windows Event Viewer or system logs.

For more details, see the main README.md and ARCHITECTURE.md files.
