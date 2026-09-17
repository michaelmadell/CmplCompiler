# CmplPiler Improvement Suggestions

This document tracks identified architectural, functional, and developer experience improvements for CmplPiler.

---

## 1. Core Engine & Direct Compilation

- [x] **1.A: Flexible Source Matching (Multi-extension & Globs)**
  - *Current State*: Resolved. Added `sources` list to `CmplProfile` and `cmpl.schema.json`. Supports explicit files, relative paths, and globs (`*.cpp`, `*.c`, `**/*.cpp`) with automatic backwards-compatible fallback.
  - *Improvement*: Added `sources` list to `CmplProfile` and `cmpl.schema.json`. If omitted, default to `["*.cpp"]` for backwards compatibility. Support relative paths and globs.

- [x] **1.B: Quote Custom Compiler Paths**
  - *Current State*: Resolved. Compiler paths containing spaces are automatically enclosed in quotes in `CommandGenerator.GenerateDirectTask`.
  - *Improvement*: Ensure any compiler path with spaces or custom paths are properly quoted when constructing the command line.

- [x] **1.C: Profile-Level Environment Variables**
  - *Current State*: Resolved. `environment` map is now supported at profile level in `CmplProfile` and `cmpl.schema.json`, merging with and overriding project-level environment during variable expansion and process execution.
  - *Improvement*: Allow `environment` in `CmplProfile`. Merge/override project-level variables with profile-level variables during variable expansion and process execution.

- [x] **1.D: Response File (`@args.rsp`) Support for Direct Builds**
  - *Current State*: Resolved. Added `use_response_file` option to `CmplProfile` and `cmpl.schema.json`, plus auto-switching when direct toolchain arguments exceed 2,048 characters. `BuildRunner` writes the `.rsp` file prior to execution.
  - *Improvement*: Generate a compiler response file (`.rsp`) when arguments exceed command-line limits or by default during direct builds.

---

## 2. CLI & Build Lifecycle

- [ ] **2.A: Preserve Process Exit Codes**
  - *Current State*: `CliRunner` collapses all non-zero exit codes to `1` (`return exitCode == 0 ? 0 : 1`).
  - *Improvement*: Forward the actual exit code from the underlying build process so CI pipelines can distinguish specific failure codes.

- [ ] **2.B: Add a `--clean` Flag & Clean Hook**
  - *Current State*: Wiping build directories requires manual file deletion or custom scripts.
  - *Improvement*: Support `cmpl <file> --clean` to remove `output_dir` (or execute a `clean` task list if specified).

- [ ] **2.C: Build Elapsed Timer**
  - *Current State*: Build success/failure banners do not show elapsed time.
  - *Improvement*: Track duration using `Stopwatch` and display formatted execution time in build result cards.

- [ ] **2.D: Respect `NO_COLOR` / Non-TTY Environments**
  - *Current State*: ANSI color codes are output unconditionally.
  - *Improvement*: Check `Console.IsOutputRedirected` and `NO_COLOR` environment variable before emitting ANSI color/bold escape codes.

---

## 3. Graphical User Interface (GUI)

- [ ] **3.A: Dry-Run Mode in GUI**
  - Add a "Dry Run" button/checkbox to preview generated command lists in the output viewer without executing them.

- [ ] **3.B: Elapsed Timer & Status Bar**
  - Add a status strip showing build status (Idle, Building, Succeeded, Failed) and execution time.

- [ ] **3.C: Remember Recent Projects**
  - Save recently loaded `.cmpl` files in user settings and offer a "Recent Files" menu/dropdown.

- [ ] **3.D: Output Auto-Scroll Control**
  - Provide a toggle to enable/disable auto-scrolling on output logs.

---

## 4. Testability & CI/CD

- [ ] **4.A: Automated Unit & Integration Test Suite**
  - Introduce `CmplPiler.Tests` using xUnit to test parsing, validation invariants, variable expansion precedence, and task generation.

- [ ] **4.B: Schema Drift Verification in CI**
  - Add a workflow check to ensure `editors/vscode-cmpl/schemas/cmpl.schema.json` is always in sync with root `cmpl.schema.json`.

- [ ] **4.C: Add macOS to CI Build Matrix**
  - Add `macos-latest` to `.github/workflows/build.yml` to verify cross-platform building on Apple Silicon.
