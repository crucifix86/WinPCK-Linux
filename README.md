# WinPCK - Linux Edition

Perfect World Company game PCK file compressed package viewer and editor, now fully ported to Linux!

## Features

### Supported Games
- Zhu Xian (诛仙)
- Perfect World (完美世界)
- Saint Seiya (圣斗士星矢)
- Swordsman (武侠世界)
- Gods and Demons (神魔大陆)
- Hot Dance Party (热舞派对)
- Pocket Westward Journey (口袋西游)

### File Format Support
- `.pck` - Standard PCK archives
- `.cup` - Compressed/encrypted PCK files (auto-decoded)
- `.zup` - Alternative PCK format
- `.pkx` - Large files (>2GB supported)
- `.pkx1` - Extended format

## Installation & Building

### Prerequisites

```bash
# Debian/Ubuntu
sudo apt-get update
sudo apt-get install build-essential cmake git zlib1g-dev dotnet-sdk-8.0

# Arch Linux
sudo pacman -S base-devel cmake git zlib dotnet-sdk
```

### Building Native Library

```bash
git clone https://github.com/yourusername/WinPCK.git
cd WinPCK
mkdir -p build
cd build
cmake ..
make -j$(nproc)
```

This creates `libpck.so` in the `build` directory.

### Building CLI Tool

```bash
cd build
g++ -o pck_cli ../cli_test.cpp -L. -lpck -I../PckDll/include -Wl,-rpath,'$ORIGIN'
```

### Building Avalonia GUI

```bash
cd avalonia_gui
dotnet restore
dotnet build
dotnet run  # To launch the GUI
```

## CLI Usage

### Basic Commands

```bash
# Display PCK file information
./pck_cli info yourfile.pck

# List all files in PCK
./pck_cli list yourfile.pck

# Extract all files
./pck_cli extract yourfile.pck /output/directory

# Search for files
./pck_cli search yourfile.pck "*.txt"

# Create new PCK from directory
./pck_cli create /source/directory output.pck

# Add files to existing PCK
./pck_cli add existingfile.pck /files/to/add

# Rebuild/optimize PCK
./pck_cli rebuild input.pck output.pck
```

### CLI Examples

```bash
# Extract configs.pck to ~/extracted/
./pck_cli extract configs.pck ~/extracted/

# Create a new PCK from a directory
./pck_cli create ~/my_game_files/ mygame.pck

# List all .ini files in a PCK
./pck_cli search gamedata.pck "*.ini"

# Get detailed info about a PCK
./pck_cli info elements.pck
```

## Avalonia GUI Usage

### Main Features

#### File Operations
- **Open** (📁) - Open and browse PCK files with TreeView navigation
- **New PCK** (✨) - Create new PCK archive from a folder
- **Batch Extract** (⚡) - Extract multiple PCK files at once without opening them
- **Close** (❌) - Close current PCK file

#### Editing Operations
- **Add Files** (➕) - Add files/folders to existing PCK
- **Rebuild** (🔧) - Rebuild and optimize PCK file structure
- **Rename** (✏️) - Rename files within PCK (right-click menu)
- **Delete** (🗑️) - Delete files from PCK (right-click menu)

#### Extraction
- **Extract All** (📦) - Extract all files from open PCK
- **Extract Selected** (📤) - Extract selected files (Ctrl+click for multiple)

#### Browsing
- **Search** (🔍) - Search for files by name
- **Info** (ℹ️) - Display detailed PCK information
- **TreeView** - Navigate folder hierarchy in left panel
- **File List** - View files with size, compression ratio in right panel

### Multi-Select Operations

1. **Ctrl+Click** - Select individual files
2. **Shift+Click** - Select range of files
3. Right-click on selected files to:
   - Extract Selected
   - Rename (single file only)
   - Delete (all selected)

### Batch Extract Workflow

Perfect for extracting dozens of PCK files:

1. Click **⚡ Batch Extract** button
2. Select multiple PCK files (Ctrl+click or Shift+click)
3. Select destination folder
4. Watch progress in log panel
5. Get summary: "Batch extraction complete: X succeeded, Y failed"

### Progress Monitoring

- **Progress Bar** - Shows extraction/creation progress
- **Status Bar** - Current operation and file count
- **Log Panel** - Detailed operation history with timestamps
- Color-coded messages: INFO (white), SUCCESS (green), ERROR (red)

