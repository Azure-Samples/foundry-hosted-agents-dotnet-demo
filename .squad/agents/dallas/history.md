# Project Context

- **Owner:** Bruno Capuano
- **Project:** Microsoft Foundry Hosted Agents — .NET Learning Lab. Three scenarios teaching hosted agent development from beginner to advanced.
- **Stack:** .NET 10, C#, Microsoft Agent Framework, Blazor Server, .NET Aspire, Docker, Azure (Foundry, ACR), azd CLI
- **Created:** 2026-04-08

## Key Files

- `scenario-1-intro/src/time-zone-agent/Program.cs` — Complete reference: single function tool agent
- `scenario-2-data-crunch/src/DataCrunchAgent/Program.cs` — WIP: multi-tool agent
- `scenario-2-data-crunch/src/DataCrunchAgent/Tools/` — StatisticsCalculator, OutlierDetector, DataParser
- `scenario-2-data-crunch/src/DataCrunch.AppHost/Program.cs` — .NET Aspire orchestration
- `scenario-3-image-gen/src/HostedAgent/Program.cs` — WIP: GPU image generation agent

## Learnings

<!-- Append new learnings below. Each entry is something lasting about the project. -->
- Test projects target net10.0 with xunit v3 (xunit 2.9.3 + runner 3.1.4), scaffolded via `dotnet new xunit`
- DataCrunchAgent.csproj has `<InternalsVisibleTo Include="DataCrunchAgent.Tests" />` so tests can reference the Exe project and access `internal` members like `Percentile()`
- Test projects referencing preview-feature projects need `<EnablePreviewFeatures>true</EnablePreviewFeatures>`
- Scenario 1 csproj lives at `scenario-1-intro/src/time-zone-agent/HostedAgent.csproj` (folder ≠ project name)
- Scenario 3 `ElBruno.Text2Image` and `.Cuda` packages pinned to v0.6.0 (community packages by @elbruno)
- slnx now includes all 3 scenario src + test projects, organized in `/scenario-N/{src,tests}/` folders
- Test paths: `scenario-{1,2,3}/tests/{TimeZoneAgent,DataCrunchAgent,ImageGenAgent}.Tests/`
- For top-level Program.cs agents (scenarios 1 & 3), tests replicate the logic locally rather than referencing the Exe
- Phase 1 delivered 65 xunit tests across 3 scenarios with full solution file integration
