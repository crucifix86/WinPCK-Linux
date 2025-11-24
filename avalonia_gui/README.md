# WinPCK Avalonia GUI - Linux Edition

A cross-platform GUI for viewing and extracting Perfect World PCK archive files, built with Avalonia UI and .NET 8.

## Features

✅ **Completed Features:**
- Open and validate PCK files
- Display file information (version, file count, size, compression stats)
- Browse PCK file contents with DataGrid view
- Extract all files from PCK archives
- Real-time progress tracking during extraction
- Comprehensive logging system with Serilog
  - Console output for development
  - Rolling file logs in `logs/` directory
  - Native library logging integration
- Modern UI with toolbar, status bar, and progress indicator
- File list with icons, sizes, and compression ratios

## Architecture

### Components

1. **Native/PckNative.cs**: P/Invoke wrapper for the C PCK library
   - Declares all C functions with proper marshaling
   - Defines callbacks for logging and file listing
   - Handles memory safety and pointer conversion

2. **Services/PckService.cs**: Managed wrapper around native library
   - Provides safe, high-level API for C# code
   - Implements logging integration
   - Handles progress reporting
   - Manages file lifecycle

3. **Models/**:
   - `PckFileInfo`: Contains PCK file metadata
   - `PckFileEntry`: Represents a single file/folder in the archive

4. **ViewModels/MainWindowViewModel.cs**: MVVM pattern implementation
   - Observable properties for UI binding
   - RelayCommands for user actions
   - Async/await for responsive UI
   - Log message management

5. **Views/MainWindow.axaml**: Avalonia XAML UI
   - Toolbar with action buttons
   - DataGrid for file listing
   - Resizable log panel with GridSplitter
   - Status bar and progress indicator

## Building and Running

### Prerequisites
- .NET 8.0 SDK
- Linux (tested on Debian-based systems)
- Native PCK library (libpcklib.so)

### Build Steps

```bash
# From avalonia_gui directory
dotnet restore
dotnet build
dotnet run
```

### Native Library

The application requires `libpcklib.so` to be present in the output directory:

```bash
# Build the native library
cd ../build
cmake ..
make

# Create shared library
gcc -shared -o libpcklib.so -Wl,--whole-archive \
    libpcklib.a libzlib_internal.a liblibdeflate_internal.a libbase64_internal.a \
    -Wl,--no-whole-archive -lpthread

# Copy to Avalonia output
cp libpcklib.so ../avalonia_gui/bin/Debug/net8.0/
```

## Logging

The application uses Serilog for comprehensive logging:

- **Console**: Real-time output during development
- **File**: `logs/winpck-YYYYMMDD.log` with daily rolling
- **Levels**: Debug, Information, Warning, Error, Fatal

### Log Output Examples:

```
[16:30:45 INF] WinPCK Avalonia starting up
[16:30:45 INF] MainWindowViewModel initialized
[16:30:45 INF] PckService initialized
[16:30:47 INF] Opening PCK file: /path/to/file.pck
[16:30:47 INF] [Native INFO] PCK file opened successfully
[16:30:47 INF] PCK file opened successfully: Version=Perfect World v2.0, Files=1234, Size=524,288,000 bytes
```

## Usage

### Opening a PCK File

Currently uses a hardcoded test path (`/home/doug/test.pck`) for testing. To test with your own PCK file:

1. Place your PCK file at `/home/doug/test.pck`, or
2. Modify the path in `MainWindowViewModel.cs` line 85:
   ```csharp
   var testPath = "/your/path/to/file.pck";
   ```

### UI Controls

- **📁 Open**: Open a PCK file for browsing
- **❌ Close**: Close the currently open file
- **ℹ️ Info**: Display detailed file information in the log
- **📦 Extract All**: Extract all files to `~/pck_extract/`
- **🗑️ Clear Log**: Clear the log message panel

### File List

The DataGrid displays:
- **Icon**: 📁 for folders, 📄 for files
- **Name**: File or folder name
- **Size**: Uncompressed size (formatted)
- **Compressed**: Compressed size (formatted)
- **Ratio**: Compression ratio percentage

## Testing Functions

The application includes extensive logging to help identify issues:

1. **Startup Logging**: Tracks application initialization
2. **P/Invoke Logging**: All native function calls are logged
3. **Operation Logging**: File operations, extraction progress
4. **Error Logging**: Exceptions with stack traces

### Common Test Scenarios:

```csharp
// Test file opening
await OpenFileCommand.ExecuteAsync(null);

// Test file info display
ShowInfoCommand.Execute(null);

// Test extraction
await ExtractAllCommand.ExecuteAsync(null);
```

## Troubleshooting

### "libpcklib.so not found"
- Ensure the shared library is in the same directory as the executable
- Check `LD_LIBRARY_PATH` includes the output directory
- Use `ldd` to verify dependencies: `ldd libpcklib.so`

### "Failed to open PCK file"
- Check file path is correct
- Verify PCK file is valid (not corrupted)
- Check log output for native library errors

### "No file info available"
- Ensure file was opened successfully first
- Check `IsFileOpen` property in ViewModel

## Future Enhancements

- [ ] File dialog for opening PCK files
- [ ] Context menu for file list (extract selected, preview, etc.)
- [ ] Search/filter functionality
- [ ] Create new PCK files
- [ ] Add files to existing PCK
- [ ] Rename files within PCK
- [ ] Delete files from PCK
- [ ] Compression options dialog
- [ ] Multiple file selection for extraction
- [ ] Preview for text/image files

## Credits

Based on WinPCK by Li Qiufeng (https://github.com/halysondev/WinPCK)
Linux port and Avalonia GUI by Claude Code Assistant
