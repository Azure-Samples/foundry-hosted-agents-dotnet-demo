# Scenario 2 — Data Crunch Agent (End-to-End Application)

<!-- demo gif goes here -->

The **Data Crunch Agent** is a full-stack application that analyzes CSV data using an AI-powered hosted agent. Upload a CSV file through the Blazor web frontend, and the agent parses the data, computes statistics, detects outliers, and returns a structured analysis — all orchestrated by .NET Aspire.

> **New to hosted agents?** Start with the [main README](../README.md) — it covers the core concepts and walks through a minimal example first.

---

## What You'll Learn

- 🎯 Building a **multi-tool hosted agent** (3 function tools working together)
- 🎯 Integrating a hosted agent with a **Blazor web frontend**
- 🎯 Using **.NET Aspire** to orchestrate multiple services (agent + web UI + telemetry)
- 🎯 Why **server-side computation matters**: LLMs are notoriously bad at math, but C# is exact
- 🎯 The full development loop: upload data → agent processes → structured results

## What Are Hosted Agents?

A **hosted agent** is your code — a .NET application — running as a container in Microsoft Foundry. You write the agent logic and function tools in C#. Foundry hosts, scales, and connects it to models. Function tools are plain C# methods the model can call — real code execution, not prompt hacks.

This scenario builds on that pattern with **three tools**, a **Blazor web frontend**, and **.NET Aspire** for orchestration. The key insight: LLMs are notoriously bad at math, but C# is exact. When the model needs to compute a median or detect outliers, it calls your code — and the answer is right every time.

## Architecture

```
┌─────────────────────────────────────────────────────┐
│              .NET Aspire AppHost                    │
│           (DataCrunch.AppHost)                      │
│                                                     │
│  ┌──────────────────┐    ┌───────────────────────┐  │
│  │   Blazor Web UI  │───▶│  Data Crunch Agent    │  │
│  │  (DataCrunch.Web)│    │  (DataCrunchAgent)    │  │
│  │                  │    │                       │  │
│  │  • Upload CSV    │    │  Tools:               │  │
│  │  • View results  │    │  • ParseData          │  │
│  │  • Download      │    │  • ComputeStatistics  │  │
│  │                  │    │  • DetectOutliers      │  │
│  └──────────────────┘    └───────────────────────┘  │
│                                                     │
│  ┌──────────────────────────────────────────────┐   │
│  │         Service Defaults                     │   │
│  │     (DataCrunch.ServiceDefaults)             │   │
│  │  • OpenTelemetry  • Health checks            │   │
│  └──────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────┘
```

## Prerequisites

| Tool | Install |
|------|---------|
| **.NET 10 SDK** | https://dotnet.microsoft.com/download |
| **Aspire workload** | `dotnet workload install aspire` |
| **Azure CLI (`az`)** | https://learn.microsoft.com/cli/azure/install-azure-cli |
| **Azure Developer CLI (`azd`)** | https://aka.ms/install-azd |
| **Docker Desktop** | https://docs.docker.com/get-docker/ |
| **Azure subscription** | With access to Microsoft Foundry |

## Quick Start

### 1. Setup Azure resources

```powershell
./setup.ps1
```

This provisions the Microsoft Foundry project, model deployment, and supporting infrastructure. You will be prompted for a subscription and region.

### 2. Log in to Azure

After setup completes, run the `az login` command shown in the output:

```powershell
az login --tenant <your-tenant-id>
```

### 3. Run the application

```powershell
dotnet run --project src/DataCrunch.AppHost
```

### 4. Open the apps

| App | URL |
|-----|-----|
| **Blazor frontend** | http://localhost:5000 |
| **Aspire Dashboard** | https://localhost:15888 |
| **Agent API (direct)** | http://localhost:8088 |

## How to Use

1. Open the **Blazor app** at http://localhost:5000
2. **Upload a CSV file** (or use one from `sample-data/`)
3. Click **Analyze**
4. The agent processes your data and returns:
   - Parsed column types and row counts
   - Descriptive statistics (mean, median, std dev, min, max)
   - Detected outliers with explanations

## Sample Data

Three sample CSV files are included in `sample-data/`:

| File | Description |
|------|-------------|
| `api-response-times.csv` | API endpoint response times with status codes — includes slow/failing endpoints to detect |
| `sales-quarterly.csv` | Quarterly sales figures — good for trend analysis and seasonal patterns |
| `sensor-readings.csv` | IoT sensor temperature readings — contains anomalous spikes for outlier detection |

## Agent Tools

The Data Crunch Agent exposes three tools to the LLM:

| Tool | Purpose |
|------|---------|
| **ParseData** | Parses raw CSV text into structured columns and rows. Identifies column types (numeric, categorical, datetime). |
| **ComputeStatistics** | Calculates descriptive statistics for numeric columns: mean, median, standard deviation, min, max, percentiles. |
| **DetectOutliers** | Identifies outlier values using statistical methods (IQR / z-score). Returns flagged rows with explanations. |

## Local vs Cloud Mode

| Mode | How to run | Notes |
|------|-----------|-------|
| **Local** | `dotnet run --project src/DataCrunch.AppHost` | Uses Microsoft Foundry endpoint from `.env`; agent runs locally in a container |
| **Cloud** | `./deploy.ps1` | Deploys the agent container to Microsoft Foundry as a hosted agent |

In both modes, the agent calls Azure OpenAI for LLM inference. The difference is where the agent container itself runs.

## Deploy to Azure

After running `setup.ps1` and verifying everything works locally:

```powershell
./deploy.ps1
```

This builds the Docker container, pushes it to Azure Container Registry, and deploys it as a hosted agent in Microsoft Foundry. Test the deployed agent in the [Microsoft Foundry Playground](https://ai.azure.com).

## Clean Up

Remove all Azure resources and local configuration:

```powershell
./cleanup.ps1
```

This runs `azd down --purge --force` and removes the `.azure/` folder and `.env` file.

## Project Structure

```
scenario-2-data-crunch/
├── src/
│   ├── DataCrunch.AppHost/        # Aspire orchestrator
│   ├── DataCrunch.ServiceDefaults/ # Shared config (telemetry, health)
│   ├── DataCrunch.Web/            # Blazor frontend
│   └── DataCrunchAgent/           # Hosted agent (API + tools)
├── sample-data/                   # Example CSV files
│   ├── api-response-times.csv
│   ├── sales-quarterly.csv
│   └── sensor-readings.csv
├── setup.ps1                      # Provision Azure resources
├── deploy.ps1                     # Deploy agent to Azure
├── cleanup.ps1                    # Tear down Azure resources
├── azure.yaml                     # azd project definition
├── test.http                      # REST Client test requests
└── README.md                      # This file
```

## Troubleshooting

| Problem | Solution |
|---------|----------|
| `dotnet workload list` doesn't show aspire | Run `dotnet workload install aspire` |
| Blazor app won't load | Ensure the AppHost is running — it orchestrates all services |
| Agent returns errors | Check that `.env` has valid `AZURE_AI_FOUNDRY_PROJECT_ENDPOINT` |
| Docker build fails | Ensure Docker Desktop is running and has sufficient resources |
| `azd provision` fails | Verify your subscription has access to Microsoft Foundry in the selected region |

---

> **↑ Back to [root README](../README.md)** · [Scenario 1 — Intro](../scenario-1-intro/README.md) · [Scenario 3 — Image Gen](../scenario-3-image-gen/README.md)
