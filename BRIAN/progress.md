# Progress & Status: CmplPiler

## Feature Status Overview

| Capability | Status | Notes |
| :--- | :---: | :--- |
| **YAML Parsing & Model Binding** | ✅ Complete | Uses YamlDotNet with snake_case naming convention and extra-property tolerance |
| **Validation & Error Reporting** | ✅ Complete | Strict invariant checking with clear error messaging for missing or invalid configuration |
| **Variable Expansion** | ✅ Complete | Supports `${project_name}`, `${base_dir}`, project `environment` map, and system environment variables |
| **Direct Toolchain (MSVC)** | ✅ Complete | Automatic `vswhere` detection, `VsDevCmd.bat` wrapping with arch flags, `/Fo` intermediate isolation, and response file support |
| **Direct Toolchain (GCC/Clang)** | ✅ Complete | Automatic argument mapping (`-I`, `-D`, `-o`), flexible `sources` matching, and response file support |
| **Flexible Source Matching** | ✅ Complete | Multi-extension, explicit source lists, recursive globbing (`**/*.cpp`), and backwards-compatible fallback |
| **Compiler Response Files** | ✅ Complete | `@args.rsp` generation via `use_response_file` or auto-triggering on > 2048 char command lines |
| **Profile-Level Environment** | ✅ Complete | Profile-level `environment` dictionary with variable expansion and process environment override |
| **CMake Integration** | ✅ Complete | Generates config (`-B`, `-S`, `-DCMAKE_BUILD_TYPE`) and build (`--build`, `--config`) tasks |
| **.NET CLI Integration** | ✅ Complete | Generates `dotnet build` and `dotnet publish` with configuration and output flags |
| **MSBuild Integration** | ✅ Complete | Supports `VsDevCmd` MSBuild on Windows and `dotnet msbuild` on Unix |
| **Pre/Post Build Hooks** | ✅ Complete | Executes shell commands in `cmd.exe` (Windows) or `/bin/sh` (Unix) relative to `.cmpl` directory |
| **Process Tree Cancellation** | ✅ Complete | Kills full process tree on Ctrl+C or GUI cancel request |
| **Console CLI Interface** | ✅ Complete | Options: `--profile`, `--list`, `--dry-run`, `--help`. Styled UTF-8 box borders and status cards |
| **Windows Forms GUI** | ✅ Complete | Project file loader, profile selector, live log viewer, cancellation support, resizable layout |
| **Single Binary Architecture** | ✅ Complete | `OutputType=WinExe` with `AttachConsole` P/Invoke and RID-based conditional compilation |
| **Self-Hosting Verification** | ✅ Complete | `examples/self-host.cmpl` successfully builds CmplPiler binaries using CmplPiler itself |
| **Packaging Automation** | ✅ Complete | Scripts under `tools/` package `.zip` (Windows) and `.tar.gz` (Linux) release archives |
| **VS Code Extension** | ✅ Complete | `editors/vscode-cmpl` provides syntax highlighting and `cmpl.schema.json` validation |
| **CI/CD Automation** | ✅ Complete | GitHub Actions workflows for dual-OS build verification and VS Code extension packaging |

---

## What Works & Verification

1. **Build & Execution**:
   - Compiles cleanly on .NET 10 SDK on both Windows and Linux.
   - Tested through CI workflow on `windows-latest` and `ubuntu-latest`.
2. **Smoke Tests in CI**:
   - Profile listing (`--list`)
   - Dry-run verification (`--dry-run`)
   - Direct compilation of C++ example with GCC and execution of resulting binary
   - Self-host compilation of CmplPiler CLI
   - Single-file publish verification for Linux and Windows
3. **Packaging**:
   - `tools/package-all.ps1`, `package-all.bat`, and `package-all.sh` generate release archives containing CLI and GUI executables, documentation, and license files.

---

## Known Limitations & Technical Debt

1. **Rebuild / Incremental Behavior**:
   - In `direct` build system mode, every execution invokes the compiler on all matching source files; no internal dependency graph or caching is maintained (relies on underlying tools for `cmake`/`dotnet`/`msbuild`).
2. **Automated Test Harness**:
   - There is no dedicated test project (e.g. `CmplPiler.Tests.csproj`) with unit tests covering edge cases in parser validation, variable recursion, or shell command escaping. Tests currently rely on end-to-end smoke testing in CI.
3. **macOS Support**:
   - While the code avoids Windows-only constructs outside `Gui` and uses `/bin/sh` for Unix, dedicated macOS packaging scripts and CI matrix jobs are not yet configured.

---

## Future Roadmap Ideas

- [x] Add support for custom source patterns/globs or explicit source lists in `direct` profiles.
- [x] Add response file (`@args.rsp`) generation for direct builds with extensive argument lists.
- [x] Support profile-level environment variable overrides.
- [ ] Implement a unit test suite for `CmplParser`, `CommandGenerator`, and variable substitution.
- [ ] Add macOS targets and packaging (`.tar.gz`) to `tools/` and GitHub Actions.
- [ ] Publish the VS Code extension to the Visual Studio Marketplace and Open VSX Registry.
