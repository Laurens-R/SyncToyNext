# Debugging SyncToyNext

This guide explains how to debug SyncToyNext using both Visual Studio Code and Visual Studio 2022. It covers configuration, launch settings, and troubleshooting tips for both environments.

---

## 1. Preparing a Debug Configuration

To avoid modifying your production configuration, create a dedicated test config file, for example:
C:\Code\SyncToyNext\SyncToyNext.test.config.json
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

- Ensure the `SourcePath` and `DestinationPath` directories exist before running.
- You can add or remove profiles as needed for your debugging scenario.

---

## 2. Debugging in Visual Studio Code

1. **Open the Workspace**  
   Open the root folder (`C:\Code\SyncToyNext`) in VS Code.

2. **Configure launch.json**  
   Add or update `.vscode/launch.json` with:
```json
{
  "version": "0.2.0",
  "configurations": [
    {
      "name": "Debug SyncToyNext",
      "type": "coreclr",
      "request": "launch",
      "program": "${workspaceFolder}/SyncToyNext.Client/bin/Debug/net9.0/win-x64/SyncToyNext.Client.exe",
      "args": ["--config", "SyncToyNext.test.config.json"],
      "cwd": "${workspaceFolder}",
      "console": "internalConsole"
    }
  ]
}   
```

- Adjust the `program` path if you are targeting a different build configuration or platform.

3. **Set Breakpoints**  
   Open any source file and click in the gutter to set breakpoints.

4. **Start Debugging**  
   Press `F5` or select "Debug SyncToyNext" from the Run and Debug panel.

---

## 3. Debugging in Visual Studio 2022

1. **Open the Solution**  
   Open `SyncToyNext.sln` in Visual Studio 2022.

2. **Set Startup Project**  
   Right-click `SyncToyNext.Client` and select "Set as Startup Project".

3. **Set Command Line Arguments**  
   - Right-click the `SyncToyNext.Client` project > Properties.
   - Go to the "Debug" tab.
   - Set `--config SyncToyNext.test.config.json` as the Application arguments.
   - Ensure the working directory is set to the solution root or where your config file is located.

4. **Set Breakpoints**  
   Click in the margin next to any line of code to set breakpoints.

5. **Start Debugging**  
   Press `F5` or click "Start Debugging".

---

## 4. Useful Debugging Tips

- **Breakpoints:** Set breakpoints in synchronizer, watcher, or context classes to inspect sync logic.
- **Logging:** Check log files generated in the source directory for detailed sync actions and errors.
- **Validation:** If the app exits with a validation error, check your config file for missing or incorrect fields.
- **Service Conflicts:** On Windows, ensure the SyncToyNext service is not running when debugging the command-line version. The app will attempt to stop the service automatically, but will exit if it cannot.
- **Missing Config:** If no config file is found, the app will print a helpful error and exit. Always specify a config file or place one at the default location.

---

## 5. Troubleshooting

- **Profile Validation:** The app will exit with a clear error if any profile is missing required fields, has a duplicate Id, or references non-existent paths.
- **File/Folder Permissions:** Ensure you have read/write permissions for all source and destination paths.
- **Service Mode:** If running as a service, logs and errors may appear in the Windows Event Viewer or system logs.
- **Clean Shutdown:** If the app did not shut down cleanly, it may trigger a full sync on next start.

---

For more details, see the main README.md and ARCHITECTURE.md files.
