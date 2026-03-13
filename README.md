# Microsoft Foundry Hosted Agents — .NET Learning Lab

A hands-on learning repo for building, testing, and deploying **hosted agents** on Microsoft Foundry using .NET 10 and the Microsoft Agent Framework. Three scenarios take you from a minimal single-tool agent to a GPU-powered image generator.

## What Are Hosted Agents?

Most AI agents are just prompts sent to a model. A **hosted agent** is different — it's a containerized application that runs **your code** server-side on Microsoft-managed infrastructure. The model decides *when* to call your C# methods, and your methods do the real work: compute, query, transform. No hallucinated math, no guessed-at results.

> **Official definition:** "Hosted agents are containerized agentic AI applications that run on Agent Service. Unlike prompt-based agents, developers build hosted agents through code and deploy them as container images on Microsoft-managed pay-as-you-go infrastructure."
>
> — [Microsoft Learn: Hosted Agents Concepts](https://learn.microsoft.com/en-us/azure/foundry/agents/concepts/hosted-agents)

```
Your C# Code → Container → Microsoft Foundry → Model (gpt-5-mini)
     ↕                          ↕
 Function Tools          Responses Protocol
 (real code)             (HTTP API)
```

### Why Not Just Use Prompts?

| Prompt-only agents | Hosted agents |
|---|---|
| Guess at math and statistics | Run exact C# computation |
| Can't access real-time system data | Call `TimeZoneInfo`, file I/O, APIs |
| No server-side execution | Your code runs in a managed container |
| Limited to model knowledge | Extend with any .NET library or GPU workload |

## Key Concepts

These concepts come directly from the [official Microsoft Learn documentation](https://learn.microsoft.com/en-us/azure/foundry/agents/concepts/hosted-agents) and are demonstrated across the scenarios in this repo:

| Concept | What It Does | Where You'll See It |
|---------|-------------|-------------------|
| **Hosting Adapter** | Framework abstraction layer that exposes your agent as an HTTP service. Provides simplified local testing on `localhost:8088`, automatic protocol translation between Foundry and your framework, and OpenTelemetry observability integration. | Every scenario — `RunAIAgentAsync()` in `Program.cs` |
| **Function Tools** | Plain C# methods the model can call — real server-side code execution, not prompt hacks | `GetCurrentDateTime` (Scenario 1), `ComputeStatistics` (Scenario 2), `GenerateImage` (Scenario 3) |
| **Agent Identity** | Unpublished agents use the **project managed identity**. Published agents get a **distinct agent identity** — you must reconfigure resource permissions after publishing. | `deploy.ps1` in each scenario |
| **Managed Service** | Foundry handles provisioning/autoscaling, conversation orchestration, identity management, tool/model integration, observability, and enterprise security. | Infrastructure provisioned via `setup.ps1` |
| **Framework Support** | Microsoft Agent Framework (Python + C#), LangGraph (Python only), Custom code (both). This repo uses **Microsoft Agent Framework for .NET**. | `ChatClientAgent` + `AIFunctionFactory` pattern |

## What You'll Learn

By working through these scenarios, you'll understand how to:

- ✅ Build a hosted agent with the Microsoft Agent Framework for .NET
- ✅ Define function tools that run real C# code server-side
- ✅ Use the hosting adapter to test locally on `localhost:8088` before containerizing
- ✅ Deploy to Microsoft Foundry using the `azd` CLI workflow
- ✅ Scale from a single tool to multi-tool agents with web frontends
- ✅ Use GPU workload profiles for compute-intensive agent tasks
- ✅ Follow security best practices (managed identities, no secrets in containers)

## Scenarios

Each scenario builds on the previous one. Start with Scenario 1 and progress from there.

| # | Scenario | Complexity | What It Teaches |
|---|----------|-----------|----------------|
| **1** | [**Intro — Time Zone Agent**](scenario-1-intro/) | 🟢 Beginner | The hosted agent pattern: one function tool, hosting adapter, local test, deploy. **Start here.** |
| **2** | [**Data Crunch Agent**](scenario-2-data-crunch/) | 🟡 Intermediate | Multi-tool agent with Blazor frontend and .NET Aspire orchestration. CSV analysis with real statistics. |
| **3** | [**Image Generator Agent**](scenario-3-image-gen/) | 🔴 Advanced (GPU) | GPU-powered image generation using Stable Diffusion and FLUX.2. Serverless GPU workload profiles. |

> 📖 See [docs/SCENARIOS.md](docs/SCENARIOS.md) for the full scenario design catalog, including three additional proposed scenarios.

## Quick Start

**➡️ [Start with Scenario 1 — Intro](scenario-1-intro/)** — it walks you through the complete workflow in about 5 minutes.

### Agent Development Workflow

Building a hosted agent follows five stages. Each scenario in this repo follows this exact pattern — you work locally first, then deploy when ready.

```
┌──────────────────────────────────────────────────────────────────┐
│                  Agent Development Workflow                      │
│                                                                  │
│   Stage 1          Stage 2           Stage 3          Stage 4    │
│  ┌─────────┐    ┌───────────┐    ┌────────────┐   ┌──────────┐  │
│  │ Provision│───▶│ Run & Test│───▶│  Package & │──▶│ Test on  │  │
│  │ Azure    │    │ Locally   │    │  Deploy    │   │ Foundry  │  │
│  └─────────┘    └───────────┘    └────────────┘   └──────────┘  │
│                                                        │         │
│                                          Stage 5 ──────┘         │
│                                         ┌──────────┐             │
│                                         │ Cleanup  │             │
│                                         └──────────┘             │
└──────────────────────────────────────────────────────────────────┘
```

#### Stage 1 — Provision Azure Resources

Set up the cloud infrastructure your agent needs: a Foundry project, model deployments (e.g. `gpt-5-mini`), Azure Container Registry, and supporting services. This only needs to run once per environment.

```powershell
cd scenario-1-intro        # Each scenario is self-contained
./setup.ps1                # Runs azd init + azd provision + configures .NET User Secrets
```

> `setup.ps1` also stores all Azure endpoints and configuration as [.NET User Secrets](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets) so you can run the agent locally without environment variables or `.env` files.

#### Stage 2 — Run & Test Locally

Your agent runs on your machine as a regular .NET app. It connects to the Azure model deployments provisioned in Stage 1, but the agent code itself runs locally — fast iteration, full debugger support.

```powershell
cd src/HostedAgent
dotnet run                 # Agent starts on http://localhost:8088
```

Test with any HTTP client:

```http
POST http://localhost:8088/responses
Content-Type: application/json

{
  "model": "TimeZoneAgent",
  "input": "What time is it in Tokyo?"
}
```

This is where you spend most of your development time — writing code, adding tools, testing prompts. No container builds, no deployments.

#### Stage 3 — Package & Deploy to Foundry

When your agent works locally, package it as a Docker container and push it to Microsoft Foundry. The `deploy.ps1` script builds the container image, pushes it to Azure Container Registry, and creates the hosted agent deployment.

```powershell
cd scenario-1-intro
./deploy.ps1               # docker build → ACR push → azd deploy
```

Behind the scenes, this runs:

```
azd deploy                 # Build container → push to ACR → deploy to Foundry
```

#### Stage 4 — Test on Foundry

Your agent is now running as a hosted agent on Microsoft-managed infrastructure. Test it using the same Responses API — this time routed through Foundry.

```http
POST https://<your-foundry-endpoint>/responses
Content-Type: application/json
Authorization: Bearer <token>

{
  "model": "TimeZoneAgent",
  "input": "What time is it in London?"
}
```

The [test.http](scenario-1-intro/test.http) file in each scenario has ready-to-use requests for both local and deployed testing.

#### Stage 5 — Cleanup (Optional)

When you're done, tear down all Azure resources to avoid charges.

```powershell
cd scenario-1-intro
./cleanup.ps1              # azd down --purge --force + clear User Secrets
```

### Command Summary

| Stage | Script / Command | What It Does |
|-------|-----------------|--------------|
| **1. Provision** | `./setup.ps1` | `azd init` + `azd provision` + configure .NET User Secrets |
| **2. Local test** | `dotnet run` | Run agent on `localhost:8088` — fast iteration |
| **3. Deploy** | `./deploy.ps1` | Docker build → ACR push → `azd deploy` to Foundry |
| **4. Cloud test** | `test.http` | Test the deployed agent via Foundry Responses API |
| **5. Cleanup** | `./cleanup.ps1` | `azd down --purge` + clear secrets + remove local state |

### Prerequisites

| Tool | Install |
|------|---------|
| .NET 10 SDK | https://dotnet.microsoft.com/download |
| Azure Developer CLI (`azd`) | https://aka.ms/install-azd |
| Azure CLI (`az`) | https://learn.microsoft.com/cli/azure/install-azure-cli |
| Docker Desktop | https://docs.docker.com/get-docker/ |
| Azure subscription | With access to Microsoft Foundry |

## Security

> **From the official docs:** "Don't put secrets in container images or environment variables. Use managed identities and connections, and store secrets in a managed secret store."
>
> — [Microsoft Learn: Security and Data Handling](https://learn.microsoft.com/en-us/azure/foundry/agents/concepts/hosted-agents#security-and-data-handling)

All scenarios in this repo use `DefaultAzureCredential` (managed identity) for authentication — no API keys or secrets in code. For production deployments, connect secrets through [Azure Key Vault](https://learn.microsoft.com/en-us/azure/ai-foundry/how-to/set-up-key-vault-connection).

## Official Documentation

| Resource | Link |
|----------|------|
| Hosted Agents Concepts | https://learn.microsoft.com/en-us/azure/foundry/agents/concepts/hosted-agents |
| Deploy a Hosted Agent | https://learn.microsoft.com/en-us/azure/foundry/agents/how-to/deploy-hosted-agent |
| Quickstart: First Hosted Agent | https://learn.microsoft.com/en-us/azure/foundry/agents/quickstarts/quickstart-hosted-agent |
| Microsoft Agent Framework (.NET) | https://github.com/microsoft/agents |
| Foundry C# Samples | https://github.com/microsoft-foundry/foundry-samples/tree/main/samples/csharp/hosted-agents |
| `azd` Agent Extension | https://learn.microsoft.com/en-us/azure/developer/azure-developer-cli/extensions/azure-ai-foundry-extension |

## License

[MIT](LICENSE)
