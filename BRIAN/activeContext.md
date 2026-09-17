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
   - Stopped classifying stderr output as fatal errors, accommodating compilers that write informational headers or progress to stderr.

---

## Active Architectural Decisions & Conventions

- **Process-Tree Termination**:
  - `BuildRunner` uses `process.Kill(entireProcessTree: true)` when cancelled to guarantee that child build processes (e.g., recursive `cmake`, `ninja`, `cl.exe`, `link.exe`) are killed and do not hang in the background.
- **Relocatable Rooting**:
  - `CmplProject.BaseDirectory` is explicitly set to the directory containing the `.cmpl` file upon loading. All user-supplied relative paths are resolved against this directory via `Path.GetFullPath(Path.Combine(baseDir, path))`.
- **Variable Expansion Priority**:
  - `${VAR}` lookup precedence is strictly defined:
    1. Built-in constants (`project_name`, `base_dir`)
    2. Project configuration `environment` dictionary
    3. Host system environment variables
- **Direct C++ Toolchain Compilation**:
  - Direct builds currently assemble compiler calls assuming all `.cpp` files in `source_dir` are to be compiled (`"<sourceDir>"/*.cpp`).

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