## Technical Details

### UTF-32 String Handling

The Linux port handles UTF-32 wchar_t (4 bytes) properly:
- All string parameters are converted from UTF-16 (.NET) to UTF-32 (Linux)
- Supports international characters in filenames
- Proper handling of surrogate pairs for emoji and extended Unicode

### Encryption Keys

Perfect World PCK encryption keys are built-in:
- Key 1: `0xA8937462`
- Key 2: `0xF1A3653`

### Compression

- Uses zlib compression (levels 1-9, default 9)
- Supports both compressed and uncompressed files
- Shows compression ratio in file list

### Architecture

```
WinPCK/
├── PckDll/              # Native C++ library
│   ├── PckClass/        # Core PCK handling
│   ├── include/         # Public API headers
│   └── linux_platform/  # Linux-specific code
├── avalonia_gui/        # .NET 8.0 Avalonia GUI
│   ├── Native/          # P/Invoke bindings
│   ├── Services/        # Business logic layer
│   ├── ViewModels/      # MVVM view models
│   └── Views/           # XAML UI definitions
├── cli_test.cpp         # CLI tool source
└── build/               # Build output
    └── libpck.so        # Shared library
```

### Logging

GUI logs are written to:
```
avalonia_gui/bin/Debug/net8.0/logs/winpck[DATE].log
```

Log levels: DEBUG, INFO, WARNING, ERROR

## Troubleshooting

### GUI won't start
```bash
# Check if .NET 8.0 is installed
dotnet --version

# Should show 8.0.x or higher
# If not, install dotnet-sdk-8.0
```

### "libpck.so not found" error
```bash
# Ensure library is built
cd build
make

# Check library exists
ls -la libpck.so

# GUI looks for it in bin/Debug/net8.0/runtimes/linux-x64/native/
# Copy if needed:
mkdir -p ../avalonia_gui/bin/Debug/net8.0/runtimes/linux-x64/native/
cp libpck.so ../avalonia_gui/bin/Debug/net8.0/runtimes/linux-x64/native/
```

### Extraction produces empty files
This was fixed - ensure you're using the latest build with proper DWORD/ulong_t handling.

### Files show as single characters
This was fixed - ensure UTF-32 string conversion is being used in callbacks.

## Development

### Building for Release

```bash
cd avalonia_gui
dotnet publish -c Release -r linux-x64 --self-contained
```

Output: `bin/Release/net8.0/linux-x64/publish/`

### Running Tests

```bash
# Test CLI
cd build
./pck_cli info yourfile.pck

# Test GUI
cd avalonia_gui
dotnet run
```

### Adding New PCK Versions

Edit `PckDll/include/pck_default_vars.h` to add new version IDs and encryption keys.

## Credits

- Original WinPCK by [stsm/lmlstarqaq](https://github.com/stsm/WinPCK)
- C++20 modernization and improvements by [halysondev](https://github.com/halysondev/WinPCK)
- Linux port and Avalonia GUI based on halysondev's fork
- Perfect World encryption reverse engineering by community

## License

This project maintains the original WinPCK license. See the source repository for details.

## ChangeLog

### WinPCK Linux v1.0.0 (2025-01-23)

- ✅ Full Linux port of native PCK library
- ✅ Avalonia-based cross-platform GUI
- ✅ Command-line interface for scripting
- ✅ UTF-32 string handling for Linux wchar_t
- ✅ Complete feature parity with Windows version:
  - Create, edit, extract, rebuild PCK files
  - Batch extraction of multiple PCK files
  - TreeView navigation and file browsing
  - Search, rename, delete operations
  - Multi-select and context menus
- ✅ Fixed extraction bugs (DWORD/ulong_t casting)
- ✅ Fixed filename display issues (UTF-32 conversion)
- ✅ Progress monitoring and detailed logging
- ✅ Desktop integration with .desktop file

### Previous WinPCK Windows Versions

#### v1.33.1.0
- Improved support for .pkx files
- Fixed package creation bug

#### v1.33.0.3
- Reading of .cup files with auto-decoding
- Support for >2GB .pkx files
- Support for .pkx1 format
- English translation
- Updated to C++20
- Visual Studio 2022 support
