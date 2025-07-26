# SyncToyNext Architecture Overview

## Core Concepts

SyncToyNext is a cross-platform file synchronization tool designed around several key architectural concepts that provide flexible, robust synchronization capabilities with support for versioning and conflict resolution.

### Repository-Based Synchronization
The application supports two primary synchronization paradigms:

1. **Traditional Profile-Based Sync**: Simple one-way synchronization from source to destination directories or zip files
2. **Repository-Based Sync**: Advanced synchronization with versioning, sync points, and merge capabilities

### SyncPoint System
A **SyncPoint** represents a snapshot of synchronized files at a specific point in time. It contains:
- A unique identifier and timestamp
- A description of the sync operation
- A list of file entries with their paths and synchronization status
- Whether it's a reference point for merging operations

### Repository
A **Repository** manages the relationship between a local working directory and a remote storage location. It provides:
- Version control-like operations (push, pull, restore)
- Sync point management and history
- Conflict detection and merge capabilities
- Support for both directory and zip-based remote storage

### Synchronizers
**Synchronizers** are the core components responsible for the actual file synchronization logic. They implement different strategies for different destination types while maintaining a consistent interface.

---

## Solution Architecture and Layering

SyncToyNext is structured as a multi-project .NET 9 solution with clear separation of concerns:

### Project Structure
SyncToyNext/
├── Stn.Core/          # Core business logic and synchronization engine
├── Stn.Cli/        # Command-line interface and execution modes
├── Stn.Gui/     # GUI application (WinForms with WebView2)
└── Documentation/             # Architecture and usage documentation

### Layer Dependencies

graph TD
    A[Stn.Cli] --> B[Stn.Core]
    C[Stn.Gui] --> B[Stn.Core]
    B --> D[.NET 9 Runtime]
    B --> E[System.IO.Compression]
    B --> F[System.Text.Json]
    C --> G[Microsoft.Web.WebView2]

### Core Layer (`Stn.Core`)

The core layer contains all business logic and is organized into the following namespaces:

- **Configuration**: JSON-based configuration management
- **Models**: Data models and enums (SyncProfile, SyncInterval, OverwriteOption, SyncMode)
- **Synchronizers**: Abstract synchronization implementations
- **Runners**: Execution engines for different operational modes
- **SyncPoints**: Version control and repository management
- **UX**: User interaction abstractions
- **Helpers**: Utility functions and file operations
- **Merging**: Conflict resolution and merge operations

### Client Layer (`Stn.Cli`)

The client layer provides command-line interface and execution coordination:

- **ExecutionModes**: Mode-specific implementations (ProfileMode, RepositoryMode, ManualMode, ServiceMode)
- **Helpers**: CLI-specific utilities (CommandLineArguments, CliHelpers)

### GUI Layer (`Stn.Gui`)

The GUI layer provides a modern web-based interface using WinForms and WebView2:

- **Controls**: Custom WinForms controls including Monaco editor integration
- **Forms**: Main application windows and dialogs

## Incremental SyncPoint Implementation

### Overview

The Repository-based synchronization system implements incremental sync points that only store changes (deltas) between sync operations, similar to Git commits. This approach provides:

- **Storage Efficiency**: Only changed files are tracked per sync point
- **Fast History Reconstruction**: Efficient rollback to any point in time
- **Merge Capability**: Ability to merge changes between repositories
- **Deleted File Tracking**: Explicit tracking of file deletions

### How Incremental SyncPoints Work

#### 1. SyncPoint Structure

Each SyncPoint contains only the files that were **changed, added, or deleted** since the previous sync point:

#### 2. Incremental Sync Process

When creating a new sync point, the system:

1. **Identifies Changes**: Compares current source files with the state at the last sync point
2. **Delta Calculation**: Only files that are different (by timestamp, size, or hash) are included
3. **Deletion Detection**: Files that existed in previous sync points but are missing from source are marked as deleted
4. **Entry Creation**: Creates SyncPointEntry records only for changed files

