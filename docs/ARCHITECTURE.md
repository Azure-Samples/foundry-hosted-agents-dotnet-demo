# Architecture

> Technical internals of the Microsoft Foundry Hosted Agents .NET demo.
> The root [README](../README.md) explains *what* the scenarios do — this document explains *how* the pieces connect.

---

## System Overview

```
┌─────────────────────────────────────────────────────────────────────┐
│                        Microsoft Foundry                            │
│                                                                     │
│  ┌──────────┐    Responses    ┌──────────────────────────────────┐  │
│  │  Client   │───  API  ─────>│  Hosted Agent Container          │  │
│  │ (browser, │    /responses  │                                  │  │
│  │  curl,    │<───────────────│  ┌────────────┐  ┌────────────┐  │  │
│  │  test.http│                │  │  Hosting   │  │  Function   │  │  │
│  └──────────┘                │  │  Adapter    │  │  Tools      │  │  │
│                               │  │ (port 8088)│  │  (C# code)  │  │  │
│                               │  └─────┬──────┘  └──────┬─────┘  │  │
│                               │        │                 │        │  │
│                               │        ▼                 │        │  │
│                               │  ┌────────────┐         │        │  │
│                               │  │ ChatClient │◄────────┘        │  │
│                               │  │ Agent      │                  │  │
│                               │  └─────┬──────┘                  │  │
│                               └────────┼─────────────────────────┘  │
│                                        │                            │
│                                        ▼                            │
│                               ┌────────────────┐                   │
│                               │ Azure OpenAI   │                   │
│                               │ (gpt-5-mini)   │                   │
│                               └────────────────┘                   │
└─────────────────────────────────────────────────────────────────────┘
```

Every scenario follows the same pattern: a `ChatClientAgent` wraps one or more function tools, and the hosting adapter exposes it as an HTTP endpoint that speaks the Foundry Responses protocol. The model orchestrates — your C# code executes.

---

## 1. Hosting Adapter Protocol

The hosting adapter is the bridge between Microsoft Foundry and the Microsoft Agent Framework. One line of code activates the entire runtime:

```csharp
// scenario-1-intro/src/time-zone-agent/Program.cs, line 89
await agent.RunAIAgentAsync(telemetrySourceName: "Agents");
```

### What `RunAIAgentAsync()` does under the hood

| Step | What Happens |
|------|-------------|
| 1. Start HTTP server | Binds to `http://localhost:8088` — the port Foundry expects for hosted agent containers |
| 2. Register `/responses` endpoint | Accepts POST requests in the Foundry Responses API format |
| 3. Protocol translation | Converts inbound Responses API JSON → `IChatClient` calls on the Agent Framework side, then converts the framework's response back to Responses API JSON |
| 4. OpenTelemetry integration | The `telemetrySourceName` parameter registers a trace source. The adapter emits traces, metrics, and structured logs automatically |
| 5. Block the process | Keeps the application alive, serving requests until shutdown |

### Protocol translation flow

```
Client Request (Responses API)
        │
        ▼
┌─────────────────────┐
│  Hosting Adapter     │
│  (port 8088)         │
│                      │
│  1. Parse JSON input │
│  2. Create chat turn │
│  3. Send to agent    │──────► ChatClientAgent
│  4. Agent calls LLM  │           │
│  5. LLM may call     │◄──────────┘
│     function tools    │
│  6. Tools execute     │──────► GetCurrentDateTime()
│  7. Tool result back  │◄──────────┘
│     to LLM            │
│  8. LLM produces      │
│     final answer      │
│  9. Convert to        │
│     Responses format  │
└───────┬──────────────┘
        │
        ▼
Client Response (Responses API JSON)
```

The adapter handles the multi-turn tool-calling loop internally. If the model decides to call two tools in sequence, the adapter executes both and feeds results back before producing the final response. The caller sees a single request/response cycle.

### Key package

All three scenarios depend on the same adapter package:

```xml
<!-- Version used across all scenarios -->
<PackageReference Include="Azure.AI.AgentServer.AgentFramework" Version="1.0.0-beta.10" />
```

