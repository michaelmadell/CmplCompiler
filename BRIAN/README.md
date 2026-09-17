# BRIAN: Memory Bank for CmplPiler

Welcome to **BRIAN**, the Memory Bank documentation suite for the **CmplPiler** repository.

The memory bank captures the foundational architecture, active decisions, technical environment, and development status of CmplPiler. It is designed to maintain project intelligence across development sessions and provide rapid onboarding for human developers and AI pair programmers alike.

---

## Memory Bank Directory Structure

| File | Description | Purpose |
| :--- | :--- | :--- |
| **[projectbrief.md](projectbrief.md)** | Core Foundation | High-level executive summary, core requirements, architectural goals, and system scope. |
| **[productContext.md](productContext.md)** | Problem & Experience | Why this project exists, real-world problems it solves, user personas, and workflows. |
| **[systemPatterns.md](systemPatterns.md)** | Architecture & Design | System architecture, component boundaries, class responsibilities, and design patterns. |
| **[techContext.md](techContext.md)** | Technical Specification | Tech stack (.NET 10, C# 13), toolchains, conditional compilation, dependencies, and packaging. |
| **[activeContext.md](activeContext.md)** | Current State & Decisions | Recent changes, active conventions, architectural decisions, and current focus areas. |
| **[progress.md](progress.md)** | Feature Checklist & Roadmap | Completed features, verification status, known limitations, and future roadmap. |

---

## Quick Reference for Developers & Agents

### Building & Running
- **Build CLI**: `dotnet build CmplPiler.slnx`
- **Run CLI**: `dotnet run --project CmplPiler -- <file.cmpl> [options]`
- **Run GUI (Windows)**: `dotnet run --project CmplPiler -r win-x64`
- **Publish Single-File (Windows)**: `dotnet publish CmplPiler -c Release -r win-x64 --self-contained /p:PublishSingleFile=true`
- **Publish Single-File (Linux)**: `dotnet publish CmplPiler -c Release -r linux-x64 --self-contained /p:PublishSingleFile=true`

### Key Files in Codebase
- **Entry & Dual-Personality Dispatch**: [`CmplPiler/Program.cs`](../CmplPiler/Program.cs)
- **YAML Parser & Validation**: [`CmplPiler/Core/CmplParser.cs`](../CmplPiler/Core/CmplParser.cs)
- **Command & Task Generator**: [`CmplPiler/Core/CommandGenerator.cs`](../CmplPiler/Core/CommandGenerator.cs)
- **Process Execution & Tree Cancellation**: [`CmplPiler/Core/BuildRunner.cs`](../CmplPiler/Core/BuildRunner.cs)
- **Visual Studio / MSVC Locator**: [`CmplPiler/Core/ToolLocator.cs`](../CmplPiler/Core/ToolLocator.cs)
- **CLI Runner & Unicode Box UI**: [`CmplPiler/Cli/CliRunner.cs`](../CmplPiler/Cli/CliRunner.cs)
- **Windows Forms GUI**: [`CmplPiler/Gui/Form1.cs`](../CmplPiler/Gui/Form1.cs)
- **JSON Schema**: [`cmpl.schema.json`](../cmpl.schema.json)
- **VS Code Extension**: [`editors/vscode-cmpl/`](../editors/vscode-cmpl/)