#### 3. Historical State Reconstruction

The `GetFileEntriesAtSyncpoint()` method reconstructs the complete file state at any sync point by traversing the incremental history:

#### 4. Change Detection Logic

The system uses multiple criteria to determine if a file has changed:

- **Timestamp Comparison**: File modification time (with 2-second tolerance for ZIP archives)
- **Size Comparison**: File size differences
- **Hash Comparison** (Strict Mode): SHA-256 hash verification
- **Existence Check**: File presence/absence

#### 5. Deletion Tracking

Deleted files are explicitly tracked in sync points with `EntryType.Deleted`:

#### 6. Storage Structure

Sync points are stored in a hierarchical directory structure:
RemoteLocation/
├── syncpointroot.json                 # Remote configuration
├── 20241201120000UTC/                 # Sync point directory
│   ├── 20241201120000UTC.syncpoint.json  # Sync point metadata
│   └── backup.zip                     # Actual synchronized files (if zip mode)
├── 20241201130000UTC/
│   ├── 20241201130000UTC.syncpoint.json
│   └── backup.zip
└── 20241201140000UTC/
    ├── 20241201140000UTC.syncpoint.json
    └── backup.zip

### Benefits of Incremental SyncPoints

1. **Efficiency**: Only stores and transfers changed data
2. **Speed**: Fast sync operations that scale with changes, not total file count
3. **History**: Complete version history without full snapshots
4. **Merging**: Enables sophisticated merge operations between repositories
5. **Recovery**: Can restore to any historical state efficiently
6. **Storage**: Minimal storage overhead for tracking changes

This incremental approach makes SyncToyNext suitable for both small personal backups and large-scale synchronization scenarios where bandwidth and storage efficiency are crucial.

---

## Architecture Components

### 1. Core Synchronization Layer

#### Synchronizer Abstract Base Class
- **Role:** Defines the contract for all synchronizer implementations
- **Key Methods:**
  - `FullSynchronization(string sourcePath, SyncPoint? syncPoint, SyncPointManager? syncPointManager)`: Performs complete directory synchronization
  - `SynchronizeFile(string srcFilePath, string relativeOrDestPath, string? oldDestFilePath)`: Synchronizes individual files

#### FileSynchronizer
- **Implements:** `Synchronizer`
- **Role:** Handles directory-to-directory synchronization
- **Features:**
  - File timestamp and size comparison
  - Overwrite policy enforcement
  - Integration with SyncPoint system for versioned sync

#### ZipFileSynchronizer
- **Implements:** `Synchronizer` 
- **Role:** Handles directory-to-zip archive synchronization
- **Features:**
  - ZIP archive creation and updating
  - Entry-level synchronization within archives
  - Compression optimization
  - Integration with SyncPoint system

### 2. SyncPoint Management System

#### SyncPoint
- **Role:** Represents a versioned snapshot of synchronized files
- **Properties:**
  - `SyncPointId`: Unique identifier (typically timestamp-based)
  - `Description`: Human-readable description
  - `LastSyncTime`: When the sync point was created
  - `ReferencePoint`: Whether this is a merge reference point
  - `Entries`: List of synchronized file entries

#### SyncPointEntry
- **Role:** Represents an individual file within a sync point
- **Properties:**
  - `SourcePath`: Relative path in source directory
  - `RelativeRemotePath`: Path in destination/remote location
  - `EntryType`: Status (Added/Changed, Deleted, etc.)

#### SyncPointManager
- **Role:** Manages collections of sync points and their persistence
- **Responsibilities:**
  - Loading and saving sync points from/to storage
  - Tracking sync point history and relationships
  - Providing file state reconstruction at any sync point
  - Managing both zipped and non-zipped remote storage

### 3. Repository System

#### Repository
- **Role:** High-level interface for version-controlled synchronization
- **Key Features:**
  - Initialization of local/remote pairs
  - Cloning from other remote locations
  - Push operations to create new sync points
  - Restore operations to revert to previous sync points
  - Merge conflict detection and resolution

