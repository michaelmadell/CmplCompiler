# Product Context: CmplPiler

## Background & Motivation
Modern software development frequently involves multi-language codebases, cross-platform compilation targets, and disparate build systems. A single project may contain:
- C/C++ native modules needing GCC, Clang, or MSVC.
- CMake-driven dependencies.
- .NET web APIs or desktop tools.
- Complex pre-build asset generation and post-build packaging steps.

Developers often find themselves writing brittle, fragmented bash or batch scripts, juggling different command-line flags, remembering Visual Studio Developer Command Prompt incantations, or dealing with heavy, opaque build tools.

**CmplPiler** bridges this gap by offering a lightweight, declarative build orchestrator. It abstracts away the idiosyncrasies of toolchains while remaining transparent and predictable.

---

## Target Audience & Personas
1. **Systems & Native Developers**:
   - Developers writing C++ who want to quickly test builds across MSVC, GCC, and Clang without writing CMake files for simple utilities.
2. **Polyglot & Full-Stack Engineers**:
   - Teams maintaining mixed-stack repositories (.NET + C++ + scripts) needing a single, standardized entry point for building artifacts.
3. **CI/CD Engineers**:
   - Automated build pipelines requiring predictable exit codes, dry-run inspection, standard error/output streaming, and fast, self-contained execution without heavy SDK installation dependencies where possible.
4. **Desktop Developers & Quick-Inspection Users**:
   - Developers on Windows who prefer a graphical interface to quickly select profiles, click "Build", and view live logs without navigating command prompts.

---

## Key Problems Solved

### 1. The MSVC Developer Environment Problem
- *Problem*: Compiling with MSVC's `cl.exe` requires launching a special Developer Command Prompt or running `VsDevCmd.bat`. Outside this environment, `cl.exe` is not on the system `PATH`. Additionally, VsDevCmd defaults to targeting x86 unless explicitly instructed.
- *Solution*: CmplPiler automatically finds the newest Visual Studio installation using `vswhere.exe`, detects the host CPU architecture (`x64`, `arm64`, `x86`), and prefixes direct MSVC build tasks with a properly parameterized `call "VsDevCmd.bat" -arch=<target> -host_arch=<host>`.

### 2. Cross-Toolchain Flag Differences
- *Problem*: MSVC uses `/I`, `/D`, `/Fo`, `/Fe:` while GCC/Clang use `-I`, `-D`, `-o`. Remembering and converting flags across platforms is tedious.
- *Solution*: `.cmpl` files allow developers to declare `include_dirs`, `defines`, and `flags` once; CmplPiler maps them to the appropriate compiler syntax automatically based on the chosen `toolchain`.

### 3. Dual GUI & CLI Dilemma
- *Problem*: Software typically requires two separate binaries or projects if it wants both a GUI and a CLI. In .NET, a `WinExe` normally swallows console output when run from a terminal, while an `Exe` pops up an ugly console window when launched from the Windows desktop.
- *Solution*: CmplPiler uses conditional target frameworks and Windows native P/Invoke (`AttachConsole(-1)`). When compiled for Windows, launching with no arguments or `--gui` opens a clean Windows Forms window; launching with build arguments attaches to the parent terminal and behaves as a native CLI tool.

### 4. Build Script Relocatability
- *Problem*: Scripts frequently break when run from outside their containing folder or when repositories are cloned into different directory depths.
- *Solution*: CmplPiler resolves all relative paths (`source_dir`, `output_dir`, `include_dirs`) against the directory containing the `.cmpl` configuration file, ensuring consistent behavior regardless of the current working directory.

---

## User Experience Workflows

### CLI Workflow
```bash
# Inspect available build profiles
cmpl project.cmpl --list

# Preview generated commands without executing them
cmpl project.cmpl -p release --dry-run

# Run a build with real-time streaming output and colored status boxes
cmpl project.cmpl -p release
```

### GUI Workflow (Windows)
1. Double-click `cmpl.exe` or execute `cmpl --gui project.cmpl`.
2. Browse or preload `.cmpl` project.
3. Select desired build profile from the dropdown.
4. Click **Build** to monitor real-time output in the log viewer.
5. Click **Cancel** at any time to gracefully terminate the entire running process tree.

### Visual Studio Code Integration
- Authors editing `.cmpl` files benefit from zero-configuration schema validation and autocompletion via the `CMPL Build Files` extension (`editors/vscode-cmpl`), powered by `cmpl.schema.json`.