This package provides the `RunAIAgentAsync()` extension method via `Azure.AI.AgentServer.AgentFramework.Extensions`.

---

## 2. Responses API Endpoint Schema

### Request format

Every agent exposes a single endpoint: `POST /responses`

```json
{
  "input": "What time is it in Tokyo?",
  "model": "TimeZoneAgent"
}
```

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `input` | string | Yes | The user's message or prompt |
| `model` | string | No | Agent name — used for routing when multiple agents share an endpoint |

The `model` field maps to the `name` parameter in the `ChatClientAgent` constructor (e.g., `"TimeZoneAgent"`, `"DataCrunchAgent"`, `"ImageGeneratorAgent"`).

### Response format

The adapter returns JSON following the Responses API structure. The C# models are defined in `scenario-2-data-crunch/src/DataCrunch.Web/Services/AgentService.cs` (lines 63–79):

```json
{
  "output": [
    {
      "type": "message",
      "role": "assistant",
      "content": [
        {
          "type": "output_text",
          "text": "The current time in Tokyo is Monday, July 21, 2025 at 02:15 AM +09:00"
        }
      ]
    }
  ]
}
```

### Response model classes

```csharp
// scenario-2-data-crunch/src/DataCrunch.Web/Services/AgentService.cs, lines 63-79
public class ResponsesApiResponse
{
    public List<ResponseOutput>? Output { get; set; }
}

public class ResponseOutput
{
    public string Type { get; set; } = "";     // "message"
    public string? Role { get; set; }          // "assistant"
    public List<ResponseContent>? Content { get; set; }
}

public class ResponseContent
{
    public string Type { get; set; } = "";     // "output_text"
    public string? Text { get; set; }          // The actual response text
}
```

### Extracting the text from a response

The Blazor frontend in Scenario 2 demonstrates the parsing pattern (`AgentService.cs`, lines 39–44):

```csharp
var outputText = agentResponse?.Output?
    .Where(o => o.Type == "message" && o.Role == "assistant")
    .SelectMany(o => o.Content ?? [])
    .Where(c => c.Type == "output_text")
    .Select(c => c.Text)
    .FirstOrDefault();
```

Filter for `type == "message"` and `role == "assistant"`, then drill into content items of `type == "output_text"`. The `output` array may contain other entry types (e.g., tool call traces), so filtering is required.

---

## 3. Credential Switching (Local vs Container)

Every scenario uses an identical dual-credential pattern — no API keys, no secrets anywhere.

### The pattern

```csharp
// Identical in all three scenarios (e.g., scenario-1-intro/src/time-zone-agent/Program.cs, lines 37-47)
TokenCredential credential;
try
{
    credential = Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true"
        ? new DefaultAzureCredential()    // Container: managed identity
        : new AzureCliCredential();       // Local: az login token
}
catch (AuthenticationFailedException ex)
{
    logger.LogCritical(ex, "Authentication failed. Run: az login --tenant <your-tenant-id>");
    throw;
}
```

### How it works

```
┌─────────────────────────────────────────────────────────┐
│            Credential Decision at Startup                │
│                                                         │
│  DOTNET_RUNNING_IN_CONTAINER == "true" ?                │
│        │                         │                      │
│       YES                        NO                     │
│        │                         │                      │
│        ▼                         ▼                      │
│  ┌──────────────────┐  ┌────────────────────┐          │
│  │ DefaultAzure     │  │ AzureCliCredential │          │
│  │ Credential       │  │                    │          │
│  │                  │  │ Uses token from    │          │
│  │ Tries managed    │  │ az login --tenant  │          │
│  │ identity first,  │  │                    │          │
│  │ then other       │  │ Respects the       │          │
│  │ Azure-aware      │  │ specific tenant    │          │
│  │ providers        │  │ you logged into    │          │
│  └──────────────────┘  └────────────────────┘          │
└─────────────────────────────────────────────────────────┘
```

