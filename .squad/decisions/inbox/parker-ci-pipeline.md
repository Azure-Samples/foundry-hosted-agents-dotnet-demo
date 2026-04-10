# Decision: CI Pipeline Added

**Author:** Parker (DevOps/Infra)
**Date:** 2025-07-24

## Summary

Created `.github/workflows/ci.yml` — a GitHub Actions CI pipeline that runs on push/PR to `main`.

## Details

- **Build & Test job:** Restores, builds, and tests the full `.slnx` solution with .NET 10 SDK.
- **Docker Validation job:** Uses a matrix strategy to build all 3 scenario Dockerfiles in parallel (`--target build` to validate the build stage only).
- Docker contexts are scoped to each project directory (not repo root) since no Dockerfile references parent paths.
- `fail-fast: false` on Docker matrix so one failing scenario doesn't mask others.

## Heads-up

The solution file (`foundry-hosted-agents-dotnet-demo.slnx`) references `scenario-1-intro/src/HostedAgent/` but the actual directory is `scenario-1-intro/src/time-zone-agent/`. Scenario-3 is also not listed in the solution. The build-and-test job may fail until the solution file is updated.
