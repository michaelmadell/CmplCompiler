# Project Brief: CmplPiler (cmpl)

## Executive Summary
**CmplPiler** (`cmpl`) is a modern, cross-platform build orchestrator designed to streamline compiling and building software across heterogeneous build systems and compilers. It is driven by simple, declarative `.cmpl` (YAML) configuration files that define *what* to build through named profiles, while CmplPiler handles *how* to generate and execute the correct commands.

The project is implemented in C# targeting **.NET 10**, structured as a single cohesive project that produces a single-file, self-contained executable for each target platform.

---

## Core Objectives & Problem Statement
1. **Declarative Simplicity**: Provide a clean, human-readable YAML specification (`.cmpl`) to define multi-profile builds without writing custom shell scripts, Makefiles, or complex procedural build automation.
2. **Unified Build Routing**: Support multiple underlying build systems under one unified configuration interface:
   - **Direct Compiler Invocation**: Direct calls to C++ compilers (`msvc`, `gcc`, `clang`, or arbitrary custom compilers).
   - **CMake**: Automatic `-B`/`-S` project configuration and `--build` invocations.
   - **.NET CLI**: `dotnet build` and `dotnet publish` orchestration.
   - **MSBuild**: Windows and Unix (`dotnet msbuild`) solution and project compilation.
3. **Dual-Personality Single Executable**:
   - On Windows: Acts as a console CLI when run with arguments, but automatically opens a Windows Forms GUI when launched without arguments (or with `--gui`).
   - On Linux/macOS: Acts as a lightweight, portable console CLI.
   - Solves the friction of maintaining separate GUI and CLI projects or executables.
4. **Zero-Setup Environment Bridging**:
   - Automatically detects Visual Studio installations via `vswhere` on Windows and transparently wraps MSVC compiler calls within `VsDevCmd.bat` with proper architecture arguments (`-arch`, `-host_arch`).
5. **Relocatability & Portability**:
   - Project-relative paths ensure `.cmpl` projects are portable across machines and directories without hardcoded absolute paths.
   - Built-in variable expansion (`${project_name}`, `${base_dir}`, project environment definitions, and host OS environment variables).

---

## Architectural Principles
- **Strict Layering**: Clear separation of concerns into `Core` (models, parsing, validation, task generation, execution), `Cli` (argument parsing, formatted console UI), and `Gui` (WinForms visual interface).
- **Target Framework Conditioning**: Compilation targets are selected dynamically via runtime identifiers (RID-driven: `win-*` targets compile `net10.0-windows` with WinForms enabled; generic or non-Windows builds target `net10.0`).
- **Fail-Fast Validation**: Schema validation, strict required fields checking, and descriptive errors prevent executing invalid configurations.
- **Robust Process Lifecycle**: Full process-tree cancellation (`Kill(entireProcessTree: true)`) when interrupted via Ctrl+C or GUI cancel buttons.

---

## Project Artifacts & Scope
- **Core Engine**: `CmplPiler/Core/`
- **Console Interface**: `CmplPiler/Cli/`
- **Windows Forms GUI**: `CmplPiler/Gui/`
- **Formal Schema**: `cmpl.schema.json` (JSON Schema Draft-07)
- **Editor Tooling**: `editors/vscode-cmpl/` (VS Code Marketplace extension providing autocomplete and schema validation)
- **Reference Examples**: `examples/hello-cpp/` (C++ multi-toolchain), `examples/self-host.cmpl` (self-hosting build definition)
- **Build & Packaging Tooling**: `tools/` (multi-platform packaging scripts for Windows zip and Linux tarball)