| Environment | Variable | Credential | Token source |
|-------------|----------|-----------|-------------|
| `dotnet run` (local) | Not set | `AzureCliCredential` | Your `az login --tenant <id>` session |
| Docker / Foundry | `DOTNET_RUNNING_IN_CONTAINER=true` | `DefaultAzureCredential` | Managed identity assigned to the container |

### Why `DOTNET_RUNNING_IN_CONTAINER`?

This isn't a custom variable — it's set automatically by the official [.NET Docker images](https://github.com/dotnet/dotnet-docker). Every `mcr.microsoft.com/dotnet/aspnet:*` image sets it. The code doesn't need to ship or manage any environment flags — the detection is free.

### Why this matters

1. **No secrets in containers** — containers carry zero credentials; they inherit identity from the platform
2. **No API keys anywhere** — not in code, not in config, not in environment variables
3. **Tenant isolation** — `AzureCliCredential` respects the specific tenant from `az login --tenant`, preventing accidental cross-tenant access during local development
4. **One code path** — the `TokenCredential` abstraction means the rest of the code is identical regardless of environment

### Configuration without secrets

Local configuration uses .NET User Secrets (set by `setup.ps1`). Container configuration uses environment variables injected by the Foundry platform. Both contain only *endpoints* (URLs), never keys:

```
AZURE_OPENAI_ENDPOINT=https://<account>.openai.azure.com/
AZURE_OPENAI_DEPLOYMENT_NAME=gpt-5-mini
```

---

## 4. Function Tools Architecture

Function tools are the core value proposition of hosted agents. The model orchestrates, but your C# methods execute real server-side computation.

### How `AIFunctionFactory.Create()` works

The factory uses reflection to wrap a static (or instance) C# method into an `AIFunction` that the Agent Framework can expose to the model:

```csharp
// scenario-1-intro/src/time-zone-agent/Program.cs, line 79
tools: [AIFunctionFactory.Create(GetCurrentDateTime)]
```

What the factory does:
1. **Reads the method signature** — parameter names, types, and `[Description]` attributes
2. **Generates a JSON schema** — the model receives a function definition with typed parameters
3. **Creates a callable wrapper** — when the model emits a tool call, the framework deserializes arguments and invokes the C# method
4. **Returns the result** — the method's return value is serialized back to the model as a tool response

### The `[Description]` attribute pattern

Every tool and parameter uses `System.ComponentModel.Description` to tell the model what things do:

```csharp
// scenario-1-intro/src/time-zone-agent/Program.cs, lines 55-58
[Description("Gets the current date and time for a given IANA timezone")]
static string GetCurrentDateTime(
    [Description("IANA timezone identifier (e.g. America/New_York, Asia/Tokyo)")] string ianaTimezone)
```

These descriptions become the `description` fields in the JSON schema sent to the model. The quality of these descriptions directly affects how well the model decides when and how to call each tool. Good descriptions include:
- What the tool does
- Expected input format and examples
- What it returns

### How the model decides when to call tools

The `ChatClientAgent` sends the model:
1. System instructions (the `instructions` parameter)
2. The user's input message
3. A list of available tool schemas

The model then either responds directly or emits a `tool_call` with a function name and arguments. The Agent Framework:
1. Deserializes the arguments from JSON
2. Invokes the matching C# method
3. Sends the return value back to the model
4. The model incorporates the result and may call more tools or produce a final answer

This loop repeats until the model produces a text response with no tool calls.

### Tool progression across scenarios

```
Scenario 1 — Beginner          Scenario 2 — Intermediate       Scenario 3 — Advanced
(1 tool)                       (3 tools)                       (3 tools + GPU)
                                                               
┌─────────────────────┐       ┌─────────────────────┐        ┌─────────────────────┐
│ GetCurrentDateTime  │       │ ParseData           │        │ GenerateImage       │
│                     │       │ ComputeStatistics   │        │   (Stable Diffusion)│
│ TimeZoneInfo API    │       │ DetectOutliers      │        │ GenerateImageFlux   │
│ Pure computation    │       │                     │        │   (FLUX.2 cloud)    │
│ No I/O              │       │ String parsing +    │        │ ListModels          │
└─────────────────────┘       │ math (IQR, σ, etc.) │        │                     │
                              └─────────────────────┘        │ GPU inference +     │
                                                              │ cloud API calls     │
                                                              └─────────────────────┘
```

| Scenario | Tools | Complexity | What's different |
|----------|-------|-----------|-----------------|
| 1 | `GetCurrentDateTime` | Single static method | Minimal — proves the pattern works |
| 2 | `ParseData`, `ComputeStatistics`, `DetectOutliers` | Three tools in separate files (`Tools/` directory) | Multi-tool orchestration — model chains tools in sequence (parse → statistics → outliers) |
| 3 | `GenerateImage`, `GenerateImageFlux`, `ListModels` | Instance + static methods, async, GPU | External library (ElBruno.Text2Image), cloud API calls, GPU workload profile |

### Tool implementation detail: Scenario 2

Scenario 2's tools live in dedicated files under `scenario-2-data-crunch/src/DataCrunchAgent/Tools/`:

- **`DataParser.cs`** — Splits CSV by newlines and commas, returns a structured summary with column names, row count, and preview
- **`StatisticsCalculator.cs`** — Computes count, min, max, mean, median, standard deviation, P25, P75 for numeric columns
- **`OutlierDetector.cs`** — Uses the IQR method (1.5× interquartile range) to flag outliers with their row index and direction

The `OutlierDetector` internally calls `StatisticsCalculator.Percentile()` — tools can share code. The agent's instructions tell the model to chain these tools: parse first, then statistics, then outlier detection.

---

## 5. .NET Aspire Service Discovery (Scenario 2)

Scenario 2 adds a Blazor Server frontend that communicates with the agent. .NET Aspire orchestrates both services and handles service discovery.

### AppHost configuration

```csharp
// scenario-2-data-crunch/src/DataCrunch.AppHost/Program.cs (complete file)
var builder = DistributedApplication.CreateBuilder(args);

var agent = builder.AddProject<Projects.DataCrunchAgent>("datacrunchagent");

builder.AddProject<Projects.DataCrunch_Web>("datacrunchweb")
    .WithReference(agent)              // Injects service discovery config
    .WithExternalHttpEndpoints();      // Exposes the web frontend externally

builder.Build().Run();
```

### How service discovery connects the pieces

```
┌────────────────────────────────────────────────────────────┐
│                    .NET Aspire AppHost                       │
│                                                             │
│  ┌───────────────────┐     ┌──────────────────────┐        │
│  │   DataCrunch.Web  │     │  DataCrunchAgent     │        │
│  │   (Blazor Server) │     │  (Hosted Agent)      │        │
│  │                   │     │                      │        │
│  │   Port: auto      │     │  Port: 8088          │        │
│  │                   │────>│  /responses           │        │
│  │   Uses HttpClient │     │                      │        │
│  │   with service    │     │                      │        │
│  │   discovery URL:  │     │                      │        │
│  │   https+http://   │     │                      │        │
│  │   datacrunchagent │     │                      │        │
│  └───────────────────┘     └──────────────────────┘        │
│                                                             │
│  ┌─────────────────────────────────────────────────┐       │
│  │  Service Defaults (shared)                       │       │
│  │  • OpenTelemetry (traces, metrics, logs)         │       │
│  │  • Health checks (/health, /alive)               │       │
│  │  • HTTP client resilience (retries, timeouts)    │       │
│  │  • Service discovery registration                │       │
│  └─────────────────────────────────────────────────┘       │
└────────────────────────────────────────────────────────────┘
```

1. **`WithReference(agent)`** — Aspire injects the agent's address into the web frontend's configuration, making `https+http://datacrunchagent` resolvable at runtime
2. **`WithExternalHttpEndpoints()`** — The Blazor frontend is exposed to external traffic; the agent is internal-only
3. **Service discovery URL** — The web frontend uses `https+http://datacrunchagent` as its base address (`DataCrunch.Web/Program.cs`, line 11). Aspire's service discovery resolves this to the actual host and port

### Service Defaults

The `DataCrunch.ServiceDefaults` project (`Extensions.cs`) provides shared configuration applied via `builder.AddServiceDefaults()`:

| Feature | What it configures |
|---------|-------------------|
| **OpenTelemetry** | Traces (`AddAspNetCoreInstrumentation`, `AddHttpClientInstrumentation`), metrics (ASP.NET Core, HTTP client, runtime), structured logs |
| **Health checks** | `/health` (all checks) and `/alive` (liveness only) endpoints |
| **HTTP resilience** | `AddStandardResilienceHandler()` — retries, circuit breaker, timeouts on all outbound HTTP calls |
| **Service discovery** | `AddServiceDiscovery()` on both the DI container and HTTP client defaults |
| **OTLP export** | Conditional — if `OTEL_EXPORTER_OTLP_ENDPOINT` is set, exports telemetry via OTLP |

### Agent mode switching

The Blazor frontend supports two modes via configuration (`appsettings.json`):

```json
{
  "AgentMode": "Local",
  "AgentEndpoints": {
    "Local": "https+http://datacrunchagent",
    "Cloud": "https://YOUR-DEPLOYED-AGENT.foundry.azure.com"
  }
}
```

The `AgentMode` setting selects which endpoint to use. During Aspire development, `"Local"` routes through service discovery. After deploying the agent to Foundry, switch to `"Cloud"` to hit the production endpoint.

---

## 6. Deployment Pipeline

Every scenario follows a 5-stage workflow. Each stage maps to a script.

### The five stages

```
┌───────────┐    ┌───────────┐    ┌───────────┐    ┌───────────┐    ┌───────────┐
│ 1. setup  │───>│ 2. dotnet │───>│ 3. deploy │───>│ 4. test   │───>│ 5. cleanup│
│    .ps1   │    │    run    │    │    .ps1   │    │    .http   │    │    .ps1   │
│           │    │           │    │           │    │           │    │           │
│ Provision │    │ Local dev │    │ Package & │    │ Cloud     │    │ Tear down │
│ Azure     │    │ on :8088  │    │ deploy    │    │ testing   │    │ everything│
└───────────┘    └───────────┘    └───────────┘    └───────────┘    └───────────┘
```

### `setup.ps1` — Provision Azure Resources

1. Checks prerequisites (`azd`, `dotnet`, `az`, `docker`)
2. Installs the `azure.ai.agents` azd extension
3. Runs `azd init` with the Foundry starter template (if not already initialized)
4. Sets `ENABLE_HOSTED_AGENTS=true` and configures the service name
5. Registers the agent definition from `agent.yaml`
6. Runs `azd provision` — creates Foundry project, ACR, capability host
7. Deploys the `gpt-5-mini` model via `az cognitiveservices account deployment create`
8. Writes Azure endpoints to .NET User Secrets (not env vars, not `.env` files)
9. Prints the `az login --tenant` command for the provisioned tenant

### `deploy.ps1` — Package & Deploy

1. Reads azd environment values (account name, project name, ACR endpoint)
2. Deletes existing agent deployment if present (idempotent redeployment)
3. Runs `az cognitiveservices agent create --source <dir>` which:
   - Builds the Docker container from source
   - Pushes the image to Azure Container Registry
   - Creates (or updates) the hosted agent deployment on Foundry
4. Injects environment variables (`AZURE_OPENAI_ENDPOINT`, `AZURE_OPENAI_DEPLOYMENT_NAME`) into the container

### `cleanup.ps1` — Tear Down

1. Runs `azd down --purge --force` to delete all Azure resources
2. Removes `.azure/` folder and `azure.yaml`
3. Restores `agent.yaml` to the repo version (azd may have modified it)
4. Clears .NET User Secrets

### Docker containerization

All Dockerfiles use a two-stage build pattern:

```dockerfile
# scenario-1-intro/src/time-zone-agent/Dockerfile
FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build    # Build stage
WORKDIR /src
COPY . .
RUN dotnet restore && dotnet build -c Release --no-restore
RUN dotnet publish -c Release --no-build -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS final  # Runtime stage
WORKDIR /app
COPY --from=build /app .
EXPOSE 8088
HEALTHCHECK --interval=30s --timeout=5s --start-period=10s --retries=3 \
  CMD wget --no-verbose --tries=1 --spider http://localhost:8088/ || exit 1
ENTRYPOINT ["dotnet", "HostedAgent.dll"]
```

| Detail | Scenarios 1 & 2 | Scenario 3 |
|--------|-----------------|------------|
| Base image | `dotnet/aspnet:10.0-alpine` | `dotnet/aspnet:10.0` (non-alpine) |
| Why | Smallest image size | CUDA compatibility — alpine lacks required glibc for GPU libraries |
| Health check | `wget` | `curl` (available in non-alpine image) |
| Port | 8088 | 8088 |

### Agent identity: unpublished vs published

| State | Identity | Permissions |
|-------|---------|------------|
| **Unpublished** (default after `deploy.ps1`) | Uses the **project managed identity** | Inherits permissions from the Foundry project |
| **Published** (after publishing in Foundry portal) | Gets a **distinct agent identity** | Must be granted its own RBAC permissions on Azure resources |

This matters if your agent accesses Azure resources (storage, databases, etc.) beyond the model endpoint. After publishing, you need to reconfigure resource permissions for the new identity.

### `agent.yaml` — Agent definition

Each scenario includes an `agent.yaml` that defines the agent for Foundry:

```yaml
# scenario-1-intro/src/time-zone-agent/agent.yaml
kind: hosted
name: time-zone-agent
protocols:
    - protocol: responses
      version: v1
environment_variables:
    - name: AZURE_OPENAI_ENDPOINT
      value: ${AZURE_OPENAI_ENDPOINT}
    - name: AZURE_OPENAI_DEPLOYMENT_NAME
      value: gpt-5-mini
```

Key fields:
- **`kind: hosted`** — tells Foundry this is a containerized agent (not a prompt-only agent)
- **`protocols: responses v1`** — declares the agent speaks the Responses API
- **`environment_variables`** — injected into the container at runtime; `${VAR}` syntax pulls from the azd environment

---

## File Reference

| File | Purpose |
|------|---------|
| `scenario-1-intro/src/time-zone-agent/Program.cs` | Simplest agent — single tool, complete pattern in one file |
| `scenario-2-data-crunch/src/DataCrunchAgent/Program.cs` | Multi-tool agent with tool files in `Tools/` directory |
| `scenario-2-data-crunch/src/DataCrunchAgent/Tools/*.cs` | `DataParser`, `StatisticsCalculator`, `OutlierDetector` |
| `scenario-2-data-crunch/src/DataCrunch.Web/Services/AgentService.cs` | HTTP client + Responses API deserialization models |
| `scenario-2-data-crunch/src/DataCrunch.Web/Program.cs` | Blazor frontend with Aspire service discovery |
| `scenario-2-data-crunch/src/DataCrunch.AppHost/Program.cs` | Aspire orchestrator — wires agent + web together |
| `scenario-2-data-crunch/src/DataCrunch.ServiceDefaults/Extensions.cs` | Shared OpenTelemetry, health checks, resilience config |
| `scenario-3-image-gen/src/HostedAgent/Program.cs` | GPU agent — Stable Diffusion + FLUX.2 tools |
| `*/agent.yaml` | Agent definition for Foundry deployment |
| `*/Dockerfile` | Two-stage Docker builds (alpine for CPU, standard for GPU) |
| `*/setup.ps1` | Provision Azure resources + configure User Secrets |
| `*/deploy.ps1` | Build container → ACR push → Foundry deployment |
| `*/cleanup.ps1` | `azd down --purge` + clear local state |
| `*/test.http` | Ready-to-use HTTP requests for local and cloud testing |
