# Scenario 1 — Intro: Time Zone Agent

> 🎯 The simplest possible hosted agent. Learn the core hosting adapter pattern with a single function tool: `GetCurrentDateTime(string ianaTimezone)`. Your introduction to how Foundry-hosted agents work in .NET.

> 🚧 **Work in Progress** — This scenario is under active development. Code and documentation may be incomplete or change without notice.

## What You'll Learn

- 🎯 The **hosted agent hosting adapter pattern** — how your C# code becomes an HTTP-callable agent in Foundry
- 🎯 Writing and exposing **function tools** — real methods the LLM calls server-side (no hallucinations)
- 🎯 **Credential switching** — `AzureCliCredential` for local development, `DefaultAzureCredential` in containers
- 🎯 Using the **Microsoft Agent Framework** with Azure OpenAI
- 🎯 Testing agents locally with REST calls before deploying

## What Are Hosted Agents?

A **hosted agent** is your .NET code running as a containerized service in Microsoft Foundry. You write the agent logic and expose **function tools** — plain C# methods the LLM can invoke. The LLM decides *when* to call them; your code provides the *exact* answer. No hallucinations, no guessing.

This scenario is the minimal starting point. It has one tool: ask for the time in any timezone. Start here, then graduate to multi-tool agents (Scenario 2) and GPU workloads (Scenario 3).

> **First time here?** Read the [main README](../README.md) for core concepts and architecture overview.

## Architecture

```
┌─────────────────────────────────────┐
│  You (REST client)                  │
│  POST /responses                    │
│  {"input": "Time in Tokyo?"}        │
└──────────────┬──────────────────────┘
               │
               ▼
┌─────────────────────────────────────┐
│  Hosting Adapter (port 8088)        │
│  (AgentServer SDK)                  │
│  • Protocol translation             │
│  • OpenTelemetry integration        │
└──────────────┬──────────────────────┘
               │
               ▼
┌─────────────────────────────────────┐
│  ChatClientAgent                    │
│  • Instructions: "Help with time"   │
│  • Tools: [GetCurrentDateTime]      │
└──────────────┬──────────────────────┘
               │
        ┌──────▼──────┐
        │             │
        ▼             ▼
   gpt-5-mini    GetCurrentDateTime()
   (Azure        (C# method)
    OpenAI)      TimeZoneInfo.ConvertTime()
                 ↓ Real answer
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
cd src/time-zone-agent
dotnet run
```

You'll see:
```
TimeZoneAgent running on http://localhost:8088
```

## How to Use

Test the agent with any REST client. Open `test.http` in VS Code with the REST Client extension, or use `curl`:

```bash
curl -X POST http://localhost:8088/responses \
  -H "Content-Type: application/json" \
  -d '{"input": "What time is it in Tokyo?"}'
```

The agent will:
1. Parse your question
2. Decide to call `GetCurrentDateTime("Asia/Tokyo")`
3. Return the current time in that timezone

Try these:
- "What time is it in London?"
- "What's the current time in New York and Sydney?"
- "Tell me the time in UTC"

## Function Tool

The `GetCurrentDateTime` tool:

| Parameter | Type | Description |
|-----------|------|-------------|
| `ianaTimezone` | string | IANA timezone identifier (e.g., `America/New_York`, `Asia/Tokyo`, `Europe/London`, `UTC`) |

**Returns:** Formatted current time in the specified timezone.

Accepts any timezone from `TimeZoneInfo.GetSystemTimeZones()`.

## Local vs Cloud Mode

| Mode | Command | Where agent runs |
|------|---------|------------------|
| **Local** | `dotnet run` from `src/time-zone-agent/` | Your machine (debugging + fast iteration) |
| **Cloud** | `./deploy.ps1` | Microsoft Foundry container (production) |

In both modes, the agent calls Azure OpenAI for LLM inference. The agent container location is the difference.

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
scenario-1-intro/
├── src/
│   └── time-zone-agent/
│       ├── Program.cs          # Single-file agent (hosting adapter + tools)
│       ├── Dockerfile          # Container definition
│       └── *.csproj            # Project file
├── setup.ps1                   # Provision Azure resources
├── deploy.ps1                  # Deploy agent to Azure
├── cleanup.ps1                 # Tear down Azure resources
├── azure.yaml                  # azd project definition
├── test.http                   # REST Client test requests
└── README.md                   # This file
```

## Troubleshooting

| Problem | Solution |
|---------|----------|
| `dotnet run` fails with "AZURE_OPENAI_ENDPOINT not set" | Run `./setup.ps1` first to provision Azure resources and configure secrets |
| `az login` fails | Ensure you have a valid Azure subscription and access to Microsoft Foundry in your region |
| Port 8088 already in use | Kill the process: `Get-Process -Name "dotnet" \| Stop-Process` or use `netstat -ano \| findstr :8088` |
| Agent doesn't call the tool | Check the agent instructions in `Program.cs` — the LLM may not recognize the timezone format. Try "the current time in Tokyo" instead of "time in Asia/Tokyo" |
| Deployed agent unreachable | Verify the agent deployed successfully: `az ai agent show --agent-id <id>` and check Microsoft Foundry for runtime errors |

## Clean Up

Remove all Azure resources and local configuration:

```powershell
./cleanup.ps1
```

This runs `azd down --purge --force` and clears .NET User Secrets.

---

> **↑ Back to [root README](../README.md)** · [Scenario 2 — Data Crunch](../scenario-2-data-crunch/README.md) · [Scenario 3 — Image Gen](../scenario-3-image-gen/README.md)
