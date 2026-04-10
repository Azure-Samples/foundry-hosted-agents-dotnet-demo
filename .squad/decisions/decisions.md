# Decisions

## Docker Hardening — .dockerignore + HEALTHCHECK

**Author:** Parker (DevOps/Infra)  
**Date:** 2025-07-24  
**Status:** Implemented

### Decision

- Added `.dockerignore` files to all three scenario build contexts with standard .NET exclusions.
- Added `HEALTHCHECK` instructions to all Dockerfiles targeting port 8088.
- Alpine-based images (scenarios 1 & 2) use `wget` (BusyBox built-in); Debian-based image (scenario 3) uses `curl`.
- Health checks: 30s interval, 5s timeout, 10s start period, 3 retries.

### Rationale

- `.dockerignore` reduces build context size, speeds up builds, and prevents leaking development artifacts into images.
- `HEALTHCHECK` enables Docker and orchestrators (Azure Container Apps) to detect unhealthy containers and restart them automatically.
- Tool selection (`wget` vs `curl`) matches what ships with each base image — no extra package installs needed.
