# System Patterns: CmplPiler

## Architectural Overview

CmplPiler is organized into distinct logical layers within a single C# codebase, with conditional compilation used to isolate platform-specific GUI components.

```
                  ┌───────────────────────────────┐
                  │          Program.cs           │
                  │   [STAThread] Entry Point     │
                  └──────────────┬────────────────┘
                                 │
                 ┌───────────────┴───────────────┐
                 │ (Windows & wants GUI?)        │
                YES                             NO
                 ▼                               ▼
       ┌──────────────────┐            ┌──────────────────┐
       │   Gui/Form1.cs   │            │   CliRunner.cs   │
       │ (Windows Forms)  │            │ (CLI Controller) │
       └─────────┬────────┘            └────────┬─────────┘
                 │                              │
                 └───────────────┬──────────────┘
                                 ▼
                     ┌───────────────────────┐
                     │     CmplPiler.Core    │
                     └───────────────────────┘
                                 │
        ┌────────────────────────┼────────────────────────┐
        ▼                        ▼                        ▼
┌──────────────┐         ┌───────────────┐        ┌──────────────┐
│  CmplParser  │         │CommandGenerator│       │ BuildRunner  │
│ (Deserialize,│         │ (Build task   │        │(Async Process│
│Validate, Exp)│         │  generation)  │        │Execution/Tree│
└──────────────┘         └───────┬───────┘        │Cancellation) │
                                 │                └──────────────┘
                                 ▼
                         ┌───────────────┐
                         │  ToolLocator  │
                         │(vswhere/MSVC) │
                         └───────────────┘
```

---

## Component Responsibilities

### 1. Entry & Bootstrapping (`Program.cs`, `ConsoleInterop.cs`)
- **Single Binary Multi-Mode**:
  - `Program.Main` checks `args`: if `args.Length == 0` or contains `--gui`, it boots the Windows Forms UI via `Application.Run(new Form1(...))` (guarded by `#if WINDOWS`).
  - If console arguments are present, it invokes `ConsoleInterop.AttachToParentConsole()` (P/Invoke to `AttachConsole(-1)`) so stdout/stderr attach to the launching terminal, then delegates to `CliRunner.RunAsync`.
  - On non-Windows builds, the GUI code is stripped at compile time, and all calls proceed directly to `CliRunner`.

### 2. Core Domain Models (`Core/CmplModels.cs`)
- **`CmplProject`**: Root project representation containing `ProjectName`, `CmplVersion`, `Environment` dictionary, and `Profiles` list. Holds a `[YamlIgnore]` `BaseDirectory` property that stores the directory path of the source `.cmpl` file.
- **`CmplProfile`**: Specific build configuration definition:
  - `BuildSystem`: `"direct"`, `"cmake"`, `"dotnet"`, or `"msbuild"`.
  - `Toolchain`: Compiler choice for direct builds (`"msvc"`, `"gcc"`, `"clang"`, or custom command/path).
  - `Arch`: Architecture target for MSVC (`"x86"`, `"x64"`, `"arm64"`).
  - Directory & file mappings: `SourceDir`, `OutputDir`, `IncludeDirs`, `Defines`, `Flags`, `PreBuild`, `PostBuild`.
  - Flags like `DotnetPublish`.
- **`BuildTask`**: Unit of work to be executed. Holds `Command`, string `Arguments`, optional `List<string> ArgumentList` (preferred for Unix argument passing), and `WorkingDirectory`.
- **`CmplValidationException`**: Domain-specific exception thrown on validation or configuration failure.

### 3. Parser, Validator & Expander (`Core/CmplParser.cs`)
- **Deserialization**:
  - Uses `YamlDotNet.Serialization.DeserializerBuilder` configured with `UnderscoredNamingConvention.Instance` and `IgnoreUnmatchedProperties()`.
- **Validation**:
  - Enforces required fields (`project_name`, non-empty `profiles`, profile `name`, valid `build_system`, `source_dir`).
  - Enforces build-system-specific prerequisites (e.g., direct builds require `toolchain` and `output_dir`; CMake requires `output_dir`).
- **Variable Expansion**:
  - Regex pattern: `\$\{([A-Za-z_][A-Za-z0-9_]*)\}`.
  - Three-tier lookup cascade:
    1. Built-in variables: `project_name`, `base_dir`.
    2. Project-level `environment` map.
    3. Host OS environment variables (`Environment.GetEnvironmentVariable`).
  - Expands strings in `source_dir`, `output_dir`, `include_dirs`, `defines`, `flags`, `pre_build`, and `post_build`.

