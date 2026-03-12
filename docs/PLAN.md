# Implementation Plan — Foundry Hosted Agents Demo

> **Goal**: A multi-scenario demo repo showing how to build, run, and deploy Microsoft Foundry **Hosted Agents** using .NET 10 and C#.
>
> **Audience**: Developers who want to see hosted agents in action — from a 5-minute intro to a full end-to-end application.
>
> **Philosophy**: Two scenarios, two levels of depth. Scenario 1 teaches the concept. Scenario 2 shows what you'd actually build.

---

## Table of Contents

1. [Repo Vision — Two Scenarios](#1-repo-vision--two-scenarios)
2. [Repo Structure](#2-repo-structure)
3. [Scenario 1 — Intro (Time Zone Agent)](#3-scenario-1--intro-time-zone-agent)
4. [Scenario 2 — Data Crunch (End-to-End Application)](#4-scenario-2--data-crunch-end-to-end-application)
   - [Architecture](#41-architecture)
   - [Aspire AppHost](#42-aspire-apphost)
   - [Data Crunch Agent](#43-data-crunch-agent)
   - [Blazor Frontend](#44-blazor-frontend)
   - [Local vs Cloud Mode](#45-local-vs-cloud-mode)
   - [File Structure](#46-file-structure)
   - [Demo Flow](#47-demo-flow)
5. [Architecture Decisions](#5-architecture-decisions)
6. [Prerequisites](#6-prerequisites)
7. [Work Breakdown](#7-work-breakdown)
8. [Implementation Order](#8-implementation-order)
9. [Repo Improvement Suggestions](#9-repo-improvement-suggestions)
10. [Reference Links](#10-reference-links)
11. [Key Risks & Mitigations](#11-key-risks--mitigations)

---

## 1. Repo Vision — Two Scenarios

This repo teaches hosted agents through two progressively complex scenarios:

| | Scenario 1: Intro | Scenario 2: Data Crunch |
|---|---|---|
| **Goal** | Understand hosted agents | Build a real app with hosted agents |
| **Time** | 5-minute demo | 15-minute demo or self-paced lab |
| **Agent** | Time Zone Agent (1 tool) | Data Crunch Agent (3 tools, CSV analysis) |
| **Frontend** | curl / test.http | Blazor Server app (file upload, charts, tables) |
| **Orchestration** | None (standalone) | .NET Aspire (service discovery, dashboard) |
| **Deploy** | azd scripts (optional) | azd scripts + Aspire deploy support |
| **Dual mode** | Local only | Local agent OR cloud-deployed agent (toggle) |

**Why two scenarios?** Scenario 1 is the hook — it removes all friction and gets someone to their first hosted agent in 5 minutes. Scenario 2 is the payoff — it shows how you'd actually build a production-like application with a hosted agent as the backend brain. Together they cover the full journey.

---

## 2. Repo Structure

### Decision: Scenario-per-folder at the root

Each scenario is a self-contained folder with its own README, source code, and scripts. Shared docs live in `docs/`. No shared libraries — scenarios are independent so you can clone and navigate to just the one you care about.

**Why not `src/` with subfolders?** A flat scenario layout matches how Azure Samples repos organize multi-scenario content (e.g., `azure-ai-agent-samples`). Each folder is a standalone experience. A developer can `cd scenario-2-data-crunch/` and forget the rest of the repo exists.

```
foundry-hosted-agents-dotnet-demo/
├── README.md                                    # Repo overview: what's here, pick a scenario
├── LICENSE                                      # MIT
├── .gitignore                                   # .NET + azd + Aspire ignores
├── foundry-hosted-agents-dotnet-demo.slnx                        # Solution file (all projects, both scenarios)
│
├── docs/
│   ├── PLAN.md                                  # This plan
│   ├── SCENARIOS.md                             # Scenario proposals and design criteria
│   └── ARCHITECTURE.md                          # Cross-scenario architecture notes (optional)
│
├── scenario-1-intro/                            # ── Scenario 1: Time Zone Agent ──
│   ├── README.md                                # Self-contained: prereqs, setup, demo, cleanup
│   ├── setup.ps1                                # Provision Azure resources
│   ├── deploy.ps1                               # Deploy to Azure (optional)
│   ├── cleanup.ps1                              # Tear down Azure resources
│   ├── test.http                                # REST Client test requests
│   └── src/
│       └── HostedAgent/
│           ├── HostedAgent.csproj
│           ├── Program.cs                       # Time Zone Agent (~60 lines)
│           ├── Dockerfile
│           └── agent.yaml
│
├── scenario-2-data-crunch/                      # ── Scenario 2: Data Crunch (Aspire) ──
│   ├── README.md                                # Self-contained: prereqs, setup, demo, cleanup
│   ├── setup.ps1                                # Provision Azure resources
│   ├── deploy.ps1                               # Deploy to Azure (optional)
│   ├── cleanup.ps1                              # Tear down Azure resources
│   └── src/
│       ├── DataCrunchAgent/                     # The hosted agent
│       │   ├── DataCrunchAgent.csproj
│       │   ├── Program.cs                       # Agent with 3 function tools
│       │   ├── Tools/
│       │   │   ├── DataParser.cs                # ParseData tool
│       │   │   ├── StatisticsCalculator.cs      # ComputeStatistics tool
│       │   │   └── OutlierDetector.cs           # DetectOutliers tool
│       │   ├── Dockerfile
│       │   └── agent.yaml
│       │
│       ├── DataCrunch.Web/                      # Blazor Server frontend
│       │   ├── DataCrunch.Web.csproj
│       │   ├── Program.cs                       # Blazor Server setup + HttpClient DI
│       │   ├── appsettings.json                 # Agent endpoint config (local/cloud toggle)
│       │   ├── appsettings.Development.json     # Local dev overrides
│       │   ├── Components/
│       │   │   ├── App.razor                    # Root component
│       │   │   ├── Routes.razor                 # Router
│       │   │   ├── Layout/
│       │   │   │   ├── MainLayout.razor         # App shell layout
│       │   │   │   └── NavMenu.razor            # Side navigation
│       │   │   └── Pages/
│       │   │       ├── Home.razor               # Landing page with instructions
│       │   │       └── Analyze.razor             # Main page: upload CSV, see results
│       │   ├── Services/
│       │   │   └── AgentService.cs              # HTTP client wrapper for agent calls
│       │   └── wwwroot/
│       │       └── css/
│       │           └── app.css                  # Minimal custom styles
│       │
│       ├── DataCrunch.AppHost/                  # Aspire orchestrator
│       │   ├── DataCrunch.AppHost.csproj
│       │   └── Program.cs                       # Wires agent + web frontend
│       │
│       └── DataCrunch.ServiceDefaults/          # Aspire shared defaults
│           ├── DataCrunch.ServiceDefaults.csproj
│           └── Extensions.cs                    # OpenTelemetry, health checks, resilience
```

### What moved from the current repo

| Current location | New location | Notes |
|---|---|---|
| `src/HostedAgent/` | `scenario-1-intro/src/HostedAgent/` | Moved as-is |
| `setup.ps1`, `deploy.ps1`, `cleanup.ps1` | `scenario-1-intro/` | Each scenario has its own scripts |
| `test.http` | `scenario-1-intro/test.http` | Stays with its scenario |
| `README.md` | Rewritten as repo-level overview | Points to both scenario READMEs |
| `docs/PLAN.md` | `docs/PLAN.md` | Updated (this file) |

---

## 3. Scenario 1 — Intro (Time Zone Agent)

Scenario 1 is the current implementation, moved to its own folder. **No code changes** — just relocated.

- **Agent**: Time Zone Agent with `GetCurrentDateTime` function tool
- **Pattern**: `ChatClientAgent` + hosting adapter + `RunAIAgentAsync()`
- **Demo**: 5 minutes — code walkthrough → `dotnet run` → curl → (optional) deploy
- **Details**: See the existing Scenario 1 README (to be moved to `scenario-1-intro/README.md`)

All architecture decisions (AD-01 through AD-07) from the original plan still apply.

---

## 4. Scenario 2 — Data Crunch (End-to-End Application)

### 4.1 Architecture

```
┌─────────────────────────────────────────────────────────────────────────┐
│                        .NET Aspire AppHost                              │
│              (orchestrates all services, provides dashboard)            │
│                                                                         │
│  ┌──────────────────────────┐        ┌──────────────────────────────┐  │
│  │    DataCrunch.Web         │        │     DataCrunchAgent          │  │
│  │    (Blazor Server)        │───────▶│     (Hosted Agent)           │  │
│  │                           │  HTTP  │                              │  │
│  │  • File upload (CSV)      │  POST  │  • ParseData tool            │  │
│  │  • Results table          │ /responses │  • ComputeStatistics tool │  │
│  │  • Statistics cards       │        │  • DetectOutliers tool       │  │
│  │  • Outlier highlights     │◀───────│                              │  │
│  │  • Local/Cloud toggle     │  JSON  │  Port: 8088                  │  │
│  │                           │        │                              │  │
│  │  Port: 5000               │        │  ChatClientAgent +           │  │
│  └──────────────────────────┘        │  Hosting Adapter             │  │
│                                       └──────────────────────────────┘  │
│                                                                         │
│  Aspire Dashboard: http://localhost:15888                               │
│  • Service health • Distributed traces • Structured logs               │
└─────────────────────────────────────────────────────────────────────────┘
```

#### Key Design Decisions

**SD-01: Blazor Server (not WebAssembly)**

Blazor Server because:
1. **Server-side rendering** — no WASM download, instant load
2. **Direct HttpClient access** — call the agent from server-side C# code with no CORS concerns
3. **File upload is server-side** — CSV stays on the server, gets forwarded to the agent. No browser memory limits for large files.
4. **Aspire integration is seamless** — Aspire injects `HttpClient` via service discovery. Blazor Server lives in the same orchestration boundary.
5. **Simpler for a demo** — one runtime model, no separate API project needed

WebAssembly would require a separate backend API (because WASM runs in the browser and can't call the agent directly without CORS). That's an extra project and extra complexity for zero demo benefit.

**SD-02: Blazor calls the agent directly (no API gateway)**

The Blazor Server app calls the agent's `/responses` endpoint directly via `HttpClient`. No intermediate API layer.

**Why**: This is a demo. An API gateway adds a project, adds routing, adds indirection — all for no benefit in a demo scenario. The Blazor Server backend *is* the server. It can call the agent directly. In production you'd likely add an API layer for auth, rate limiting, etc. — but here, simplicity wins.

**SD-03: Data flow — CSV upload to results**

```
User browser                Blazor Server              Data Crunch Agent
     │                           │                            │
     │  1. Select CSV file       │                            │
     │  ──────────────────▶      │                            │
     │                           │  2. Read file content       │
     │                           │  3. POST /responses         │
     │                           │  ───────────────────────▶   │
     │                           │     { "input": "Analyze     │
     │                           │       this data: <CSV>" }   │
     │                           │                            │
     │                           │     Agent calls ParseData,  │
     │                           │     ComputeStatistics,      │
     │                           │     DetectOutliers tools     │
     │                           │                            │
     │                           │  4. JSON response           │
     │                           │  ◀───────────────────────   │
     │  5. Render results        │                            │
     │  ◀──────────────────      │                            │
     │  (table, stats, outliers) │                            │
```

The CSV content is sent as part of the agent prompt. The agent uses its function tools to parse, compute, and detect — then responds with a natural language summary plus structured data. The Blazor app parses the response and renders it as tables, stat cards, and highlighted outliers.

**File size consideration**: For a demo, CSV files will be small (< 100 rows). The entire content fits in a single prompt. For production, you'd stream the file or use a chunking strategy — but that's out of scope.

**SD-04: Aspire for orchestration (not just "nice to have")**

Aspire provides real value even in a demo:
1. **Service discovery** — the Blazor app finds the agent via `http://datacrunchagent"` instead of hardcoded `localhost:8088`. Config-free.
2. **Dashboard** — shows both services, their logs, traces, and health in one view. This is the demo's "wow" moment for the Aspire story.
3. **One command to run everything** — `dotnet run --project DataCrunch.AppHost` starts both the agent and the frontend.
4. **OpenTelemetry** — distributed traces show the full request flow: browser → Blazor → agent → tool calls → response. Visible in the dashboard.

### 4.2 Aspire AppHost

The AppHost project is the Aspire orchestrator. It wires together the agent and the frontend.

```csharp
// DataCrunch.AppHost/Program.cs
var builder = DistributedApplication.CreateBuilder(args);

// The hosted agent — runs on port 8088
var agent = builder.AddProject<Projects.DataCrunchAgent>("datacrunchagent");

// The Blazor frontend — gets a reference to the agent for service discovery
var web = builder.AddProject<Projects.DataCrunch_Web>("datacrunchweb")
    .WithReference(agent)
    .WithExternalHttpEndpoints();

builder.Build().Run();
```

**What the Aspire Dashboard shows**:
- Two services: `datacrunchagent` (port 8088) and `datacrunchweb` (port 5000)
- Distributed traces: full request lifecycle from Blazor → agent → tool execution → response
- Structured logs: agent tool invocations, model calls, response times
- Health checks: both services green/red status

**NuGet packages for AppHost**:
| Package | Purpose |
|---|---|
| `Aspire.Hosting.AppHost` | Aspire orchestration host |
| `Aspire.Hosting.Projects` | Project reference support |

### 4.3 Data Crunch Agent

Reuses the same `ChatClientAgent` + hosting adapter pattern from Scenario 1. The only difference: three function tools instead of one.

#### Function Tools

Tools live in separate files under `Tools/` for readability (the Scenario 1 style of everything-in-Program.cs doesn't scale to 3 tools with real logic):

**`Tools/DataParser.cs`** (~30 lines)
```csharp
[Description("Parses CSV or JSON array data and returns it as a normalized table with column names and row count")]
static string ParseData(
    [Description("Raw CSV or JSON array data")] string rawData)
```
- Splits CSV by lines, detects headers, counts rows/columns
- Returns structured summary: column names, row count, first 5 rows as preview
- Handles both CSV and JSON array input

**`Tools/StatisticsCalculator.cs`** (~40 lines)
```csharp
[Description("Computes descriptive statistics for a numeric column: count, min, max, mean, median, std dev, p25, p75")]
static string ComputeStatistics(
    [Description("Comma-separated numeric values")] string values,
    [Description("Name of the column being analyzed")] string columnName)
```
- Pure C# math — `double[]` operations
- Returns: count, min, max, mean, median, standard deviation, P25, P75

**`Tools/OutlierDetector.cs`** (~20 lines)
```csharp
[Description("Detects statistical outliers using the IQR method and returns any values outside 1.5x the interquartile range")]
static string DetectOutliers(
    [Description("Comma-separated numeric values")] string values,
    [Description("Name of the column being analyzed")] string columnName)
```
- IQR method: Q1, Q3, IQR = Q3 - Q1, fences at Q1 - 1.5×IQR and Q3 + 1.5×IQR
- Returns list of outlier values and their positions

#### Program.cs (~40 lines)

```csharp
// DataCrunchAgent/Program.cs — same pattern as Scenario 1
var endpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT")
    ?? throw new InvalidOperationException("AZURE_OPENAI_ENDPOINT is not set.");
var deploymentName = Environment.GetEnvironmentVariable("AZURE_OPENAI_DEPLOYMENT_NAME") ?? "gpt-5-mini";

var chatClient = new AzureOpenAIClient(new Uri(endpoint), new DefaultAzureCredential())
    .GetChatClient(deploymentName)
    .AsIChatClient()
    .AsBuilder()
    .Build();

var agent = new ChatClientAgent(chatClient,
    name: "DataCrunchAgent",
    instructions: """
        You are a data analysis assistant. When given CSV or JSON data, use your tools to:
        1. First parse the data to understand its structure
        2. Compute statistics for numeric columns
        3. Detect outliers if appropriate
        Present results clearly with tables and highlight any anomalies.
        """,
    tools: [
        AIFunctionFactory.Create(DataParser.ParseData),
        AIFunctionFactory.Create(StatisticsCalculator.ComputeStatistics),
        AIFunctionFactory.Create(OutlierDetector.DetectOutliers)
    ])
    .AsBuilder()
    .Build();

await agent.RunAIAgentAsync(telemetrySourceName: "Agents");
```

#### NuGet Packages (same as Scenario 1)

| Package | Version | Purpose |
|---|---|---|
| `Azure.AI.AgentServer.AgentFramework` | 1.0.0-beta.10 | Hosting adapter |
| `Azure.AI.OpenAI` | 2.8.0-beta.1 | Azure OpenAI client |
| `Azure.Identity` | 1.19.0 | Azure auth |
| `Microsoft.Extensions.AI.OpenAI` | 10.4.0 | IChatClient abstraction |

Plus Aspire service defaults:
| Package | Purpose |
|---|---|
| `Aspire.ServiceDefaults` | OpenTelemetry, health checks, resilience |

### 4.4 Blazor Frontend

#### Pages

**`Home.razor`** — Landing page
- Brief description of what the Data Crunch Agent does
- Link/button to navigate to the Analyze page
- Shows current mode (Local / Cloud) with indicator

**`Analyze.razor`** — Main interactive page
- **File upload area**: `<InputFile>` component for CSV selection. Shows file name and size after selection.
- **"Analyze" button**: Sends CSV to the agent via `AgentService`
- **Loading state**: Spinner + "Crunching your data..." while waiting for agent response
- **Results display**:
  - **Data preview table**: First 5–10 rows of the parsed CSV (rendered as a `<table>`)
  - **Statistics cards**: One card per numeric column showing count, min, max, mean, median, std dev (Bootstrap card layout)
  - **Outlier alerts**: Highlighted values that exceed the IQR fences, with visual emphasis (yellow/red badges)
  - **Agent narrative**: The full natural language response from the agent (the "story" around the data)
- **Mode indicator**: Small badge showing "🟢 Local Agent" or "☁️ Cloud Agent"

#### Services

**`Services/AgentService.cs`**
```csharp
public class AgentService(HttpClient httpClient, IConfiguration config)
{
    public async Task<AgentResponse> AnalyzeAsync(string csvContent)
    {
        var payload = new { input = $"Analyze this CSV data:\n{csvContent}" };
        var response = await httpClient.PostAsJsonAsync("/responses", payload);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<AgentResponse>();
    }
}
```

The `HttpClient` base address is set by Aspire service discovery (local mode) or by `appsettings.json` (cloud mode). See [4.5 Local vs Cloud Mode](#45-local-vs-cloud-mode).

#### UI Framework

- **Bootstrap 5** (ships with Blazor template) — no extra CSS frameworks needed
- **No JavaScript charting library** — for the initial version, use HTML tables and Bootstrap cards. Charts can be added later with a library like Chart.js or Plotly if needed. Keep it simple for the demo.

### 4.5 Local vs Cloud Mode

**One config value** controls where the Blazor app sends requests:

```json
// appsettings.json
{
  "AgentMode": "Local",
  "AgentEndpoints": {
    "Local": "http://datacrunchagent",
    "Cloud": "https://<your-deployed-agent>.foundry.azure.com"
  }
}
```

**How it works**:

| Mode | Value | What happens |
|---|---|---|
| `Local` | `"Local"` | Blazor calls the agent via Aspire service discovery (`http://datacrunchagent`). Both run via AppHost. |
| `Cloud` | `"Cloud"` | Blazor calls the deployed agent URL. Only the Blazor app runs locally (or is also deployed). |

**Toggle mechanisms** (pick any):

1. **Config file**: Change `"AgentMode": "Cloud"` in `appsettings.json` → restart
2. **Environment variable**: `AGENT_MODE=Cloud dotnet run` — overrides config
3. **UI toggle** (stretch goal): A switch in the Blazor nav bar that sets the mode at runtime (swaps the `HttpClient` base address). Nice for demos but not required for v1.

**Registration in DI**:

```csharp
// DataCrunch.Web/Program.cs
var agentMode = builder.Configuration["AgentMode"] ?? "Local";
var agentUrl = builder.Configuration[$"AgentEndpoints:{agentMode}"];

builder.Services.AddHttpClient<AgentService>(client =>
{
    client.BaseAddress = new Uri(agentUrl);
});
```

When running via Aspire (`dotnet run --project DataCrunch.AppHost`), the `Local` endpoint uses Aspire's service discovery name. The `WithReference(agent)` call in the AppHost injects the actual endpoint URL.

### 4.6 File Structure

Every file in Scenario 2, with purpose:

```
scenario-2-data-crunch/
├── README.md                          # Scenario 2 guide: prereqs, run, demo, deploy
├── setup.ps1                          # Provision Azure resources (same pattern as S1)
├── deploy.ps1                         # Deploy agent to Azure (same pattern as S1)
├── cleanup.ps1                        # Tear down Azure resources
│
└── src/
    ├── DataCrunchAgent/               # ── The Hosted Agent ──
    │   ├── DataCrunchAgent.csproj     # net10.0, same NuGet refs as S1 + ServiceDefaults
    │   ├── Program.cs                 # Agent setup: 3 tools, instructions, RunAIAgentAsync
    │   ├── Tools/
    │   │   ├── DataParser.cs          # ParseData: CSV/JSON → structured table summary
    │   │   ├── StatisticsCalculator.cs# ComputeStatistics: mean, median, stdev, percentiles
    │   │   └── OutlierDetector.cs     # DetectOutliers: IQR method, returns flagged values
    │   ├── Dockerfile                 # Multi-stage build (same pattern as S1)
    │   └── agent.yaml                 # azd agent definition
    │
    ├── DataCrunch.Web/                # ── Blazor Server Frontend ──
    │   ├── DataCrunch.Web.csproj      # net10.0, Blazor Server
    │   ├── Program.cs                 # Host setup, HttpClient DI, AgentService registration
    │   ├── appsettings.json           # AgentMode + AgentEndpoints config
    │   ├── appsettings.Development.json  # Local overrides
    │   ├── Components/
    │   │   ├── App.razor              # Root Blazor component
    │   │   ├── Routes.razor           # Router setup
    │   │   ├── Layout/
    │   │   │   ├── MainLayout.razor   # App shell: header, nav, footer
    │   │   │   └── NavMenu.razor      # Navigation links
    │   │   └── Pages/
    │   │       ├── Home.razor         # Welcome page, scenario description
    │   │       └── Analyze.razor      # CSV upload + results display (main page)
    │   ├── Services/
    │   │   └── AgentService.cs        # HttpClient wrapper for /responses calls
    │   └── wwwroot/
    │       └── css/
    │           └── app.css            # Custom styles (minimal)
    │
    ├── DataCrunch.AppHost/            # ── Aspire Orchestrator ──
    │   ├── DataCrunch.AppHost.csproj  # Aspire AppHost
    │   └── Program.cs                 # Wires agent + web, service discovery
    │
    └── DataCrunch.ServiceDefaults/    # ── Aspire Shared Defaults ──
        ├── DataCrunch.ServiceDefaults.csproj
        └── Extensions.cs             # OpenTelemetry, health checks, resilience
```

**File count**: ~25 files (vs ~8 for Scenario 1). The increase comes from Blazor's component structure and Aspire's two extra projects. Each file is small and focused.

### 4.7 Demo Flow

#### Pre-Demo Setup (15-20 minutes)

```powershell
git clone <repo>
cd foundry-hosted-agents-dotnet-demo/scenario-2-data-crunch
./setup.ps1                              # Provision Azure resources
az login --tenant <id>                   # Printed by setup.ps1
```

Have a sample CSV file ready (e.g., `sample-data/api-response-times.csv` — we should include 2-3 sample CSVs in the repo).

#### The Demo (15 minutes)

**Part 1 (0:00–3:00): "The Agent"**

> "Let me show you the Data Crunch Agent — a hosted agent that does real statistical analysis. Let's look at the code."

- Open `DataCrunchAgent/Program.cs` — same pattern as the intro scenario
- Open `Tools/StatisticsCalculator.cs` — "This is real C# math. The LLM can't compute a median — but this code can."
- Quick curl test to show it works standalone:
  ```powershell
  cd src/DataCrunchAgent && dotnet run &
  curl -X POST http://localhost:8088/responses -H "Content-Type: application/json" \
    -d '{"input":"Analyze: endpoint,ms\n/api/users,145\n/api/users,132\n/api/orders,1847"}'
  ```

**Part 2 (3:00–7:00): "The Aspire Application"**

> "Now let's see the full application. One command starts everything."

```powershell
dotnet run --project src/DataCrunch.AppHost
```

- Open the Aspire Dashboard (`http://localhost:15888`)
  - "Two services running: the agent and the web frontend. Both healthy."
  - "Aspire handles service discovery — the frontend finds the agent automatically."
- Open the Blazor app (`http://localhost:5000`)
  - Point out the mode indicator: "🟢 Local Agent"
  - Upload a CSV file (the sample `api-response-times.csv`)
  - Click "Analyze"
  - Walk through the results:
    - "Here's the parsed data — 9 rows, 3 columns"
    - "Statistics: mean 574ms, but median only 145ms — that's suspicious"
    - "Two outliers detected: 1847ms and 2103ms — both HTTP 500 errors"
  - "All the math was done by our C# tools. The model orchestrated the analysis and wrote the narrative."

**Part 3 (7:00–10:00): "Traces and Observability"**

- Switch to Aspire Dashboard → Traces tab
  - Show a trace from browser → Blazor → agent HTTP call → response
  - "Full distributed tracing, out of the box. You can see exactly how long each tool call took."
- Show Logs tab
  - "Structured logs from both services, in one place."

**Part 4 (10:00–13:00): "Deploy to Cloud" (optional)**

> "Let's switch to the cloud-deployed agent."

```powershell
./deploy.ps1
```

- After deploy, change `AgentMode` to `"Cloud"` in config (or flip the UI toggle)
- Re-upload the same CSV
- "Same results, but now the agent is running on Foundry Agent Service. The Blazor app is still local, talking to Azure."
- Show the Foundry playground as well

**Part 5 (13:00–15:00): "Wrap Up"**

> "You've seen: a hosted agent with real compute tools, a Blazor frontend, Aspire orchestration, and cloud deployment. All .NET, all open source SDKs, all running in minutes."

#### Post-Demo

```powershell
./cleanup.ps1
```

---

## 5. Architecture Decisions

### Carried Forward from Scenario 1

All original architecture decisions (AD-01 through AD-07) still apply. See [the original decisions in `.squad/decisions.md`](.squad/decisions.md) for details. Summary:

| ID | Decision | Still applies? |
|---|---|---|
| AD-01 | Use .csproj, not file-based .NET 10 | ✅ Yes — Docker needs project files |
| AD-02 | Local function tools, no MCP | ✅ Yes — zero external dependencies |
| AD-03 | azd starter template for infra | ✅ Yes — no Bicep maintenance |
| AD-04 | Two-script deployment model | ✅ Yes — setup.ps1 + deploy.ps1 per scenario |
| AD-05 | Environment variables for config | ✅ Yes — one pattern for local + deployed |
| AD-06 | AzureCliCredential for auth | ✅ Yes |
| AD-07 | Default model gpt-5-mini | ✅ Yes |

### New Decisions for Scenario 2

| ID | Decision | Rationale |
|---|---|---|
| SD-01 | Blazor Server (not WebAssembly) | Server-side rendering, no CORS, direct HttpClient, simpler for demo |
| SD-02 | Blazor calls agent directly (no API gateway) | Simplicity wins for a demo. No extra project needed. |
| SD-03 | CSV content in agent prompt | Small demo files. Full content fits in one request. |
| SD-04 | Aspire for orchestration | Service discovery, dashboard, traces — real value even for demos |
| SD-05 | Scenario-per-folder repo layout | Self-contained scenarios, matches Azure Samples patterns |
| SD-06 | No shared libraries | Scenarios are independent. Copy > coupling for a demo repo. |

---

## 6. Prerequisites

### Scenario 1 (same as before)

| Tool | Version | Install |
|---|---|---|
| .NET 10 SDK | Latest | https://dotnet.microsoft.com/download |
| Azure Developer CLI (azd) | ≥ 1.23.0 | `winget install Microsoft.Azd` |
| Azure CLI (az) | ≥ 2.80 | https://learn.microsoft.com/cli/azure/install-azure-cli |
| Docker Desktop | Latest | https://docs.docker.com/get-docker/ |
| Azure subscription | Contributor access | https://azure.microsoft.com/free/ |

### Scenario 2 (adds Aspire)

Everything from Scenario 1, plus:

| Tool | Version | Install |
|---|---|---|
| .NET Aspire workload | Latest | `dotnet workload install aspire` |

The Aspire workload includes the AppHost tooling, dashboard, and service defaults templates.

---

## 7. Work Breakdown

### Phase 1-3: Scenario 1 (DONE ✅)

Phases 1–3 are complete. Kaylee (K-1 through K-6), Wash (W-1 through W-4), and Zoe validation are done. The timezone agent works end-to-end.

**Remaining Scenario 1 work**: Move files to `scenario-1-intro/` folder structure (see [Phase 4](#phase-4-repo-restructure-day-3)).

---

### Scenario 2 Work Breakdown

#### Kaylee (Agent + Backend)

| Task | Description | Depends On | Est. |
|---|---|---|---|
| K2-1: DataCrunchAgent scaffold | Create `DataCrunchAgent.csproj`, copy hosting adapter pattern from S1. Add Aspire ServiceDefaults reference. | Repo restructure | 30m |
| K2-2: ParseData tool | Implement CSV/JSON parsing in `Tools/DataParser.cs`. Split lines, detect headers, count rows/columns, return structured summary. | K2-1 | 1h |
| K2-3: ComputeStatistics tool | Implement statistics in `Tools/StatisticsCalculator.cs`. Pure C# math: count, min, max, mean, median, stdev, P25, P75. | K2-1 | 1.5h |
| K2-4: DetectOutliers tool | Implement IQR outlier detection in `Tools/OutlierDetector.cs`. Calculate Q1, Q3, fences, flag outliers. | K2-1 | 1h |
| K2-5: Agent Program.cs | Wire tools + instructions + RunAIAgentAsync. Verify agent responds to CSV input via curl. | K2-2, K2-3, K2-4 | 1h |
| K2-6: Dockerfile + agent.yaml | Same pattern as S1 with updated names. | K2-5 | 30m |
| K2-7: AgentService | Create `DataCrunch.Web/Services/AgentService.cs` — HttpClient wrapper that posts CSV to agent and parses response. | K2-5 | 1h |
| K2-8: Blazor integration | Wire `AgentService` into `Analyze.razor` — file upload triggers analysis, response renders as table + stats + outliers. | K2-7, F-3 | 2h |

#### Wash (DevOps + Aspire)

| Task | Description | Depends On | Est. |
|---|---|---|---|
| W2-1: Aspire AppHost | Create `DataCrunch.AppHost` project. Wire agent + web references. Verify `dotnet run` starts both services. | K2-1 | 1h |
| W2-2: ServiceDefaults | Create `DataCrunch.ServiceDefaults` with OpenTelemetry, health checks, resilience. Standard Aspire template. | — | 30m |
| W2-3: Local/Cloud config | Implement `AgentMode` toggle in `appsettings.json`. Wire into `Program.cs` DI. Document toggle options. | W2-1, K2-7 | 1h |
| W2-4: setup.ps1 (S2) | Adapt S1 setup script for S2 paths. Register DataCrunchAgent via azd. | W2-1, K2-6 | 1h |
| W2-5: deploy.ps1 (S2) | Adapt S1 deploy script. Deploys only the agent (Blazor can stay local or deploy separately). | W2-4 | 1h |
| W2-6: cleanup.ps1 (S2) | Adapt S1 cleanup script for S2. | — | 15m |
| W2-7: Solution file | Create `foundry-hosted-agents-dotnet-demo.slnx` at repo root with all projects from both scenarios. | All projects exist | 15m |

#### Inara (Frontend) — NEW TEAM MEMBER SUGGESTED

We need a frontend developer for the Blazor UI. Kaylee can build the `AgentService` and basic wiring, but the `Analyze.razor` page — with file upload, responsive tables, stat cards, outlier highlighting, loading states — deserves focused frontend attention.

**Suggested role**: Inara — Frontend Dev (Blazor)

| Task | Description | Depends On | Est. |
|---|---|---|---|
| F-1: Blazor project scaffold | Create `DataCrunch.Web` from Blazor Server template. Add ServiceDefaults. Configure nav. | W2-2 | 1h |
| F-2: Home.razor | Landing page with scenario description, mode indicator, nav to Analyze. | F-1 | 30m |
| F-3: Analyze.razor — Upload | File upload area using `<InputFile>`, file preview (name, size), "Analyze" button, loading state. | F-1 | 1.5h |
| F-4: Analyze.razor — Results | Render agent response: data table, statistics cards (Bootstrap), outlier badges, agent narrative. | F-3, K2-8 | 2.5h |
| F-5: Mode indicator | Show Local/Cloud badge in nav. Read from config. (Optional: runtime toggle.) | F-1, W2-3 | 1h |
| F-6: Sample CSV files | Create 3 sample CSVs in `scenario-2-data-crunch/sample-data/` (api-response-times, sales-quarterly, sensor-readings). Each must have outliers for demo impact. See §9.5 for specs. | — | 30m |
| F-7: S2 README | Write Scenario 2 README with prerequisites, quickstart, demo script, architecture diagram. Include `<!-- demo gif goes here -->` placeholder. | All S2 tasks | 1h |

#### Wash (DevOps) — Additional Tasks

| Task | Description | Depends On | Est. |
|---|---|---|---|
| W2-8: Dev container | Create `.devcontainer/devcontainer.json` + `post-create.sh` per §9.3 spec. Test in Codespaces or local dev container. | W2-7 (.sln exists) | 1h |
| W2-9: global.json | Create `global.json` at repo root pinning .NET 10 SDK version. | — | 5m |

#### Demo GIFs (Post-Implementation)

| Task | Description | Depends On | Est. |
|---|---|---|---|
| GIF-1: Scenario 1 GIF | Record `dotnet run` → curl → response. 15s, 800×450, save to `docs/images/scenario-1-demo.gif` | S1 final | 30m |
| GIF-2: Scenario 2 GIF | Record CSV upload → results in Blazor. 20s, 800×450, save to `docs/images/scenario-2-demo.gif` | S2 final | 30m |
| GIF-3: Hero GIF | Record Aspire dashboard + Blazor side-by-side. 25s, 800×450, save to `docs/images/hero-demo.gif` | S2 final | 30m |

#### Zoe (Tests)

| Task | Description | Depends On | Est. |
|---|---|---|---|
| Z2-1: Agent tool tests | Test each tool function directly (unit-level): ParseData, ComputeStatistics, DetectOutliers with known inputs. | K2-2, K2-3, K2-4 | 1.5h |
| Z2-2: Agent integration test | Send CSV via curl → verify agent response has stats + outliers. | K2-5 | 1h |
| Z2-3: Aspire smoke test | `dotnet run --project AppHost` → verify both services start → hit Blazor → hit dashboard. | W2-1, F-1 | 1h |
| Z2-4: E2E demo walkthrough | Follow S2 README: start AppHost → upload CSV → verify results → switch to Cloud → verify. | F-7 | 1.5h |
| Z2-5: Repo-level validation | Verify both scenarios work independently. `dotnet build` from solution root. Both READMEs accurate. | W2-7 | 1h |

---

## 8. Implementation Order

### Phase 4: Repo Restructure (Day 3)

Move existing Scenario 1 files to `scenario-1-intro/`. Create top-level README pointing to both scenarios. Create solution file.

```
1. Create scenario-1-intro/ folder structure
2. Move src/HostedAgent/ → scenario-1-intro/src/HostedAgent/
3. Move setup.ps1, deploy.ps1, cleanup.ps1, test.http → scenario-1-intro/
4. Rewrite root README.md as scenario picker
5. Create foundry-hosted-agents-dotnet-demo.slnx
```

**Gate**: `dotnet build` works from solution root. Scenario 1 still runs from its new location.

### Phase 5: Data Crunch Agent (Day 3-4)

Build the agent independently — it should work standalone before adding Aspire or Blazor.

```
K2-1 ──▶ K2-2 ──┐
          K2-3 ──┼──▶ K2-5 ──▶ K2-6
          K2-4 ──┘
```

1. **K2-1**: Project scaffold (copy pattern from S1)
2. **K2-2, K2-3, K2-4**: Three tools (can be parallel)
3. **K2-5**: Wire tools into agent, verify via curl
4. **K2-6**: Dockerfile + agent.yaml

**Gate**: Agent runs standalone on port 8088, responds to CSV input, returns correct statistics.

### Phase 6: Aspire + Blazor (Day 4-5)

Stand up the Aspire orchestration and Blazor frontend.

```
W2-2 ──▶ F-1 ──▶ F-2
                   F-3 ──▶ F-4
W2-1 ──▶ W2-3
          K2-7 ──▶ K2-8
                    F-5
```

1. **W2-2**: ServiceDefaults project
2. **W2-1**: AppHost (wires agent + web)
3. **F-1**: Blazor project scaffold
4. **F-2, F-3**: Home page + upload page (can be parallel with W2-3)
5. **K2-7**: AgentService (backend integration)
6. **W2-3**: Local/Cloud config toggle
7. **K2-8 + F-4**: Full integration — upload → agent → render results
8. **F-5**: Mode indicator in UI

**Gate**: `dotnet run --project DataCrunch.AppHost` starts both services. Upload CSV in browser → see results. Aspire dashboard shows traces.

### Phase 7: Deploy + Polish (Day 5-6)

```
W2-4 ──▶ W2-5
W2-6
F-6
W2-7 ──▶ W2-8
W2-9
F-7
```

1. **W2-4, W2-5, W2-6**: Deploy scripts for S2
2. **F-6**: Sample CSV files (3 CSVs with designed outliers)
3. **W2-7**: Solution file (all projects)
4. **W2-8**: Dev container setup (`.devcontainer/devcontainer.json` + `post-create.sh`)
5. **W2-9**: `global.json` (pin .NET SDK version)
6. **F-7**: Scenario 2 README (with GIF placeholder)

**Gate**: Full demo works end-to-end. Cloud mode works. README is followable. Dev container builds and restores successfully.

### Phase 8: Validation + Demo GIFs (Day 6-7)

```
Z2-1 ──▶ Z2-2 ──▶ Z2-3 ──▶ Z2-4
                              Z2-5
                              GIF-1, GIF-2, GIF-3
```

1. **Z2-1 → Z2-5**: Full validation sweep
2. **GIF-1**: Record Scenario 1 demo (15s)
3. **GIF-2**: Record Scenario 2 demo — CSV upload + results (20s)
4. **GIF-3**: Record hero GIF — Aspire + Blazor side-by-side (25s)
5. Embed GIFs into READMEs (replace `<!-- demo gif goes here -->` placeholders)

**Gate**: A new person can clone the repo, pick a scenario, and complete the demo by following the README.

### Full Dependency Graph

```
Phase 4 (Restructure)
    │
    ▼
Phase 5 (Agent)
    K2-1 ─▶ K2-2 ─┐
              K2-3 ─┼─▶ K2-5 ─▶ K2-6
              K2-4 ─┘          │
    │                          │
    ▼                          ▼
Phase 6 (Aspire + Blazor)
    W2-2 ─▶ F-1 ─▶ F-2
                    F-3 ─┐
    W2-1 ─▶ W2-3        │
             K2-7 ─┐     │
                   ├─▶ K2-8 / F-4
                   │     F-5
    │              │
    ▼              ▼
Phase 7 (Deploy + Polish + DevContainer)
    W2-4, W2-5, W2-6
    F-6 (sample CSVs)
    W2-7 ─▶ W2-8 (dev container)
    W2-9 (global.json)
    F-7 (README + GIF placeholder)
    │
    ▼
Phase 8 (Validation + Demo GIFs)
    Z2-1 ─▶ Z2-2 ─▶ Z2-3 ─▶ Z2-4, Z2-5
    GIF-1, GIF-2, GIF-3 ─▶ Embed in READMEs
```

---

## 9. Repo Improvement Suggestions

### 9.1 README Structure for Multi-Scenario Repos

The root `README.md` should become a **scenario picker**, not a full guide:

```markdown
# Foundry Hosted Agents Demo (.NET)

Two scenarios showing how to build hosted agents with Microsoft Foundry.

| Scenario | What you'll learn | Time |
|---|---|---|
| [1. Intro — Time Zone Agent](scenario-1-intro/) | Create and run your first hosted agent | 5 min |
| [2. Data Crunch — Full Application](scenario-2-data-crunch/) | Build an end-to-end app with Aspire + Blazor | 15 min |

## Prerequisites (shared)
...

## What are Hosted Agents?
(Brief explainer, then point to scenario READMEs for details)
```

Each scenario README is fully self-contained. A developer should be able to read only one README and complete that scenario.

### 9.2 GitHub Actions CI

Add `.github/workflows/ci.yml`:

```yaml
name: CI
on: [push, pull_request]
jobs:
  build:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'
      - run: dotnet workload install aspire
      - run: dotnet build foundry-hosted-agents-dotnet-demo.slnx
      - run: dotnet test foundry-hosted-agents-dotnet-demo.slnx --no-build
```

Catches build breaks across both scenarios. Doesn't need Azure credentials — just verifies the code compiles and tools work.

### 9.3 Dev Container / Codespaces Support ✅ PLANNED

Full dev container setup for one-click Codespaces and local VS Code Dev Container support.

**Files to create:**

#### `.devcontainer/devcontainer.json`

```json
{
  "name": "Foundry Hosted Agents Demo",
  "image": "mcr.microsoft.com/devcontainers/dotnet:10.0",
  "features": {
    "ghcr.io/devcontainers/features/azure-cli:1": {},
    "ghcr.io/devcontainers/features/docker-in-docker:2": {},
    "ghcr.io/azure/azure-dev/azd:0": {}
  },
  "postCreateCommand": "bash .devcontainer/post-create.sh",
  "forwardPorts": [5000, 8088, 15888],
  "portsAttributes": {
    "5000": { "label": "Blazor Frontend", "onAutoForward": "notify" },
    "8088": { "label": "Hosted Agent", "onAutoForward": "notify" },
    "15888": { "label": "Aspire Dashboard", "onAutoForward": "notify" }
  },
  "customizations": {
    "vscode": {
      "extensions": [
        "ms-dotnettools.csharp",
        "ms-dotnettools.csdevkit",
        "ms-azuretools.azure-dev",
        "humao.rest-client"
      ],
      "settings": {
        "dotnet.defaultSolution": "foundry-hosted-agents-dotnet-demo.slnx"
      }
    }
  }
}
```

#### `.devcontainer/post-create.sh`

```bash
#!/bin/bash
set -e

echo "📦 Installing .NET Aspire workload..."
dotnet workload install aspire

echo "📦 Restoring NuGet packages..."
dotnet restore foundry-hosted-agents-dotnet-demo.slnx

echo "✅ Dev container ready!"
echo ""
echo "Quick start:"
echo "  Scenario 1: cd scenario-1-intro/src/HostedAgent && dotnet run"
echo "  Scenario 2: cd scenario-2-data-crunch && dotnet run --project src/DataCrunch.AppHost"
```

**Why this matters:**
- **Zero-install experience** — open in Codespaces, wait 2 minutes, run the demo
- **Consistent environment** — eliminates ".NET 10 not installed" / "wrong SDK version" issues
- **Port forwarding** — Blazor, agent, and Aspire dashboard auto-forward with labels
- **VS Code extensions** — C# Dev Kit, Azure Dev CLI, REST Client pre-installed
- **Post-create script** — installs Aspire workload + restores packages on first open

### 9.4 Solution-Level .sln File

`foundry-hosted-agents-dotnet-demo.slnx` at the repo root including all projects:

- `scenario-1-intro/src/HostedAgent/HostedAgent.csproj`
- `scenario-2-data-crunch/src/DataCrunchAgent/DataCrunchAgent.csproj`
- `scenario-2-data-crunch/src/DataCrunch.Web/DataCrunch.Web.csproj`
- `scenario-2-data-crunch/src/DataCrunch.AppHost/DataCrunch.AppHost.csproj`
- `scenario-2-data-crunch/src/DataCrunch.ServiceDefaults/DataCrunch.ServiceDefaults.csproj`

Enables `dotnet build` from root, IDE solution explorer shows everything, CI builds everything in one pass.

### 9.5 Sample Data Files ✅ PLANNED

Include 3 sample CSVs in `scenario-2-data-crunch/sample-data/`. These are critical — they're what the audience sees during the demo. Each file is designed to trigger a different "aha!" moment.

| File | Description | Rows | Columns | Why it's good for demo |
|---|---|---|---|---|
| `api-response-times.csv` | API endpoint response times with HTTP status codes | ~20 | `endpoint`, `response_ms`, `status_code`, `timestamp` | Outliers (1800ms+) correlate with 500 errors. The agent connects the dots: "slow responses are failures." Visual storytelling. |
| `sales-quarterly.csv` | Quarterly sales by region and product | ~24 | `quarter`, `region`, `product`, `revenue`, `units_sold` | Clean multi-column data. Shows stats across multiple numeric columns. Good for "which region is underperforming?" narrative. |
| `sensor-readings.csv` | IoT temperature sensor readings over 24 hours | ~30 | `sensor_id`, `timestamp`, `temperature_c`, `humidity_pct` | Time-series feel. 2-3 obvious temperature spikes (anomalies). Agent says "Sensor B spiked at 14:00 — possible malfunction." |

**Design guidelines for sample data:**
- Every file must have at least one outlier so `DetectOutliers` always has something to flag
- Keep files under 30 rows — they need to fit in the agent's prompt and be readable on screen
- Use realistic column names and values — this is a demo, not a test fixture
- Include a mix of string and numeric columns to show `ParseData` handling both
- Save as UTF-8, comma-delimited, with headers in row 1

### 9.6 No Shared Libraries (Deliberate)

I'm explicitly recommending **no shared library** project. The temptation is to extract common hosting adapter code into a shared project. Don't.

Reasons:
1. Each scenario should be fully self-contained — you can delete one without breaking the other
2. The "shared" code is just the hosting adapter NuGet package reference — it's already shared
3. Coupling scenarios creates maintenance burden for a demo repo
4. If someone wants to fork just one scenario, they can copy the folder

### 9.7 Animated GIF in README ✅ PLANNED

Record a demo GIF and embed it in the root README. This is the single most effective way to sell a demo repo — visitors see it working before reading a single word.

**What to record:**

| GIF | Location | Content | Duration |
|---|---|---|---|
| `docs/images/scenario-1-demo.gif` | `scenario-1-intro/README.md` | Terminal: `dotnet run` → curl request → agent responds with timezone | ~15s |
| `docs/images/scenario-2-demo.gif` | `scenario-2-data-crunch/README.md` | Browser: upload CSV → loading → results with stats + outliers | ~20s |
| `docs/images/hero-demo.gif` | Root `README.md` | Combined: Scenario 2 with Aspire dashboard visible alongside Blazor app | ~25s |

**Recording guidelines:**
- Use a tool like [ScreenToGif](https://www.screentogif.com/) (Windows), [Kap](https://getkap.co/) (Mac), or [Peek](https://github.com/phw/peek) (Linux)
- **Resolution**: 800×450px (16:9, fits GitHub README width without scaling)
- **Frame rate**: 10-15 fps (keeps file size under 5MB)
- **Font size**: 16px+ in terminal, 14px+ in browser — must be readable at README width
- **No mouse cursor** in terminal recordings (cleaner). Show cursor in browser recordings (shows interaction).
- **Loop**: GIF should loop seamlessly. End on the results screen, not mid-transition.

**README embedding:**

```markdown
## See it in Action

![Data Crunch Agent Demo](docs/images/scenario-2-demo.gif)

*Upload a CSV → get statistics, outlier detection, and a narrative — powered by a hosted agent running real C# code.*
```

**Note:** GIFs are recorded after implementation is complete. This is a Phase 8 (polish) task. Placeholder `<!-- demo gif goes here -->` can be added to READMEs during Phase 7.

### 9.8 Additional Polish Ideas

- **`CONTRIBUTING.md`** — For community contributions (if this goes public)
- **`.editorconfig`** — Consistent code style across scenarios
- **`global.json`** — Pin .NET SDK version to avoid "works on my machine" issues
- **Sample test.http for Scenario 2** — REST Client requests for the agent

---

## 10. Reference Links

| Resource | URL |
|---|---|
| Hosted Agents Concepts | https://learn.microsoft.com/en-us/azure/foundry/agents/concepts/hosted-agents |
| Hosted Agent Quickstart | https://learn.microsoft.com/en-us/azure/foundry/agents/quickstarts/quickstart-hosted-agent |
| Deploy Hosted Agent | https://learn.microsoft.com/en-us/azure/foundry/agents/how-to/deploy-hosted-agent |
| Agent Framework C# (Foundry) | https://learn.microsoft.com/en-us/agent-framework/agents/providers/azure-ai-foundry |
| Agent Skills .NET Demo | https://github.com/Azure-Samples/agent-skills-dotnet-demo |
| Foundry C# Samples | https://github.com/microsoft-foundry/foundry-samples/tree/main/samples/csharp/hosted-agents |
| azd AI Agent Extension | https://aka.ms/azdaiagent/docs |
| azd Starter Template | https://github.com/Azure-Samples/azd-ai-starter-basic |
| .NET Aspire Overview | https://learn.microsoft.com/dotnet/aspire/get-started/aspire-overview |
| .NET Aspire Service Discovery | https://learn.microsoft.com/dotnet/aspire/fundamentals/service-discovery |
| Blazor Server Overview | https://learn.microsoft.com/aspnet/core/blazor/ |

---

## 11. Key Risks & Mitigations

### Carried Forward

| Risk | Impact | Mitigation |
|---|---|---|
| NuGet packages are prerelease/beta | API may change | Pin exact versions, verify at implementation time |
| azd ai agent extension is preview | Commands may change | Document exact azd version, test before demo |
| Docker build on ARM64 (Apple Silicon) | Container won't run on Azure | Use `--platform linux/amd64`, or azd cloud build |
| Model quota limits | Provisioning may fail | Document fallback models |
| Azure region availability | Not all regions support hosted agents | Default to East US |

### New for Scenario 2

| Risk | Impact | Mitigation |
|---|---|---|
| Aspire workload not installed | AppHost won't build | setup.ps1 checks for Aspire workload, prompts install |
| Large CSV files exceed prompt limits | Agent truncates or errors | Include only small sample CSVs (< 50 rows). Document limit in README. |
| Aspire port conflicts (15888, 5000) | Dashboard or web won't start | Document required ports, show how to change in launchSettings.json |
| Blazor Server requires persistent connection | May drop if network flaky | Acceptable for demo. WebSocket reconnect is built into Blazor Server. |
| Cloud mode requires deployed agent | Can't demo cloud without prior deploy | Deploy scripts must be run first. README makes this clear. |
| Agent response format varies | Blazor can't parse structured data reliably | AgentService parses the natural language response. Use structured instructions to guide agent output format. Consider asking the agent to return JSON in a code block. |

---

## Open Questions

1. **Exact NuGet package versions for .NET 10**: The foundry-samples target net9.0. We need to verify that `Azure.AI.AgentServer.AgentFramework` supports net10.0, or determine the right version. Kaylee to verify during K-1.

2. **Function tool registration pattern in C#**: The Python sample uses `@ai_function` decorator. The C# equivalent needs to be confirmed — likely `AIFunctionFactory.Create()` from `Microsoft.Extensions.AI` or tools parameter on `ChatClientAgent`. Kaylee to verify during K-2.

3. **azd init interaction with existing repo**: Running `azd init -t <template>` in a repo that already has files — need to verify it merges cleanly and doesn't overwrite our code. Wash to verify during W-1.

---

*Plan authored by Mal. Last updated: March 2026.*
