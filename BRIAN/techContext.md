# Technical Context: CmplPiler

## Technology Stack

### Runtime & Language
- **Language**: C# 13
- **Runtime Target**: .NET 10 (`net10.0` / `net10.0-windows`)
- **SDK**: .NET SDK 10.0+
- **Project Format**: Microsoft.NET.Sdk, Solution file format `CmplPiler.slnx`

### Core Dependencies
- **`YamlDotNet` (v18.0.0)**: Used in `Core/CmplParser.cs` for robust YAML deserialization into C# object models with underscored naming convention mapping.
- **Windows Forms (`System.Windows.Forms`)**: Built into the Windows Desktop SDK workload for .NET 10, enabled conditionally on Windows targets.
- **`kernel32.dll` (Win32 API)**: P/Invoke `AttachConsole` used in `Gui/ConsoleInterop.cs` for parent console reattachment.

---

## Build System & Project Configuration

The project file (`CmplPiler/CmplPiler.csproj`) utilizes advanced MSBuild property conditioning to achieve single-source cross-platform output:

```xml
<PropertyGroup>
  <OutputType>Exe</OutputType>
  <TargetFramework Condition="'$(TargetFramework)' == '' and '$(IncludeGui)' != 'false' and $(RuntimeIdentifier.StartsWith('win'))">net10.0-windows</TargetFramework>
  <TargetFramework Condition="'$(TargetFramework)' == ''">net10.0</TargetFramework>
  <Nullable>enable</Nullable>
  <ImplicitUsings>enable</ImplicitUsings>
  <AssemblyName>cmpl</AssemblyName>
  <RootNamespace>CmplPiler</RootNamespace>
  <StartupObject>CmplPiler.Program</StartupObject>
  <!-- Allows compiling (not running) the Windows target on Linux/macOS build agents -->
  <EnableWindowsTargeting>true</EnableWindowsTargeting>
</PropertyGroup>

<PropertyGroup Condition="'$(TargetFramework)' == 'net10.0-windows'">
  <OutputType>WinExe</OutputType>
  <UseWindowsForms>true</UseWindowsForms>
</PropertyGroup>

<ItemGroup Condition="'$(TargetFramework)' != 'net10.0-windows'">
  <Compile Remove="Gui/**" />
  <EmbeddedResource Remove="Gui/**" />
</ItemGroup>
```

### Key Compilation Highlights:
1. **Target Selection via Runtime Identifier**:
   - Specifying `-r win-x64` (or any `win-*` RID) automatically targets `net10.0-windows` and sets `OutputType=WinExe` with `UseWindowsForms=true`.
   - Plain builds (or non-Windows RIDs like `linux-x64`) target `net10.0` as a standard console `Exe`, excluding `Gui/**` from compilation and embedded resources.
2. **Cross-Compilation (`EnableWindowsTargeting`)**:
   - Enables Linux and macOS CI runners to compile `net10.0-windows` assemblies without running them.

---

## Supported Compilers & External Toolchains

| Toolchain / System | Supported Executable / Tool | Platform | Detection / Resolution Mechanism |
| :--- | :--- | :--- | :--- |
| `msvc` (direct) | `cl.exe` | Windows | Located via `%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe` -> `VsDevCmd.bat` |
| `gcc` (direct) | `g++` | Windows, Linux, macOS | Resolved via system `PATH` |
| `clang` (direct) | `clang++` | Windows, Linux, macOS | Resolved via system `PATH` |
| Custom (direct) | User-defined command/path | Any | Executed as specified |
| `cmake` | `cmake` | Any | Resolved via system `PATH` |
| `dotnet` | `dotnet` | Any | Resolved via system `PATH` |
| `msbuild` | `msbuild.exe` or `dotnet msbuild` | Windows (VS) / Unix (SDK) | Handled via `VsDevCmd.bat` on Windows; falls back to `dotnet msbuild` on Unix |

---

## Development & Publishing Workflows

### 1. Local Development
```bash
# Build CLI default
dotnet build CmplPiler.slnx

# Run CLI against example
dotnet run --project CmplPiler -- examples/hello-cpp/hello.cmpl --list
dotnet run --project CmplPiler -- examples/hello-cpp/hello.cmpl -p gcc-release

# Run Windows GUI in Visual Studio
dotnet run --project CmplPiler -r win-x64
```

### 2. Publishing Single-File Binaries
```bash
# Linux CLI (Self-contained ELF single binary)
dotnet publish CmplPiler -c Release -r linux-x64 --self-contained /p:PublishSingleFile=true

# Windows CLI + GUI (Self-contained PE single executable)
dotnet publish CmplPiler -c Release -r win-x64 --self-contained /p:PublishSingleFile=true

# Windows CLI only (headless, without WinForms)
dotnet publish CmplPiler -c Release -r win-x64 -p:IncludeGui=false --self-contained /p:PublishSingleFile=true
```

### 3. Packaging Scripts (`tools/` & `CmplCompiler.iss`)
The `tools/` directory and root repository provide packaging automation:
- `build-win-cli.*`: Compiles Windows CLI (`build/win-cli/cmpl.exe`).
- `build-win-gui.*`: Compiles Windows GUI (`build/win-gui/cmpl.exe`).
- `build-installer.ps1`: Compiles Inno Setup 7 installer (`dist/CmplCompiler-1.0.0-x64-Setup.exe`).
- `build-linux-cli.*`: Compiles Linux CLI (`build/linux-cli/cmpl`).
- `package-all.*`: Packages releases into `dist/cmpl-x86_64-win.zip`, `dist/cmpl-x86_64-linux.tar.gz`, and the Windows Inno Setup installer.
- `CmplCompiler.iss`: Inno Setup 7.1.0 script featuring modern wizard styling, dual CLI/GUI installation, PATH environment configuration, and `.cmpl` shell file associations.

---

## Editor Integration (`editors/vscode-cmpl/`)
- **Package Details**: VS Code extension defined in `package.json` (`cmpl-language`).
- **Dependencies**: `redhat.vscode-yaml`.
- **Validation**: Binds `*.cmpl` files to `schemas/cmpl.schema.json`.
- **Sync Script**: `scripts/sync-schema.mjs` synchronizes the extension's local schema copy with repository root `cmpl.schema.json`.
- **Tooling**: `@vscode/vsce` and `ovsx` for building `.vsix` packages.