#### RemoteConfig
- **Role:** Configuration management for repository remotes
- **Storage:** JSON-based configuration in `.stn/stn.remote.json`
- **Properties:**
  - `RemotePath`: Location of remote storage
  - `CurrentSyncPoint`: Currently active sync point ID

### 4. Execution Modes and Runners

#### ExecutionContext
- **Role:** Application-wide coordination and lifecycle management
- **Responsibilities:**
  - Configuration loading and validation
  - Profile management for traditional sync
  - FileSystemRunner coordination
  - Graceful shutdown handling

#### ManualRunner
- **Role:** Executes one-time synchronization operations
- **Features:**
  - Support for both directory and zip destinations
  - Optional sync point creation
  - Integration with repository workflows

#### FileSystemRunner
- **Role:** Real-time file system monitoring and synchronization
- **Features:**
  - FileSystemWatcher-based change detection
  - Debounced synchronization triggers
  - Integration with synchronizer implementations
  - Queue management for interval-based sync modes
  - Support for both incremental and full sync modes

### 5. Execution Modes

The application supports multiple execution modes based on command-line arguments:

#### ProfileMode
- **Purpose:** Traditional profile-based synchronization
- **Features:**
  - Single profile execution
  - Continuous task mode with file watching
  - Clean shutdown detection and recovery

#### RepositoryMode
- **Purpose:** Repository-based version-controlled synchronization
- **Operations:**
  - `init`: Initialize a new local/remote repository pair
  - `clone`: Clone from an existing remote repository
  - `push`: Create new sync points
  - `restore`: Restore files from sync points
  - `list`: Display sync point history

#### ManualMode
- **Purpose:** Direct path-to-path synchronization
- **Features:**
  - Simple source-to-destination sync
  - Optional sync point creation
  - Support for both directory and zip destinations

#### ServiceMode
- **Purpose:** Run as system service/daemon
- **Features:**
  - Background execution
  - Integration with system service managers
  - Persistent configuration

### 6. Merge and Conflict Resolution

#### SyncPointMerger
- **Role:** Handles merge operations between repositories
- **Process:**
  1. Create pre-merge sync points for both repositories
  2. Detect and present merge conflicts to user
  3. Allow manual conflict resolution
  4. Perform post-merge synchronization
  5. Create final merged sync points

#### Merge Conflict Detection
- **File-level conflicts:** Same file modified in both repositories
- **Rename conflicts:** Files moved to different locations
- **Delete conflicts:** File deleted in one repository, modified in another

### 7. User Interface Layer

#### UserIO (UX)
- **Role:** Centralized user interaction and messaging
- **Features:**
  - Consistent error and status reporting
  - Cross-platform console output
  - Integration with GUI components

#### Command Line Interface
- **CommandLineArguments:** Robust argument parsing and validation
- **Help system:** Context-aware help and usage information
- **Validation:** Argument combination validation

---

## High-Level Application Flow

### Traditional Profile-Based Sync
1. **Configuration Loading:** Load sync profiles from JSON configuration
2. **Profile Validation:** Validate source/destination paths and settings
3. **Synchronizer Creation:** Instantiate appropriate synchronizers per profile
4. **Watch Mode (Optional):** Start FileSystemRunners for real-time monitoring
5. **Synchronization:** Execute sync operations based on file changes or manual triggers

### Repository-Based Workflow
1. **Repository Initialization:** Set up local working directory and remote storage
2. **Sync Point Creation:** Create versioned snapshots of synchronized files
3. **Change Tracking:** Monitor changes between sync points
4. **Merge Operations:** Handle conflicts when merging between repositories
5. **History Management:** Maintain sync point history for rollback capabilities

### Service/Daemon Mode
1. **Service Registration:** Install as system service
2. **Background Execution:** Run continuously with configuration monitoring
3. **Automatic Recovery:** Handle service restarts and configuration changes
4. **Logging Integration:** System-level logging and monitoring

---

## Configuration System

### Traditional Profile Configuration

