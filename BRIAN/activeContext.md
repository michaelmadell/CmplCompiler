# Active Context: CmplPiler

## Current Repository State

The repository represents a mature, functional, and well-structured .NET 10 project. The core architecture is decoupled into `Core`, `Cli`, and `Gui` sub-namespaces, supported by automated CI workflows for both the compiler and its VS Code extension.

---

## Recent Significant Milestones & Changes

Based on recent commit history and repository files:
1. **Target Framework Selection Refactoring (`e2a0bfd`)**:
   - Replaced earlier dual `TargetFrameworks` (which emitted separate folders for `net10.0` and `net10.0-windows`) with a single conditional `TargetFramework` selected by runtime identifier (`RuntimeIdentifier.StartsWith('win')`).
   - Plain builds or Linux publishes output `net10.0` portable CLI.
   - Windows RID publishes output `net10.0-windows` with WinForms compiled in.
2. **Terminal UX & Formatting Polish (`8e040d0`)**:
   - Upgraded `CliRunner.cs` output to use styled UTF-8 box borders (`╭───╮`).
   - Added formatted "BUILD SUCCEEDED" (bold green) and "BUILD FAILED" (bold red) banners, with centered exit code display.
3. **Packaging Tooling (`7aed3f2`, `e559fc5`)**:
   - Added automated scripts under `tools/` (`build-*-cli`, `build-*-gui`, `package-all`) across `.ps1`, `.bat`, and `.sh`.
   - Enabled packaging of zip and tarball distribution archives into `dist/`.
4. **VS Code Extension Integration (`d880da6`)**:
   - Created declarative VS Code extension in `editors/vscode-cmpl/`.
   - Added GitHub Actions workflow (`.github/workflows/vscode-extension.yml`) to package and publish the extension.
5. **MSVC Tooling Fixes (`085c948`)**:
   - Added `-arch` and `-host_arch` to `VsDevCmd.bat` initialization to prevent defaulting to x86 compilers on 64-bit systems.
   - Directed MSVC intermediate object files into `output_dir` via `/Fo` switch to keep the source tree clean.
6. **Core Engine & Direct Compilation Enhancements (Section 1)**:
   - Added `sources` globbing/pattern matching supporting explicit files, subdirectories, and recursive globs (`**/*.cpp`, `*.c`, etc.) with backwards-compatible fallback.
   - Enclosed custom compiler paths containing spaces in quotes to prevent shell execution syntax errors.
   - Implemented profile-level `environment` overrides in model, schema, variable expansion, and process start info.
   - Implemented response file (`@args.rsp`) support via `use_response_file` configuration and automatic switching for direct toolchain invocations exceeding 2,048 characters.

---

## Active Architectural Decisions & Conventions

- **Process-Tree Termination**:
  - `BuildRunner` uses `process.Kill(entireProcessTree: true)` when cancelled to guarantee that child build processes (e.g., recursive `cmake`, `ninja`, `cl.exe`, `link.exe`) are killed and do not hang in the background.
- **Relocatable Rooting**:
  - `CmplProject.BaseDirectory` is explicitly set to the directory containing the `.cmpl` file upon loading. All user-supplied relative paths are resolved against this directory via `Path.GetFullPath(Path.Combine(baseDir, path))`.
- **Variable Expansion Priority**:
  - `${VAR}` lookup precedence is strictly defined:
    1. Built-in constants (`project_name`, `base_dir`)
    2. Profile-level `environment` dictionary (if set)
    3. Project-level `environment` dictionary
    4. Host system environment variables
- **Direct C++ Source Resolution**:
  - Direct builds resolve source files through `ResolveSourceArguments`. If matching files exist on disk, their full paths are passed explicitly. If not found or when pattern globbing is relied upon, it safely falls back to formatted shell glob patterns.
- **Compiler Response Files**:
  - Direct compiler invocations with `msvc`, `gcc`, or `clang` automatically generate a `.rsp` file in the output directory if `use_response_file: true` or if argument length exceeds 2,048 characters, preventing `cmd.exe` command length limitations.

---

## Immediate Considerations & Next Steps

1. **Schema Synchronization**:
   - When modifying `cmpl.schema.json` at the repo root, remember to run `node editors/vscode-cmpl/scripts/sync-schema.mjs` (or `npm run sync-schema`) to keep the VS Code extension schema synchronized.
2. **Direct Compiler File Matching**:
   - Consider expanding `source_dir` pattern support (e.g. supporting subdirectories, explicit source file lists, or other extensions like `.c`, `.cc`, `.cxx`).
3. **Incremental Build Support in Direct Mode**:
   - Direct compiler builds recompile all source files on every run. Delegating compilation to ninja or generating response files could be investigated if project sizes scale.
4. **Automated Integration & Unit Testing**:
   - Currently, tests are smoke-tested through GitHub Actions and self-hosting scripts. Introducing a dedicated xUnit or NUnit test project for `CmplParser` and `CommandGenerator` would strengthen regression prevention.
