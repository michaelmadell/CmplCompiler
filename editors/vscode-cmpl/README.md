# CMPL Build Files

Zero-config editor support for [`.cmpl`](https://github.com/michaelmadell/cmplcompiler)
build-orchestrator files in Visual Studio Code.

`.cmpl` files are YAML, so this extension teaches VS Code to treat `*.cmpl`
as YAML and automatically applies the official CMPL JSON schema. You get:

- **Syntax highlighting** — full YAML highlighting for keys, strings,
  numbers, booleans and comments.
- **Validation** — red squiggles for invalid `build_system`, `arch` or
  `build_type` values, missing required fields (`project_name`, profile
  `name`/`build_system`/`source_dir`), and wrong value types.
- **Autocomplete** — suggestions for every key and enum value as you type.
- **Hover docs** — inline descriptions for each field, straight from the
  schema.

No settings to configure — open a `.cmpl` file and it just works.

## Requirements

Validation, autocomplete and hover are powered by the
[YAML extension by Red Hat](https://marketplace.visualstudio.com/items?itemName=redhat.vscode-yaml),
which is declared as a dependency and installed automatically.

## How it works

The extension registers the `.cmpl` extension with VS Code's built-in YAML
language and contributes the CMPL schema via the `yamlValidation`
contribution point, associating it with `*.cmpl`. Because `.cmpl` files are
handled as YAML, the Red Hat YAML language server provides validation and
IntelliSense against the schema.

> A `.cmpl` file is shown as **YAML** in the language indicator — that is
> intentional and is what enables schema validation.

## Example

```yaml
project_name: "hello"
cmpl_version: "1.0"

profiles:
  - name: "gcc-release"
    build_system: "direct"   # autocompletes: direct | cmake | dotnet | msbuild
    toolchain: "gcc"
    build_type: "Release"    # autocompletes: Debug | Release | RelWithDebInfo | MinSizeRel
    source_dir: "src"
    output_dir: "build/gcc-release"
    flags: ["-O2", "-Wall"]
```

## License

MIT