```json
  "Profiles": [
    {
      "Id": "ProjectBackup",
      "SourcePath": "C:/Projects/MyProject",
      "DestinationPath": "D:/Backups/MyProject.zip",
      "DestinationIsZip": true,
      "SyncInterval": "Realtime",
      "OverwriteOption": "OnlyOverwriteIfNewer",
      "Mode": "Incremental"
    }
  ]
}
```

### Repository Configuration
- **Local:** `.stn/stn.remote.json` in working directory
- **Remote:** `syncpointroot.json` and individual sync point files
- **Sync Points:** Individual JSON files in timestamped directories

### Configuration Properties

#### Profile Settings
- `Id`: Unique identifier for the profile
- `SourcePath`: Source directory to synchronize
- `DestinationPath`: Destination directory or zip file
- `DestinationIsZip`: Boolean indicating zip file destination
- `SyncInterval`: Synchronization frequency
  - `Realtime`: Immediate sync on file changes
  - `Hourly`: Sync every hour
  - `Daily`: Sync once per day
  - `AtShutdown`: Sync only when application shuts down
- `OverwriteOption`: File overwrite behavior
  - `OnlyOverwriteIfNewer`: Overwrite only if source is newer
  - `AlwaysOverwrite`: Always overwrite destination files
- `Mode`: Synchronization mode
  - `Incremental`: Only sync changed files
  - `FullSync`: Always sync all files

---

## Extensibility and Modularity

### Plugin Architecture
- **Synchronizer Interface:** Easy addition of new destination types
- **Runner Interface:** Support for different monitoring strategies
- **Merge Strategy Interface:** Pluggable conflict resolution algorithms

### Cross-Platform Support
- **File System Abstraction:** Consistent file operations across platforms
- **Path Handling:** Robust cross-platform path management
- **Service Integration:** Platform-specific service/daemon support

### Performance Optimizations
- **Debounced File Watching:** Reduces unnecessary sync operations
- **Incremental Synchronization:** Only sync changed files
- **Memory Management:** Efficient handling of large file sets
- **Parallel Operations:** Multi-threaded synchronization where appropriate

---

## Error Handling and Recovery

### Graceful Degradation
- **Partial Sync Success:** Continue operation even if some files fail
- **Network Resilience:** Handle temporary remote storage unavailability
- **Conflict Resolution:** User-guided merge conflict resolution

### Recovery Mechanisms
- **Clean Shutdown Detection:** Detect improper application termination
- **State Reconstruction:** Rebuild sync state from persisted information
- **Rollback Capabilities:** Restore to previous sync points on failure

### Logging and Monitoring
- **Centralized Logging:** Consistent logging across all components
- **Performance Metrics:** Track sync operation statistics
- **Error Reporting:** Detailed error information for troubleshooting

---

## Security and Integrity

### File Integrity
- **Checksum Validation:** Optional SHA-256 hash comparison in strict mode
- **Timestamp Verification:** Consistent file modification time handling
- **Corruption Detection:** Identify and handle corrupted files

### Access Control
- **Permission Handling:** Respect file system permissions
- **Safe Operations:** Atomic file operations where possible
- **Backup Safety:** Never corrupt existing backups

---

## Summary

SyncToyNext's architecture provides a comprehensive file synchronization solution that scales from simple directory mirroring to complex version-controlled repository management. The modular design enables easy extension while maintaining robustness and reliability across different platforms and usage scenarios.

Key architectural strengths:
- **Separation of Concerns:** Clear boundaries between synchronization logic, version control, and user interfaces
- **Extensibility:** Plugin-based architecture for new features and destination types  
- **Reliability:** Comprehensive error handling and recovery mechanisms
- **Flexibility:** Support for multiple synchronization paradigms and execution modes
- **Cross-Platform:** Consistent operation across Windows, Linux, and macOS
- **Efficiency:** Incremental sync points minimize storage and transfer overhead
- **Versioning:** Complete history tracking with point-in-time recovery capabilities
