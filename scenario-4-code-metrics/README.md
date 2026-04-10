# Scenario 4 — Code Metrics: Complexity & Maintainability Analyzer

> 🎯 Paste source code → get cyclomatic complexity, line counts, and maintainability index. Demonstrates pure server-side computation with no external API dependencies.

> 🚧 **Work in Progress** — This scenario is under active development. Code and documentation may be incomplete or change without notice.

## What You'll Learn

- 🎯 **Code complexity analysis** — counting decision points to calculate cyclomatic complexity
- 🎯 **Line metrics computation** — breaking code into lines of code, comments, and blank lines
- 🎯 **Maintainability index calculation** — using Halstead metrics, cyclomatic complexity, and LOC to produce a 0-100 maintainability score
- 🎯 **Pure server-side computation** — running all analysis locally without external APIs or dependencies
- 🎯 **Multi-tool agent patterns** — orchestrating three specialized analysis tools in one agent

## What Are Code Metrics Agents?

A **code metrics agent** is a hosted agent that performs static code analysis server-side. Instead of letting developers guess at code quality or rely on IDE plugins, the agent receives code snippets and returns factual metrics: cyclomatic complexity (how many decision points the code contains), line breakdowns (code vs. comments vs. blank), and a maintainability index (a composite score 0-100 indicating how easy the code is to maintain and modify).

This scenario demonstrates why hosted agents matter: all computation happens in .NET on Foundry's managed infrastructure. No hallucinations, no approximations — just exact metrics.

> **First time here?** Read the [main README](../README.md) for core concepts and architecture overview.

## Architecture

```
┌────────────────────────────────────┐
│  You (REST client)                 │
│  POST /responses                   │
│  {"input": "Analyze this code..."}│
└──────────────┬─────────────────────┘
               │
               ▼
┌────────────────────────────────────┐
│  Hosting Adapter (port 8088)       │
│  (AgentServer SDK)                 │
│  • Protocol translation            │
│  • OpenTelemetry integration       │
└──────────────┬─────────────────────┘
               │
               ▼
┌────────────────────────────────────┐
│  ChatClientAgent                   │
│  • Instructions: "Analyze code"    │
│  • Tools: [3 analysis functions]   │
└──────────────┬─────────────────────┘
               │
      ┌────────┼────────┐
      ▼        ▼        ▼
AnalyzeComplexity  CountLines  CalculateMaintainability
(C# parsing)       (regex)     (Halstead+CC+LOC)
      ↓            ↓           ↓
   Metrics       Breakdown    MI Score
```

## Prerequisites

| Tool | Install |
|------|---------|
| **.NET 10 SDK** | https://dotnet.microsoft.com/download |
| **Azure CLI (`az`)** | https://learn.microsoft.com/cli/azure/install-azure-cli |
| **Azure Developer CLI (`azd`)** | https://aka.ms/install-azd |
| **Docker Desktop** | https://docs.docker.com/get-docker/ |
| **Azure subscription** | With access to Microsoft Foundry |

## Quick Start

### 1. Setup Azure resources

```powershell
./setup.ps1
```

This provisions the Microsoft Foundry project, Azure OpenAI deployment, and supporting infrastructure. You'll be prompted for a subscription and region.

### 2. Log in to Azure

After setup completes, run the `az login` command shown in the output:

```powershell
az login --tenant <your-tenant-id>
```

### 3. Run the agent

```powershell
cd src/code-metrics-agent
dotnet run
```

You'll see:
```
CodeMetricsAgent running on http://localhost:8088
```

## How to Use

Test the agent with any REST client. Open `test.http` in VS Code with the REST Client extension, or use `curl`:

```bash
curl -X POST http://localhost:8088/responses \
  -H "Content-Type: application/json" \
  -d '{"input": "Analyze this C# method for complexity..."}'
```

The agent will:
1. Parse your question and extract the code snippet
2. Call the appropriate analysis tools (`AnalyzeComplexity`, `CountLines`, `CalculateMaintainability`)
3. Return a comprehensive code review with metrics and recommendations

Try these:
- "What's the complexity of this function?"
- "How maintainable is this code on a scale of 0-100?"
- "Count the lines of code in this snippet"

## Function Tools

### `AnalyzeComplexity`

Calculates **cyclomatic complexity** by counting decision points in source code.

| Parameter | Type | Description |
|-----------|------|-------------|
| `sourceCode` | string | The C# code snippet to analyze |

