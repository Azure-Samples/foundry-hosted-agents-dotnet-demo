# Squad Decisions

## Active Decisions

### 1. Test Strategy for Single-File Agents
**Author:** Dallas (Backend Dev)  
**Date:** 2025-07-18  
**Status:** Implemented

**Decision:**
- **Scenario 1 & 3:** Tests replicate logic locally (TimeZoneInfo calls, string formatting) rather than refactoring single-file agents
- **Scenario 2:** Tests reference DataCrunchAgent directly via project reference + `InternalsVisibleTo`

**Rationale:** Learning repo where single-file simplicity is a feature. Extracting logic would add unnecessary complexity.

### 2. CI Pipeline Architecture
**Author:** Parker (DevOps/Infra)  
**Date:** 2025-07-24  
**Status:** Implemented

**Decision:**
- Created `.github/workflows/ci.yml` with two jobs:
  - **build-and-test:** Restores, builds, runs tests on full .slnx solution
  - **docker-validate:** Matrix build of all 3 scenario Dockerfiles in parallel (`--target build`)
- Docker contexts scoped to each project directory (no parent path references)
- `fail-fast: false` on Docker matrix for isolated failure detection

**Rationale:** Parallel validation catches scenario-specific Docker issues without blocking others. Targeting build stage validates syntax without full image push.

## Governance

- All meaningful changes require team consensus
- Document architectural decisions here
- Keep history focused on work, decisions focused on direction
