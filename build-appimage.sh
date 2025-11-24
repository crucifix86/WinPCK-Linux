#!/bin/bash
set -e

echo "Building WinPCK AppImage..."

# Colors
GREEN='\033[0;32m'
BLUE='\033[0;34m'
NC='\033[0m'

# Build native library first
echo -e "${BLUE}[1/5] Building native PCK library...${NC}"
cd /home/doug/WinPCK
mkdir -p build
cd build
cmake ..
make -j$(nproc)

# Copy library to runtime location
echo -e "${BLUE}[2/5] Copying native library...${NC}"
mkdir -p ../avalonia_gui/bin/Release/net8.0/linux-x64/publish/runtimes/linux-x64/native/
cp libpcklib.so ../avalonia_gui/bin/Release/net8.0/linux-x64/publish/runtimes/linux-x64/native/

# Build Avalonia GUI as self-contained
echo -e "${BLUE}[3/5] Building Avalonia GUI (self-contained)...${NC}"
cd ../avalonia_gui
dotnet publish -c Release -r linux-x64 --self-contained true -p:PublishSingleFile=false -p:IncludeNativeLibrariesForSelfExtract=true

# Create AppDir structure
echo -e "${BLUE}[4/5] Creating AppImage structure...${NC}"
cd /home/doug/WinPCK
rm -rf AppDir
mkdir -p AppDir/usr/bin
mkdir -p AppDir/usr/lib
mkdir -p AppDir/usr/share/applications
mkdir -p AppDir/usr/share/icons/hicolor/256x256/apps

# Copy published files
cp -r avalonia_gui/bin/Release/net8.0/linux-x64/publish/* AppDir/usr/bin/

# Also copy native library to main bin directory for direct access
cp build/libpcklib.so AppDir/usr/bin/

# Create desktop file
cat > AppDir/usr/share/applications/winpck.desktop << 'EOF'
[Desktop Entry]
Type=Application
Name=WinPCK
Comment=Perfect World PCK File Editor
Exec=WinPCK.Avalonia
Icon=winpck
Categories=Utility;FileTools;
Terminal=false
EOF

# Create AppRun launcher
cat > AppDir/AppRun << 'EOF'
#!/bin/bash
SELF=$(readlink -f "$0")
HERE=${SELF%/*}
export PATH="${HERE}/usr/bin:${PATH}"
export LD_LIBRARY_PATH="${HERE}/usr/bin:${HERE}/usr/lib:${HERE}/usr/bin/runtimes/linux-x64/native:${LD_LIBRARY_PATH}"
cd "${HERE}/usr/bin"
exec "${HERE}/usr/bin/WinPCK.Avalonia" "$@"
EOF

chmod +x AppDir/AppRun

# Copy icon (use avalonia default if winpck.png not available)
if [ -f winpck.png ]; then
    cp winpck.png AppDir/usr/share/icons/hicolor/256x256/apps/winpck.png
    cp winpck.png AppDir/winpck.png
    cp winpck.png AppDir/.DirIcon
else
    echo "Warning: winpck.png not found, using default icon"
fi

# Desktop file at root
cp AppDir/usr/share/applications/winpck.desktop AppDir/

# Download appimagetool if not present
if [ ! -f appimagetool-x86_64.AppImage ]; then
    echo -e "${BLUE}Downloading appimagetool...${NC}"
    wget -q https://github.com/AppImage/AppImageKit/releases/download/continuous/appimagetool-x86_64.AppImage
    chmod +x appimagetool-x86_64.AppImage
fi

# Build AppImage
echo -e "${BLUE}[5/5] Building AppImage...${NC}"
ARCH=x86_64 ./appimagetool-x86_64.AppImage AppDir WinPCK-x86_64.AppImage

echo -e "${GREEN}✓ AppImage created: WinPCK-x86_64.AppImage${NC}"
ls -lh WinPCK-x86_64.AppImage
