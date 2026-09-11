#!/usr/bin/env bash
set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
CMPL_FILE="$REPO_ROOT/examples/self-host.cmpl"
PROJECT_FILE="$REPO_ROOT/CmplPiler/CmplPiler.csproj"
OUTPUT_DIR="$REPO_ROOT/build/win-cli"
PROFILE="win-cli"

echo "=== Building win-cli ==="

find_cmpl() {
    local target_out=""
    if [ -d "$OUTPUT_DIR" ]; then
        target_out="$(cd "$OUTPUT_DIR" && pwd)"
    fi

    local candidates=(
        "$REPO_ROOT/build/win-gui/cmpl.exe"
        "$REPO_ROOT/build/windows-x64/cmpl.exe"
        "$REPO_ROOT/build/cli-release/cmpl.exe"
        "$REPO_ROOT/build/cli-release/cmpl"
        "$REPO_ROOT/build/linux-cli/cmpl"
        "$REPO_ROOT/build/linux-x64/cmpl"
        "$REPO_ROOT/CmplPiler/bin/Release/net10.0-windows/win-x64/cmpl.exe"
        "$REPO_ROOT/CmplPiler/bin/Release/net10.0/cmpl.exe"
        "$REPO_ROOT/CmplPiler/bin/Release/net10.0/cmpl"
        "$REPO_ROOT/CmplPiler/bin/Release/net10.0/linux-x64/cmpl"
        "$REPO_ROOT/CmplPiler/bin/Debug/net10.0/cmpl.exe"
        "$REPO_ROOT/CmplPiler/bin/Debug/net10.0/cmpl"
        "$REPO_ROOT/build/win-cli/cmpl.exe"
    )

    # Check PATH first if not pointing to target output dir
    if command -v cmpl >/dev/null 2>&1; then
        local p
        p="$(command -v cmpl)"
        local p_dir
        p_dir="$(cd "$(dirname "$p")" 2>/dev/null && pwd || true)"
        if [ -z "$target_out" ] || [ "$p_dir" != "$target_out" ]; then
            echo "$p"
            return 0
        fi
    fi

    for c in "${candidates[@]}"; do
        if [ -f "$c" ]; then
            local c_dir
            c_dir="$(cd "$(dirname "$c")" 2>/dev/null && pwd || true)"
            if [ -n "$target_out" ] && [ "$c_dir" = "$target_out" ]; then
                continue
            fi
            if [ -x "$c" ] || [[ "$c" == *.exe ]]; then
                echo "$c"
                return 0
            fi
        fi
    done

    return 1
}

CMPL_BIN="$(find_cmpl 2>/dev/null || true)"
BUILD_SUCCEEDED=0

if [ -n "$CMPL_BIN" ]; then
    echo "Found pre-built cmpl compiler at: $CMPL_BIN"
    echo "Attempting build using cmpl ($CMPL_FILE -p $PROFILE)..."
    if "$CMPL_BIN" "$CMPL_FILE" -p "$PROFILE"; then
        BUILD_SUCCEEDED=1
        echo "cmpl build succeeded!"
    else
        echo "cmpl build failed. Reverting to dotnet tooling..."
    fi
else
    echo "No pre-built cmpl compiler found. Using dotnet tooling..."
fi

if [ "$BUILD_SUCCEEDED" -ne 1 ]; then
    echo "Running dotnet tooling..."
    dotnet publish "$PROJECT_FILE" -c Release -r win-x64 -p:IncludeGui=false --self-contained true -p:PublishSingleFile=true -o "$OUTPUT_DIR"
    echo "dotnet build succeeded!"
fi
