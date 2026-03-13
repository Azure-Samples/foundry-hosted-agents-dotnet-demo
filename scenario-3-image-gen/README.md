# Image Generator Agent — GPU-Powered Hosted Agent (.NET)

A .NET 10 hosted agent that generates images from text descriptions using GPU-accelerated **Stable Diffusion 1.5** (local) and cloud-based **FLUX.2** (Microsoft Foundry). Orchestrated by gpt-5-mini via the Microsoft Agent Framework.

> **Prerequisites:** Complete [Scenario 1](../scenario-1-intro/) first to understand the basic hosted agent pattern. This scenario adds GPU workload profiles on top of the same architecture.

## What You'll Learn

- 🎯 How to add **GPU workload profiles** to a hosted agent deployment
- 🎯 Using **external NuGet packages** (ElBruno.Text2Image) as function tools
- 🎯 Running **compute-intensive operations** (diffusion model inference) server-side
- 🎯 The difference between **local GPU** (ONNX Runtime + CUDA) and **cloud inference** (FLUX.2)
- 🎯 **Scale-to-zero GPU billing** — pay only when the agent is generating images

## Key Concepts

- **Hosted Agent** — Your agent runs as a container in Microsoft Foundry. You write the code, Foundry hosts and scales it.
- **GPU Workload Profile** — The container runs on Azure Container Apps with a serverless GPU (NVIDIA A100/T4) for fast image generation. Scale-to-zero billing means you only pay when generating.
- **Function Tools** — Three C# methods the model can call:
  - `GenerateImage` — Local GPU generation via Stable Diffusion 1.5 (ONNX Runtime + CUDA). Fast, no cloud dependency.
  - `GenerateImageFlux` — Cloud generation via FLUX.2 on Microsoft Foundry. Higher quality, better text rendering.
  - `ListModels` — Returns available models and their capabilities.

## Prerequisites

| Tool | Install |
|---|---|
| .NET 10 SDK | https://dotnet.microsoft.com/download |
| Azure Developer CLI (`azd`) | https://aka.ms/install-azd |
| Azure CLI (`az`) | https://learn.microsoft.com/cli/azure/install-azure-cli |
| Docker Desktop | https://docs.docker.com/get-docker/ |
| Azure subscription | With access to Microsoft Foundry |
| GPU environment | Azure Container Apps with GPU workload profile (for deployment) |

> **Local development:** A CUDA-capable GPU is recommended for local Stable Diffusion inference. Without a GPU, generation will fall back to CPU (slower). FLUX.2 always runs in the cloud.

## Setup Environment

```powershell
./setup.ps1
```

This will:
1. Check prerequisites
2. Pull the azd Foundry starter template (`azd init`)
3. Register the agent definition from `src/HostedAgent/agent.yaml`
4. Provision Azure resources (`azd provision`)
5. Configure .NET User Secrets with your endpoints

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
  -d '{"input": "Generate an image of a sunset over mountains"}'
```

Or open `scenario-3-image-gen/test.http` in VS Code with the REST Client extension.

## What Happens

```
┌──────────────┐     ┌───────────────────┐     ┌───────────────┐
│  HTTP POST   │────▶│  Hosting Adapter   │────▶│  ChatClient   │
│  /responses  │     │  (AgentServer SDK) │     │  Agent        │
│  :8088       │◀────│  Protocol ↔ Agent  │◀────│  + gpt-5-mini │
└──────────────┘     └───────────────────┘     └───────┬───────┘
                                                       │
                                              ┌────────▼────────┐
                                              │  Function Tools  │
                                              │  GenerateImage   │──▶ GPU (ONNX/CUDA)
                                              │  GenerateFlux    │──▶ Microsoft Foundry
                                              │  ListModels      │
                                              └─────────────────┘
```

1. You send a JSON request to `/responses`
2. The hosting adapter translates it and forwards to the agent
3. The agent calls gpt-5-mini, which decides to invoke a function tool
4. **GenerateImage** runs Stable Diffusion 1.5 on the local GPU via ONNX Runtime
5. **GenerateImageFlux** calls FLUX.2 on Microsoft Foundry for higher-quality output
6. The image is saved to `output/` and the agent describes what was generated

## Image Generation Packages

This agent uses [ElBruno.Text2Image](https://www.nuget.org/packages/ElBruno.Text2Image) for image generation:

| Package | Purpose |
|---|---|
| `ElBruno.Text2Image` | Core image generation API (Stable Diffusion 1.5, FLUX.2) |
| `ElBruno.Text2Image.Cuda` | ONNX Runtime CUDA binaries for GPU acceleration |

## Deploy to Azure (Optional)

```powershell
./deploy.ps1
```

This builds the Docker container, pushes it to Azure Container Registry, and creates a hosted agent deployment in Foundry with a GPU workload profile. Once deployed, test your agent in the [Microsoft Foundry playground](https://ai.azure.com).

> **Note:** The deployed container runs on a GPU workload profile with scale-to-zero. You only pay for GPU time when the agent is actively generating images.

## Clean Up

```powershell
./cleanup.ps1
```

Deletes all Azure resources (`azd down --purge --force`), removes `.azure/`, clears .NET User Secrets, and removes generated images.

---

> **↑ Back to [root README](../README.md)** · [Scenario 1 — Intro](../scenario-1-intro/README.md) · [Scenario 2 — Data Crunch](../scenario-2-data-crunch/README.md)

## Learn More

- [Hosted Agents Concepts](https://learn.microsoft.com/en-us/azure/foundry/agents/concepts/hosted-agents)
- [Microsoft Agent Framework (.NET)](https://github.com/microsoft/agents)
- [Foundry C# Samples](https://github.com/microsoft-foundry/foundry-samples/tree/main/samples/csharp/hosted-agents)
- [ElBruno.Text2Image on NuGet](https://www.nuget.org/packages/ElBruno.Text2Image)
- [Azure Container Apps GPU workload profiles](https://learn.microsoft.com/azure/container-apps/gpu-workload-profiles)
- [azd AI agent extension](https://learn.microsoft.com/en-us/azure/developer/azure-developer-cli/extensions/azure-ai-foundry-extension)
