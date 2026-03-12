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

**➡️ [Start with Scenario 1 — Intro](scenario-1-intro/)** — it walks you through the complete flow in about 5 minutes.

The pattern every scenario follows:

```powershell
cd scenario-1-intro   # Navigate to the scenario folder first
setup.ps1             → Provision Azure resources (azd init + provision)
dotnet run            → Test locally on localhost:8088
deploy.ps1            → Build container, push to ACR, deploy to Foundry
cleanup.ps1           → Tear down all Azure resources
```

### Prerequisites

| Tool | Install |
|------|---------|
| .NET 10 SDK | https://dotnet.microsoft.com/download |
| Azure Developer CLI (`azd`) | https://aka.ms/install-azd |
| Azure CLI (`az`) | https://learn.microsoft.com/cli/azure/install-azure-cli |
| Docker Desktop | https://docs.docker.com/get-docker/ |
| Azure subscription | With access to Microsoft Foundry |

### Deployment Flow

This repo uses the [Azure Developer CLI (`azd`) agent extension](https://learn.microsoft.com/en-us/azure/developer/azure-developer-cli/extensions/azure-ai-foundry-extension) for deployment:

```
azd init -t azd-ai-starter-basic     # Pull Foundry starter template
azd ai agent init -m agent.yaml      # Register agent definition
azd provision                         # Create Azure infrastructure
azd deploy                            # Build container → push to ACR → deploy
```

Or use `azd up` to provision and deploy in one step.

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
