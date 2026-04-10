# Decisions

## Dependabot + .env.example Configuration (Phase 4 Polish)

**Author:** Parker (DevOps/Infra)  
**Date:** 2025-07-24  
**Status:** Implemented

### Decision

#### 1. Dependabot Configuration (`.github/dependabot.yml`)
- **NuGet package updates:** Weekly schedule for all 3 scenario directories
  - Targets `/scenario-{1,2,3}-{intro,data-crunch,image-gen}` 
  - Includes prerelease packages (Azure.AI.AgentServer.AgentFramework, Azure.AI.OpenAI, ElBruno.Text2Image)
  - Dependency type: "all" (production and development)
- **Docker base images:** Weekly schedule for each scenario's Dockerfile
  - Scenario 1 & 2: Alpine-based `dotnet/sdk:10.0-alpine`
  - Scenario 3: Debian-based `dotnet/sdk:10.0` (CUDA compatibility)
- **GitHub Actions:** Root-level weekly schedule for workflow action versions

#### 2. Environment Variable Examples (`.env.example` per scenario)
- **Scenario 1 & 2:** Minimal config
  - `AZURE_OPENAI_ENDPOINT` (required)
  - `AZURE_OPENAI_DEPLOYMENT_NAME` (optional, defaults to "gpt-5-mini")
- **Scenario 3:** Extended config (image generation)
  - All Scenario 1 vars + `AZURE_AI_FOUNDRY_PROJECT_ENDPOINT` (required for FLUX.2)
- **Developer guidance:** Each `.env.example` includes:
  - Comment on copy → .env workflow
  - `dotnet user-secrets` instructions (recommended)
  - Placeholder values for clarity

### Rationale

#### Dependabot Benefits
- **Automated scanning:** NuGet prerelease packages (Agent Framework, OpenAI) are fast-moving; weekly checks catch new versions early
- **Isolated scenarios:** Directory-scoped updates prevent cross-scenario dependency conflicts
- **Docker base image coverage:** Alpine vs. Debian base images have different security patch cadences
- **Actions versioning:** GitHub Actions versions should be updated regularly for security

#### .env.example Benefits
- **Developer onboarding:** Quick reference for required config without secrets in source
- **User Secrets recommendation:** Guides developers to prefer secure local storage over plaintext .env files
- **Scenario clarity:** Developers immediately see that Scenario 3 needs Foundry endpoint
- **Deployment readiness:** Environment variables match exact Program.cs usage (verified from each agent's configuration loading)

### Notes
- No changes to CI/CD pipeline needed; Dependabot PRs integrate automatically with existing workflows
- `.env.example` files are NOT secrets and are safe to commit (placeholders only)
- Verified environment variable names match each scenario's Program.cs exactly

---

## Root-Level Community Files + WIP Status Updates (Phase 4 Polish)

**Author:** Brett (DevRel/Docs)  
**Date:** 2025-07-31  
**Status:** Implemented

### Context

GitHub surfaces community files (CONTRIBUTING.md, SECURITY.md, SUPPORT.md) in the repo UI only when they exist at the repository root. The lab had these files in `scenario-1-intro/` but not at root, making them invisible to users browsing the main repo page.

Similarly, Scenarios 2 and 3 had outdated status indicators that misrepresented their completion level:
- Scenario 2: Marked "🚧 WIP" but has 75% working code, unit tests, and complete Blazor frontend
- Scenario 3: Marked "🚧 WIP" but has 40% working code with core GPU integration functional

### Decision

#### 1. Root Community Files
Create three concise community files at repository root, adapted from scenario-1-intro/ versions for repo-wide scope:

- **CONTRIBUTING.md** — How to contribute to this learning lab (not a large OSS project; keep brief)
- **SECURITY.md** — Microsoft MSRC security reporting + lab-specific secure patterns
- **SUPPORT.md** — GitHub Issues + docs + official Microsoft Learn links

**Rationale:** These files are discovered and surfaced by GitHub on the repo main page, making them visible to users without digging through folders. Links point to the main README, docs/SCENARIOS.md, and official Foundry documentation.

#### 2. WIP Status Accuracy
Update status indicators across three locations:

**Root README scenarios table:**
- Scenario 1: Add "✅ Complete" status column
- Scenario 2: Change from "🚧 **WIP**" to "🟡 **Beta (75%)**" in Status column
- Scenario 3: Add "🚧 **WIP (40%)**" in Status column
- Add "Status" column header to clarify completion levels

**Scenario READMEs:**
- Scenario 2: Update banner to "🟡 **Beta** — substantially complete (75%) with working code, tests, and Blazor frontend"
- Scenario 3: Update banner to "🚧 **Work in Progress** — core GPU integration and image generation working (40% complete)"

**Rationale:** Status indicators now accurately reflect completion level. Users understand which scenarios are production-ready (Scenario 1), substantially usable with minor refinements (Scenario 2), or still in active development (Scenario 3).

#### 3. SCENARIOS.md Status Column
Add **Status** row to the Comparison Matrix in docs/SCENARIOS.md:

| Scenario | Status |
|----------|--------|
| Code Metrics | 📋 Proposed |
| Data Crunch | ✅ In Development |
| Secret Scanner | 📋 Proposed |
| Image Generator | 🟡 Beta |
| Style Transfer | 📋 Proposed |
| Storyboard | 📋 Proposed |

**Rationale:** The matrix now shows at a glance which proposed scenarios are actively being built vs. planned. Aligns with root README status updates.

### Visibility Impact

These changes ensure:
1. **Community files surface in GitHub UI** — CONTRIBUTING, SECURITY, SUPPORT now on main repo page
2. **Status clarity** — Users instantly see which scenarios are ready, in progress, or planned
3. **Scenario roadmap** — docs/SCENARIOS.md now shows the full pipeline (proposed → in development → beta/complete)
4. **Reduced user confusion** — No more guessing whether "WIP" means "use it carefully" or "don't use yet"

### Files Modified

- ✅ Created: `CONTRIBUTING.md` (repo root)
- ✅ Created: `SECURITY.md` (repo root)
- ✅ Created: `SUPPORT.md` (repo root)
- ✅ Updated: `README.md` (scenarios table with Status column)
- ✅ Updated: `scenario-2-data-crunch/README.md` (banner)
- ✅ Updated: `scenario-3-image-gen/README.md` (banner)
- ✅ Updated: `docs/SCENARIOS.md` (status row in comparison matrix)

---

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
