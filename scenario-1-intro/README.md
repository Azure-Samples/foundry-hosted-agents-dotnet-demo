# Scenario 1 — Introduction to Hosted Agents

<!-- demo gif goes here -->

> **Start here.** This is the simplest hosted agent — one function tool, one file, five minutes to run. It teaches the core pattern that every hosted agent follows.

## What You'll Learn

- 🎯 The **hosted agent pattern**: function tool → chat client → agent → hosting adapter
- 🎯 How the **hosting adapter** wraps your agent as an HTTP service on `localhost:8088`
- 🎯 How **function tools** let the model call real C# code server-side
- 🎯 The **local testing flow**: run locally → test with REST calls → validate before containerizing
- 🎯 The **deployment flow**: `setup.ps1` → `dotnet run` → `deploy.ps1`

## Key Concepts

These map directly to the [official hosted agents documentation](https://learn.microsoft.com/en-us/azure/foundry/agents/concepts/hosted-agents):

| Concept | How This Scenario Demonstrates It |
|---------|----------------------------------|
| **Hosted Agent** | `Program.cs` — your agent is a .NET app that runs in a container. Foundry hosts and scales it. |
| **Hosting Adapter** | `RunAIAgentAsync()` — exposes the agent as an HTTP endpoint, handles protocol translation between Foundry Responses API and the Microsoft Agent Framework, and integrates OpenTelemetry observability. |
| **Function Tool** | `GetCurrentDateTime()` — a plain C# method the model can call. Runs server-side with real `TimeZoneInfo` data — not a prompt guess. |
| **Agent Identity** | When running locally, uses `DefaultAzureCredential`. When deployed (unpublished), uses the **project managed identity**. After publishing, gets a **distinct agent identity**. |
| **Managed Service** | After deployment, Foundry handles autoscaling, conversation orchestration, and enterprise security for your agent. |

## Prerequisites

| Tool | Install |
|---|---|
| .NET 10 SDK | https://dotnet.microsoft.com/download |
| Azure Developer CLI (`azd`) | https://aka.ms/install-azd |
| Azure CLI (`az`) | https://learn.microsoft.com/cli/azure/install-azure-cli |
| Docker Desktop | https://docs.docker.com/get-docker/ |
| Azure subscription | With access to Microsoft Foundry |

## Setup Environment

```powershell
./setup.ps1
```

This will:
1. Check prerequisites
2. Pull the azd Foundry starter template (`azd init`)
3. Register the agent definition from `src/HostedAgent/agent.yaml`
4. Provision Azure resources (`azd provision`)
5. Write a `.env` file with your endpoints

Then log in to the correct tenant (the script prints the command):

```powershell
az login --tenant <your-tenant-id>
```

## Run the Demo

```powershell
cd src/HostedAgent
dotnet run
```

The agent starts on `http://localhost:8088`. Test it:

```bash
curl -X POST http://localhost:8088/responses \
  -H "Content-Type: application/json" \
  -d '{"input": "What time is it in Tokyo?"}'
```

Or open `scenario-1-intro/test.http` in VS Code with the REST Client extension.

## What Happens

```
┌──────────────┐     ┌───────────────────┐     ┌───────────────┐
│  HTTP POST   │────▶│  Hosting Adapter   │────▶│  ChatClient   │
│  /responses  │     │  (AgentServer SDK) │     │  Agent        │
│  :8088       │◀────│  Protocol ↔ Agent  │◀────│  + gpt-5-mini  │
└──────────────┘     └───────────────────┘     └───────┬───────┘
                                                       │
                                              ┌────────▼────────┐
                                              │  Function Tool   │
                                              │  GetCurrentTime  │
                                              └─────────────────┘
```

1. You send a JSON request to `/responses`
2. The hosting adapter translates it and forwards to the agent
3. The agent calls GPT-5-mini, which decides to invoke the `GetCurrentDateTime` function tool
4. The C# method runs server-side, returns the time, and the agent responds

## Deploy to Azure (Optional)

```powershell
./deploy.ps1
```

This builds the Docker container, pushes it to Azure Container Registry, and creates a hosted agent deployment in Foundry. Once deployed, test your agent in the [Microsoft Foundry playground](https://ai.azure.com).

## Clean Up

```powershell
./cleanup.ps1
```

Deletes all Azure resources (`azd down --purge --force`), removes `.azure/` and `.env`.

## Next Steps

Once you're comfortable with this single-tool agent, move on to:

- ➡️ **[Scenario 2 — Data Crunch Agent](../scenario-2-data-crunch/)** — Multi-tool agent with Blazor frontend and .NET Aspire orchestration
- ➡️ **[Scenario 3 — Image Generator Agent](../scenario-3-image-gen/)** — GPU-powered image generation with Stable Diffusion and FLUX.2

---

> **↑ Back to [root README](../README.md)** · [Scenario 2 — Data Crunch](../scenario-2-data-crunch/README.md) · [Scenario 3 — Image Gen](../scenario-3-image-gen/README.md)

## Learn More

- [Hosted Agents Concepts](https://learn.microsoft.com/en-us/azure/foundry/agents/concepts/hosted-agents)
- [Microsoft Agent Framework (.NET)](https://github.com/microsoft/agents)
- [Foundry C# Samples](https://github.com/microsoft-foundry/foundry-samples/tree/main/samples/csharp/hosted-agents)
- [azd AI agent extension](https://learn.microsoft.com/en-us/azure/developer/azure-developer-cli/extensions/azure-ai-foundry-extension)
