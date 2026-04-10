# Project Context

- **Owner:** Bruno Capuano
- **Project:** Microsoft Foundry Hosted Agents — .NET Learning Lab. Three scenarios teaching hosted agent development from beginner to advanced (time zone agent → data crunch with Blazor/Aspire → GPU image gen).
- **Stack:** .NET 10, C#, Microsoft Agent Framework, Blazor Server, .NET Aspire, Docker, Azure (Foundry, ACR), azd CLI
- **Created:** 2026-04-08

## Key Files

- `foundry-hosted-agents-dotnet-demo.slnx` — solution file covering all scenarios
- `scenario-1-intro/` — Complete. Time Zone Agent with single function tool.
- `scenario-2-data-crunch/` — WIP. Multi-tool agent with Blazor frontend and .NET Aspire orchestration.
- `scenario-3-image-gen/` — WIP. GPU-powered image generation.
- `docs/PLAN.md` — Detailed implementation plan
- `docs/SCENARIOS.md` — Scenario design catalog with additional proposals

## Learnings

<!-- Append new learnings below. Each entry is something lasting about the project. -->

- **2025-07-24 — Created `docs/ARCHITECTURE.md`**: Comprehensive technical architecture document (~400 lines) covering all six required sections: hosting adapter protocol, Responses API schema, credential switching, function tools architecture, .NET Aspire service discovery, and deployment pipeline. All content sourced from actual code with file/line references. Fills the gap between the "what" (README) and the "how" (internals).
- The hosting adapter (`RunAIAgentAsync`) is from `Azure.AI.AgentServer.AgentFramework` v1.0.0-beta.10 — same version pinned across all three scenarios. Single extension method handles HTTP server, protocol translation, and telemetry.
- The credential switch relies on `DOTNET_RUNNING_IN_CONTAINER` which is set automatically by official .NET Docker images — zero custom configuration needed.
- Scenario 3 uses non-alpine Docker base image (`dotnet/aspnet:10.0` instead of `10.0-alpine`) because CUDA requires glibc. This is a meaningful distinction worth documenting.
- `deploy.ps1` uses `az cognitiveservices agent create --source` which handles Docker build, ACR push, and agent creation in a single CLI command.
