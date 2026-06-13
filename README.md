# CmplPiler

A build orchestrator driven by simple, declarative `.cmpl` (YAML) project
files. Describe *what* to build in named profiles; CmplPiler generates and
runs the right commands for direct compiler invocation, CMake, the .NET CLI,
or MSBuild.

It is a single application, `cmpl`, built from one project. The **target
platform you publish for** decides the format:

| Publish for… | Framework (auto-selected) | Contents |
| --- | --- | --- |
| a Windows RID (`-r win-*`) | `net10.0-windows` | CLI **plus** the Windows Forms GUI |
| anything else / no RID | `net10.0` | Command-line interface |

There is no separate "GUI build" and "CLI build" to juggle: the runtime
identifier selects the target framework, so a plain build and a Linux
publish produce a portable CLI, while a `win-*` publish additionally
compiles in the GUI. (A single *file* can't be both a Linux and a Windows
executable — those are different formats, and WinForms only runs on
Windows — so you publish one self-contained binary per platform.)

On the Windows build, launching `cmpl` with no arguments (or with `--gui`)
opens the GUI; any other invocation behaves as a normal console tool.
Internally the code is layered as `Core/` (parsing, validation, command
generation, build execution), `Cli/` and `Gui/`, with `Gui/` compiled only
for the Windows target.

## Getting started

Requires the [.NET 10 SDK](https://dotnet.microsoft.com/download). The
project builds on any OS (the Windows target cross-compiles on Linux/macOS
via `EnableWindowsTargeting`).

```sh
dotnet build CmplPiler.slnx

# Run the CLI
dotnet run --project CmplPiler -- examples/hello-cpp/hello.cmpl --list
dotnet run --project CmplPiler -- examples/hello-cpp/hello.cmpl -p gcc-release
```

### Publishing

The same command shape produces one self-contained, single-file binary per
platform — the RID picks CLI vs GUI automatically:

```sh
# Linux CLI (ELF)
dotnet publish CmplPiler -c Release -r linux-x64 --self-contained /p:PublishSingleFile=true

# Windows CLI + GUI (single .exe)
dotnet publish CmplPiler -c Release -r win-x64 --self-contained /p:PublishSingleFile=true
```

> Developing the GUI in Visual Studio? The WinForms designer needs the
> Windows target, so set `RuntimeIdentifier` to `win-x64` (or build with
> `-r win-x64`) — a plain build defaults to the CLI-only `net10.0` target.

### CLI usage

```
cmpl <file.cmpl> [options]

  -p, --profile <name>   Build the named profile (default: first profile)
  -l, --list             List the profiles in the file and exit
  -n, --dry-run          Print the commands without running them
      --gui              Open the graphical interface (Windows builds only)
  -h, --help             Show this help
```

The CLI exits non-zero on failure, making it suitable for CI pipelines, and
Ctrl+C cancels the build (killing the whole process tree). On Windows,
`cmpl --gui path/to/project.cmpl` opens the GUI with the project preloaded.

## The .cmpl format

A `.cmpl` file is YAML validated against [`cmpl.schema.json`](cmpl.schema.json):

```yaml
project_name: "hello"
cmpl_version: "1.0"

environment:          # applied to every build process, usable as ${VAR}
  MY_FLAG: "1"

profiles:
  - name: "gcc-release"
    build_system: "direct"      # direct | cmake | dotnet | msbuild
    toolchain: "gcc"            # direct only: msvc | gcc | clang | custom path
    build_type: "Release"       # mapped per build system
    source_dir: "src"           # relative paths resolve against this file
    output_dir: "build/release" # created automatically if missing
    include_dirs: ["include"]
    defines: ["ENABLE_EXTRA"]
    flags: ["-O2", "-Wall"]
    pre_build:                  # shell commands, run from this file's dir
      - "echo starting ${project_name}"
    post_build:
      - "echo done"
```

Key behaviours:

- **Relative paths** in `source_dir`, `output_dir` and `include_dirs` are
  resolved against the directory containing the `.cmpl` file, so projects
  are relocatable and shareable.
- **Variable expansion**: `${NAME}` tokens in profile strings expand from
  built-ins (`${project_name}`, `${base_dir}`), the `environment` map, then
  OS environment variables.
- **Cross-platform shell hooks**: `pre_build`/`post_build` run under
  `cmd.exe` on Windows and `/bin/sh` elsewhere.
- **Toolchain mapping** for `direct` builds: `msvc` → `cl` (located via a
  Visual Studio developer prompt using `vswhere`), `gcc` → `g++`,
  `clang` → `clang++`, or any custom compiler command/path. MSVC-style
  (`/I`, `/D`, `/Fo`, `/Fe:`) vs GCC-style (`-I`, `-D`, `-o`) switches are
  chosen automatically, and object files land in `output_dir`.
- **MSVC architecture**: the developer prompt is initialized with
  `-arch`/`-host_arch` matching the host OS (VsDevCmd would otherwise
  default to x86 tools). Override the target with `arch: x86 | x64 | arm64`
  on the profile.
- **`build_type` mapping**: `-c` for dotnet, `-DCMAKE_BUILD_TYPE` /
  `--config` for cmake, `/p:Configuration` for msbuild.
- **dotnet publish**: set `dotnet_publish: true` to run `dotnet publish`
  instead of `dotnet build`.

See [`examples/`](examples/) for working samples, including
[`self-host.cmpl`](examples/self-host.cmpl) which builds this repository's
own `cmpl` with itself:

```sh
dotnet run --project CmplPiler -- examples/self-host.cmpl -p cli-release
```

## Editor support

Associate `*.cmpl` with YAML and point your editor's YAML language server at
`cmpl.schema.json` for completion and validation. In VS Code:

```json
"yaml.schemas": { "./cmpl.schema.json": "*.cmpl" }
```

## License

MIT — see [LICENSE.txt](LICENSE.txt).