### 4. Command Generator (`Core/CommandGenerator.cs`)
- Translates high-level profile declarations into an ordered `List<BuildTask>`:
  1. `PreBuild` tasks (wrapped in platform shell).
  2. Main build system task(s) (`direct`, `cmake`, `dotnet`, or `msbuild`).
  3. `PostBuild` tasks (wrapped in platform shell).
- **Direct Compiler Translation**:
  - Resolves compiler alias (`msvc` -> `cl`, `gcc` -> `g++`, `clang` -> `clang++`).
  - Formats flags appropriately:
    - MSVC: `/I"<path>"`, `/D<name>`, `/Fo"<outdir>/"`, `/Fe:"<outfile>"`.
    - GCC/Clang: `-I"<path>"`, `-D<name>`, `-o "<outfile>"`.
  - Adds glob pattern `"<sourceDir>"/*.cpp`.
  - Wraps MSVC tasks in `call "VsDevCmd.bat" -arch=... -host_arch=... && <commandLine>` if running on Windows.
- **CMake Task Generation**:
  - Emits configuration step: `cmake -B "<buildDir>" -S "<sourceDir>" [-DCMAKE_BUILD_TYPE=...]`.
  - Emits build step: `cmake --build "<buildDir>" [--config ...]`.
- **.NET & MSBuild Task Generation**:
  - Handles `dotnet build` vs `dotnet publish`.
  - MSBuild wraps in developer command prompt on Windows; falls back to `dotnet msbuild` on Unix.
- **Platform Shell Wrapper (`ShellTask`)**:
  - Windows: `cmd.exe /c <command>`
  - Unix: `/bin/sh -c <command>`

### 5. Tool Locator (`Core/ToolLocator.cs`)
- Encapsulates `vswhere.exe` execution:
  - Invokes `%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe` with `-latest -property installationPath`.
  - Locates `Common7\Tools\VsDevCmd.bat`.
  - Gracefully returns `null` on non-Windows hosts or when Visual Studio is not installed.

### 6. Build Runner (`Core/BuildRunner.cs`)
- **Asynchronous Pipeline Execution**:
  - Iterates through `BuildTask` sequence sequentially.
  - Automatically ensures output directories exist before execution (`Directory.CreateDirectory`).
  - Standard output and standard error are redirected and streamed asynchronously using `Process.OutputDataReceived` and `Process.ErrorDataReceived`.
  - Communicates progress via C# events: `event Action<string>? OutputReceived` and `event Action<string>? ErrorReceived`.
- **Cancellation & Process Tree Cleanup**:
  - Respects `CancellationToken`.
  - When cancelled, executes `process.Kill(entireProcessTree: true)` to ensure child compiler processes (e.g. `cl.exe`, `link.exe`, child compilers spawned by CMake) are terminated immediately and cleanly without leaving orphan processes.

### 7. CLI Runner (`Cli/CliRunner.cs`)
- Formats command output and status cards using UTF-8 box-drawing characters:
  - Header and usage box.
  - Build Succeeded banner in bold green.
  - Build Failed banner with dynamically centered exit code in bold red.
- Captures Ctrl+C (`Console.CancelKeyPress`) and forwards cancellation to `BuildRunner`.
- Exit codes:
  - `0`: Success.
  - `1`: Validation error, argument error, or build task failure.
  - `130`: Build cancelled by user (standard Unix SIGINT exit convention).

### 8. Graphical User Interface (`Gui/Form1.cs`)
- WinForms window with project loading (`OpenFileDialog` or preloaded file argument).
- Profile selection ComboBox.
- Build and Cancel buttons with state toggling.
- Thread-safe UI logging via `Control.InvokeRequired` appending to a scrollable `TextBox`.

---

## Design Patterns & Key Idioms
- **Pipeline Pattern**: `BuildRunner` orchestrates tasks as a strictly ordered pipeline (Pre-Build -> Compilation/Build -> Post-Build), failing on the first non-zero exit code.
- **Strategy / Routing Pattern**: `CommandGenerator` routes the task generation based on `build_system` value (`direct`, `cmake`, `dotnet`, `msbuild`).
- **Observer / Event Pattern**: `BuildRunner` decouples process I/O from consumer presentation by firing `OutputReceived` and `ErrorReceived` events, consumed identically by `CliRunner` (console) and `Form1` (WinForms).
- **Separation of Concerns**: Parsing/Validation (`CmplParser`), Command Construction (`CommandGenerator`), and Process Execution (`BuildRunner`) have zero UI dependencies and are fully reusable.
