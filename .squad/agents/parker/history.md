# Project Context

- **Owner:** Bruno Capuano
- **Project:** Microsoft Foundry Hosted Agents — .NET Learning Lab. Three scenarios teaching hosted agent development from beginner to advanced.
- **Stack:** .NET 10, C#, Microsoft Agent Framework, Blazor Server, .NET Aspire, Docker, Azure (Foundry, ACR), azd CLI
- **Created:** 2026-04-08

## Key Files

- `scenario-1-intro/setup.ps1` — Azure provisioning (reference implementation)
- `scenario-1-intro/deploy.ps1` — Docker build + ACR push + azd deploy
- `scenario-1-intro/cleanup.ps1` — Teardown script
- `scenario-1-intro/infra/` — Azure infrastructure templates
- `scenario-2-data-crunch/setup.ps1`, `deploy.ps1`, `cleanup.ps1` — WIP
- `scenario-3-image-gen/setup.ps1`, `deploy.ps1`, `cleanup.ps1` — WIP

## Learnings

<!-- Append new learnings below. Each entry is something lasting about the project. -->

- **CI workflow:** `.github/workflows/ci.yml` — build verification pipeline (build+test .NET solution, Docker validation for all 3 scenarios via matrix). Triggers on push/PR to main.
- **Dockerfiles:** All three scenario Dockerfiles use `COPY . .` with no parent-directory references, so each project directory is its own build context. All have a `build` stage target.
- **Solution file:** Fixed stale path (`scenario-1-intro/src/HostedAgent/` → `scenario-1-intro/src/time-zone-agent/`) and added Scenario-3 to `.slnx` in Phase 1.
- **Docker base images:** scenario-1 and scenario-2 use `dotnet/sdk:10.0-alpine`; scenario-3 uses `dotnet/sdk:10.0` (non-alpine, for CUDA compat).
- **Docker matrix strategy:** `fail-fast: false` ensures one scenario's Docker build failure doesn't hide issues in others.
- Phase 1 CI/CD pipeline now validates solution build/test + Docker scenarios in parallel
- **Phase 2 — .dockerignore:** Added `.dockerignore` in each scenario's build context directory. Standard .NET exclusions (`bin/`, `obj/`, `.git/`, `.vs/`, `.vscode/`, `*.md`, `*.http`, `agent.yaml`, `.dockerignore`, `Dockerfile`). Keeps Docker contexts lean.
- **Phase 2 — HEALTHCHECK:** Added `HEALTHCHECK` instructions to all three Dockerfiles (port 8088). Alpine images (scenarios 1 & 2) use `wget` (BusyBox built-in); Debian image (scenario 3) uses `curl`. Placed before ENTRYPOINT.
- **Alpine vs Debian tooling:** Alpine-based .NET images ship BusyBox `wget` but not `curl`. Debian-based .NET images have `curl` available. Always match health-check tool to base image.
