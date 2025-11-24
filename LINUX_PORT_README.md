# WinPCK Linux Port

This document describes the Linux port of WinPCK, a tool for working with Perfect World PCK game files.

## Current Status

This is a **partial port** of WinPCK to Linux. The project has extensive Windows dependencies that require significant porting work to compile on Linux.

### What Has Been Created

1. **Build System** (`CMakeLists.txt`)
   - CMake-based build system for Linux
   - Compiles dependencies: zlib, libdeflate, base64
   - Conditional compilation for platform-specific code

2. **POSIX MapViewFile Implementation** (`linux_platform/MapViewFile_posix.{h,cpp}`)
   - POSIX replacement for Windows memory-mapped file I/O using `mmap`
   - Compatible API with original Windows implementation
   - Located in: `linux_platform/`

3. **CLI Tool** (`linux_cli/main.cpp`)
   - Command-line interface for PCK operations
   - Supports: list, extract, info, create commands
   - Ready to use once library compiles

4. **Platform Compatibility Headers**
   - `platform_defs.h`: Cross-platform type definitions
   - Maps Microsoft types (`__int32`, `LPBYTE`, etc.) to standard C types
   - Defines Windows-specific keywords for GCC

### Remaining Issues

The codebase requires additional porting work:

1. **Windows API Dependencies**
   - Many source files still use Windows-specific APIs
   - Thread management (Windows threads → pthreads)
   - File path handling (backslashes → forward slashes)
   - Wide character conversions may need adjustment

2. **Compiler-Specific Code**
   - Microsoft-specific preprocessor macros
   - Visual Studio pragmas and attributes
   - Some inline assembly or intrinsics (if present)

3. **Missing Type Definitions**
   - Additional Windows types may need mapping
   - Some structures may have padding/alignment differences

## How to Build (When Complete)

```bash
cd /home/doug/WinPCK
mkdir -p build
cd build
cmake ..
make -j$(nproc)
```

The resulting binary will be `./pck_cli`

## Usage (When Complete)

```bash
# List contents of a PCK file
./pck_cli list game.pck

# Extract all files
./pck_cli extract game.pck ./output_dir

# Show file information
./pck_cli info game.pck

# Create new PCK file
./pck_cli create ./source_dir game.pck
```

## Next Steps to Complete the Port

### Option 1: Continue Manual Porting

1. Fix remaining compilation errors systematically:
   - Add missing type definitions to `platform_defs.h`
   - Replace Windows threading with pthreads
   - Fix path separator issues
   - Add `#include <cstring>` where needed for `memset`, `strlen`, etc.

2. Create stub implementations for Windows-only features
3. Test with actual PCK files

### Option 2: Use Wine/Compatibility Layer

If the porting effort is too extensive:
- Run the Windows version under Wine
- Create a wrapper script for easier invocation
- This would work immediately without porting

### Option 3: Focus on Core Library Only

Extract just the PCK file format code:
- Strip out all GUI and Windows-specific code
- Create minimal library with just PCK read/write
- Build simple CLI tool around it

## File Structure

```
WinPCK/
├── CMakeLists.txt              # Main build configuration
├── linux_platform/             # Linux-specific implementations
│   ├── MapViewFile_posix.h
│   └── MapViewFile_posix.cpp
├── linux_cli/                  # Command-line tool
│   └── main.cpp
├── PckDll/                     # Core PCK library (needs porting)
│   ├── include/
│   │   ├── platform_defs.h    # Cross-platform type definitions
│   │   └── pck_handle.h       # Main API
│   ├── PckClass/              # PCK file handling
│   ├── MapViewFile/           # Windows implementation
│   └── ...
├── zlib/                       # zlib compression (portable)
├── libdeflate/                 # libdeflate compression (portable)
└── base64/                     # Base64 encoding (portable)
```

## Dependencies

### Build Dependencies
- CMake >= 3.10
- GCC/G++ with C++20 support
- pthread library
- Standard C/C++ libraries

### Runtime Dependencies
- Linux kernel with mmap support
- libc with wide character support

## Known Limitations

1. This is an in-progress port - **it does not currently compile completely**
2. Some PCK format features may not work identically to Windows version
3. Performance may differ due to different memory-mapped I/O implementations
4. GUI features from original WinPCK are not included (CLI only)

## Original Project

WinPCK v1.33.1.0 by Li Qiufeng/stsm/liqf
- Perfect World PCK file viewer and editor
- Originally Windows-only with Visual Studio 2022
- Supports: Zhu Xian, Perfect World, Saint Seiya, and other games

## License

This port maintains the original open-source nature of WinPCK.
Please retain original author information as requested in source files.

## Contributing

To continue this porting effort:

1. Fix compilation errors in `/home/doug/WinPCK/PckDll/PckClass/`
2. Replace Windows threading with pthreads
3. Test with sample PCK files
4. Report issues and submit patches

---

**Note**: This is a work-in-progress port. The original Windows version is fully functional.
For immediate use, consider running the Windows version under Wine on Linux.
