#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
DIST_DIR="$REPO_ROOT/dist"
BUILD_DIR="$REPO_ROOT/build"
STAGING_DIR="$BUILD_DIR/staging"

echo "=== Packaging Cmpl Release Artifacts ==="

REBUILD=0
for arg in "$@"; do
    case "$arg" in
        -r|--rebuild) REBUILD=1 ;;
        -h|--help)
            echo "Usage: $(basename "$0") [--rebuild]"
            exit 0
            ;;
    esac
done

WIN_CLI_EXE="$BUILD_DIR/win-cli/cmpl.exe"
WIN_GUI_EXE="$BUILD_DIR/win-gui/cmpl.exe"
LINUX_CLI="$BUILD_DIR/linux-cli/cmpl"

if [ "$REBUILD" -eq 1 ] || [ ! -f "$WIN_CLI_EXE" ]; then
    echo "Building Windows CLI..."
    bash "$SCRIPT_DIR/build-win-cli.sh"
fi

if [ "$REBUILD" -eq 1 ] || [ ! -f "$WIN_GUI_EXE" ]; then
    echo "Building Windows GUI..."
    bash "$SCRIPT_DIR/build-win-gui.sh"
fi

if [ "$REBUILD" -eq 1 ] || [ ! -f "$LINUX_CLI" ]; then
    echo "Building Linux CLI..."
    bash "$SCRIPT_DIR/build-linux-cli.sh"
fi

# Setup directories
rm -rf "$STAGING_DIR"
mkdir -p "$DIST_DIR" "$BUILD_DIR" "$STAGING_DIR/win" "$STAGING_DIR/linux"

# Package Windows
echo "Packaging Windows release..."
cp "$WIN_CLI_EXE" "$STAGING_DIR/win/cmpl.exe"
cp "$WIN_GUI_EXE" "$STAGING_DIR/win/cmpl-gui.exe"
[ -f "$REPO_ROOT/LICENSE.txt" ] && cp "$REPO_ROOT/LICENSE.txt" "$STAGING_DIR/win/"
[ -f "$REPO_ROOT/README.md" ] && cp "$REPO_ROOT/README.md" "$STAGING_DIR/win/"

WIN_ZIP="$DIST_DIR/cmpl-x86_64-win.zip"
rm -f "$WIN_ZIP"

if command -v zip >/dev/null 2>&1; then
    (cd "$STAGING_DIR/win" && zip -q -r "$WIN_ZIP" .)
elif command -v tar >/dev/null 2>&1; then
    tar -a -cf "$WIN_ZIP" -C "$STAGING_DIR/win" .
else
    echo "Error: neither zip nor tar found to create Windows zip archive" >&2
    exit 1
fi
cp -f "$WIN_ZIP" "$BUILD_DIR/cmpl-x86_64-win.zip"

echo "Created: $WIN_ZIP"

# Package Linux
echo "Packaging Linux release..."
cp "$LINUX_CLI" "$STAGING_DIR/linux/cmpl"
chmod +x "$STAGING_DIR/linux/cmpl" 2>/dev/null || true
[ -f "$REPO_ROOT/LICENSE.txt" ] && cp "$REPO_ROOT/LICENSE.txt" "$STAGING_DIR/linux/"
[ -f "$REPO_ROOT/README.md" ] && cp "$REPO_ROOT/README.md" "$STAGING_DIR/linux/"

LINUX_TAR="$DIST_DIR/cmpl-x86_64-linux.tar.gz"
rm -f "$LINUX_TAR"
tar -czf "$LINUX_TAR" -C "$STAGING_DIR/linux" .
cp -f "$LINUX_TAR" "$BUILD_DIR/cmpl-x86_64-linux.tar.gz"

echo "Created: $LINUX_TAR"

# Clean up staging
rm -rf "$STAGING_DIR"

echo "=== Packaging Complete ==="
