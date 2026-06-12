# CmplPiler

A build orchestrator driven by simple, declarative `.cmpl` (YAML) project
files. Describe *what* to build in named profiles; CmplPiler generates and
runs the right commands for direct compiler invocation, CMake, the .NET CLI,
or MSBuild.

The repository contains three projects:

| Project | Description | Platforms |
| --- | --- | --- |
| `CmplPiler.Core` | Parsing, validation, command generation and build execution | Windows, Linux, macOS |
| `CmplPiler.Cli` | `cmpl` command-line front-end | Windows, Linux, macOS |
| `CmplPiler` | Windows Forms GUI front-end | Windows |

## Getting started

Requires the [.NET 10 SDK](https://dotnet.microsoft.com/download).

```sh
dotnet build CmplPiler.slnx

# Run the CLI
dotnet run --project CmplPiler.Cli -- examples/hello-cpp/hello.cmpl --list
dotnet run --project CmplPiler.Cli -- examples/hello-cpp/hello.cmpl -p gcc-release
```

On Windows you can instead launch the `CmplPiler` GUI, pick a `.cmpl` file,
choose a profile and press **Build Project**.

### CLI usage

```
cmpl <file.cmpl> [options]

  -p, --profile <name>   Build the named profile (default: first profile)
  -l, --list             List the profiles in the file and exit
  -n, --dry-run          Print the commands without running them
  -h, --help             Show this help
```

The CLI exits non-zero on failure, making it suitable for CI pipelines, and
Ctrl+C cancels the build (killing the whole process tree).

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
  (`/I`, `/D`, `/Fe:`) vs GCC-style (`-I`, `-D`, `-o`) switches are chosen
  automatically.
- **`build_type` mapping**: `-c` for dotnet, `-DCMAKE_BUILD_TYPE` /
  `--config` for cmake, `/p:Configuration` for msbuild.
- **dotnet publish**: set `dotnet_publish: true` to run `dotnet publish`
  instead of `dotnet build`.

See [`examples/`](examples/) for working samples, including
[`self-host.cmpl`](examples/self-host.cmpl) which builds this repository's
own CLI with itself:

```sh
dotnet run --project CmplPiler.Cli -- examples/self-host.cmpl -p cli-release
```

## Editor support

Associate `*.cmpl` with YAML and point your editor's YAML language server at
`cmpl.schema.json` for completion and validation. In VS Code:

```json
"yaml.schemas": { "./cmpl.schema.json": "*.cmpl" }
```

## License

MIT — see [LICENSE.txt](LICENSE.txt).
