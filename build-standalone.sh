#!/bin/bash
set -e

echo "Building WinPCK Standalone Package..."

# Build native library
echo "[1/3] Building native library..."
cd /home/doug/WinPCK
mkdir -p build
cd build
cmake ..
make -j$(nproc)

# Build GUI as self-contained
echo "[2/3] Building self-contained .NET app..."
cd ../avalonia_gui
dotnet publish -c Release -r linux-x64 --self-contained true -p:PublishSingleFile=false

# Create standalone package
echo "[3/3] Creating standalone package..."
cd /home/doug/WinPCK
rm -rf WinPCK-Standalone
mkdir -p WinPCK-Standalone

# Copy published files
cp -r avalonia_gui/bin/Release/net8.0/linux-x64/publish/* WinPCK-Standalone/

# Copy native library to multiple locations to ensure it's found
cp build/libpcklib.so WinPCK-Standalone/
cp build/libpcklib.so WinPCK-Standalone/runtimes/linux-x64/native/

# Create launcher script
cat > WinPCK-Standalone/WinPCK << 'EOF'
#!/bin/bash
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
export LD_LIBRARY_PATH="$SCRIPT_DIR:$SCRIPT_DIR/runtimes/linux-x64/native:$LD_LIBRARY_PATH"
cd "$SCRIPT_DIR"
exec "$SCRIPT_DIR/WinPCK.Avalonia" "$@"
EOF

chmod +x WinPCK-Standalone/WinPCK

# Create tarball
tar -czf WinPCK-Linux-x86_64.tar.gz WinPCK-Standalone/

echo "✓ Standalone package created: WinPCK-Linux-x86_64.tar.gz"
ls -lh WinPCK-Linux-x86_64.tar.gz
echo ""
echo "To use:"
echo "  tar -xzf WinPCK-Linux-x86_64.tar.gz"
echo "  cd WinPCK-Standalone"
echo "  ./WinPCK"