**Returns:** Cyclomatic complexity score (integer ≥ 1) with a list of decision points found.

**How it works:** Counts `if`, `else`, `switch`, `case`, `for`, `foreach`, `while`, `do-while`, `catch`, and ternary operators. Higher scores indicate more complex control flow and increased testing burden.

**Guidance:**
- **1-3:** Simple, easy to test
- **4-6:** Moderate complexity, consider breaking down
- **7-10:** High complexity, strong refactoring candidate
- **>10:** Very high complexity, definitely refactor

---

### `CountLines`

Breaks down source code into **lines of code**, **comment lines**, and **blank lines**.

| Parameter | Type | Description |
|-----------|------|-------------|
| `sourceCode` | string | The code snippet to analyze |

**Returns:** Object with fields:
- `totalLines` — all lines in the snippet
- `codeLines` — lines containing executable code
- `commentLines` — lines with `//` or `/* */` comments
- `blankLines` — empty lines

**Guidance:**
- Aim for a 1:3 ratio of comment lines to code lines for well-documented code
- High blank-line counts may indicate readability issues (too much whitespace)
- Low comment counts suggest potential documentation debt

---

### `CalculateMaintainability`

Produces a **maintainability index** (0-100 scale) using:
- Halstead volume (code entropy)
- Cyclomatic complexity
- Lines of code

| Parameter | Type | Description |
|-----------|------|-------------|
| `sourceCode` | string | The code snippet to analyze |

**Returns:** Maintainability index (0-100) with interpretation.

**Score interpretation:**
- **85-100:** Highly maintainable — green ✅
- **70-84:** Maintainable — yellow ⚠️
- **50-69:** Difficult to maintain — orange 🟠
- **<50:** Very difficult to maintain — red 🔴

---

## Local vs Cloud Mode

| Mode | Command | Where agent runs |
|------|---------|------------------|
| **Local** | `dotnet run` from `src/code-metrics-agent/` | Your machine (debugging + fast iteration) |
| **Cloud** | `./deploy.ps1` | Microsoft Foundry container (production) |

In both modes, the agent calls Azure OpenAI for LLM orchestration. The agent container location is the difference.

## Deploy to Azure

After verifying locally that everything works:

```powershell
./deploy.ps1
```

This:
1. Builds the Docker container
2. Pushes it to Azure Container Registry
3. Deploys it as a hosted agent in Microsoft Foundry

Test your deployed agent in the [Microsoft Foundry Playground](https://ai.azure.com).

## Project Structure

```
scenario-4-code-metrics/
├── src/
│   └── code-metrics-agent/
│       ├── Program.cs          # Single-file agent + analysis tools
│       ├── Dockerfile          # Container definition
│       └── *.csproj            # Project file
├── setup.ps1                   # Provision Azure resources
├── deploy.ps1                  # Deploy agent to Azure
├── cleanup.ps1                 # Tear down Azure resources
├── azure.yaml                  # azd project definition
├── test.http                   # REST Client test requests
├── .env.example                # Environment variable template
└── README.md                   # This file
```

## Troubleshooting

| Problem | Solution |
|---------|----------|
| `dotnet run` fails with "AZURE_OPENAI_ENDPOINT not set" | Run `./setup.ps1` first to provision Azure resources and configure secrets |
| `az login` fails | Ensure you have a valid Azure subscription and access to Microsoft Foundry in your region |
| Port 8088 already in use | Kill the process: `Get-Process -Name "dotnet" \| Stop-Process` or use `netstat -ano \| findstr :8088` |
| Agent doesn't call analysis tools | Check the agent instructions in `Program.cs` — try asking with code inline or attached. The LLM may need clearer formatting. |
| Complexity score seems wrong | Some code patterns (nested ternaries, lambdas) may be counted differently than manual inspection. This is normal — the tool counts decision points, not subjective complexity. |
| Deployed agent unreachable | Verify the agent deployed successfully: `az ai agent show --agent-id <id>` and check Microsoft Foundry for runtime errors |

## Clean Up

Remove all Azure resources and local configuration:

```powershell
./cleanup.ps1
```

This runs `azd down --purge --force` and clears .NET User Secrets.

---

> **↑ Back to [root README](../README.md)** · [Scenario 1 — Intro](../scenario-1-intro/README.md) · [Scenario 2 — Data Crunch](../scenario-2-data-crunch/README.md) · [Scenario 3 — Image Gen](../scenario-3-image-gen/README.md)
