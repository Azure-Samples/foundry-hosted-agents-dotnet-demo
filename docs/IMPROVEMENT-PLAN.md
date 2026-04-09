# Improvement Plan — Foundry Hosted Agents .NET Learning Lab

> **Status:** Proposed (not yet implemented)
> **Analyzed by:** Squad — Ripley (Lead), Dallas (Backend), Parker (DevOps), Brett (DevRel)
> **Date:** 2026-04-08

---

## Executive Summary

The repo is a **mature, well-documented learning lab** scoring **B+ (7.5/10)** overall. Code quality is high, security posture is strong (managed identity, no secrets in containers), and the root README is excellent. The biggest gaps are **zero test coverage** and **no CI/CD pipeline**. Scenarios 2 and 3 are marked WIP but have substantial working code.

---

## 1. Critical Priority 🔴

### 1.1 Zero Test Coverage

- **Finding:** No test projects exist anywhere in the repo — no `xunit`, no `nunit`, no test runner.
- **Impact:** No automated way to verify tools work correctly; regressions go undetected.
- **Recommendation:** Add test projects per scenario:
  - `scenario-1-intro/tests/TimeZoneAgent.Tests/` — unit tests for the time zone function tool
  - `scenario-2-data-crunch/tests/DataCrunchAgent.Tests/` — tests for `DataParser`, `StatisticsCalculator`, `OutlierDetector`
  - `scenario-3-image-gen/tests/ImageGenAgent.Tests/` — tests for image generation tools
- **Priority targets:** Scenario 2's three tool classes are pure functions — ideal first test candidates.

### 1.2 No CI/CD Pipeline

- **Finding:** No GitHub Actions workflow for build/test/deploy. Only `.github/workflows/` contains squad management workflows.
- **Impact:** PRs merge without build verification; no automated quality gate.
- **Recommendation:** Add `.github/workflows/ci.yml`:
  - Trigger on `push` and `pull_request` to `main`
  - Matrix build across all 3 scenarios
  - Run tests (once test projects exist)
  - Validate Dockerfiles build successfully
  - Optional: deploy to staging on merge to `main`

### 1.3 Wildcard Package Versions in Scenario 3

- **Finding:** `ElBruno.Text2Image` and `ElBruno.Text2Image.Cuda` use `Version="v*"` — pulls whatever latest is available.
- **Impact:** Builds are non-reproducible; a breaking upstream change silently breaks the project.
- **Recommendation:** Pin to specific versions (e.g., `Version="1.2.3"`). Add a Dependabot config to manage updates.

---

## 2. High Priority 🟠

### 2.1 Missing `.dockerignore` Files

- **Finding:** No `.dockerignore` in any scenario — entire repo context is sent to Docker daemon.
- **Impact:** Slower builds, larger contexts, potential leaking of `.git/` and other unnecessary files.
- **Recommendation:** Add `.dockerignore` to each scenario with standard .NET exclusions (`bin/`, `obj/`, `.git/`, `*.md`, etc.).

### 2.2 No `HEALTHCHECK` in Dockerfiles

- **Finding:** None of the Dockerfiles include a `HEALTHCHECK` instruction.
- **Impact:** Container orchestrators can't detect unhealthy containers for auto-restart.
- **Recommendation:** Add `HEALTHCHECK CMD curl --fail http://localhost:8088/health || exit 1` (or equivalent) to each Dockerfile.

### 2.3 Error Handling Gaps

- **Finding:**
  - `AgentService.cs` (Scenario 2) has a bare `catch` block that swallows all exceptions.
  - `Program.cs` files lack try-catch around credential initialization (`AzureCliCredential` / `DefaultAzureCredential`).
  - Scenario 3 uses null-forgiving operator `.GetString()!` which can throw `NullReferenceException`.
- **Recommendation:**
  - Catch specific exceptions (`JsonException`, `AuthenticationFailedException`) and log them.
  - Add startup validation for required environment variables (fail fast with clear message).
  - Replace `!` null-forgiving with proper null checks.

### 2.4 Prerelease Package Tracking

- **Finding:**
  - `Azure.AI.AgentServer.AgentFramework` v1.0.0-beta.10
  - `Azure.AI.OpenAI` v2.8.0-beta.1
- **Impact:** Prerelease APIs may change; no alert when GA versions ship.
- **Recommendation:** Add a comment in each `.csproj` noting these are prerelease, and configure Dependabot to track updates.

---

## 3. Medium Priority 🟡

### 3.1 No Structured Logging

- **Finding:** All scenarios use `Console.WriteLine` for output. No `ILogger` injection, no log levels.
- **Impact:** No integration with OpenTelemetry, Azure Monitor, or any log aggregator. The hosting adapter already supports OpenTelemetry, but app-level logs don't flow through it.
- **Recommendation:** Inject `ILogger<Program>` (or equivalent) and use structured log messages. At minimum: `LogInformation` for tool invocations, `LogWarning` for missing config, `LogError` for failures.

### 3.2 Fragile Markdown Parsing in Blazor UI

- **Finding:** `Analyze.razor` (Scenario 2) manually converts Markdown to HTML with string replacements.
- **Impact:** Breaks on complex Markdown (nested lists, tables, code blocks with special chars).
- **Recommendation:** Use `Markdig` NuGet package for proper Markdown-to-HTML conversion.

### 3.3 Magic Configuration Strings

- **Finding:** Environment variable names like `"AZURE_AI_FOUNDRY_PROJECT_ENDPOINT"` appear as inline strings across multiple files.
- **Impact:** Typos cause silent failures; no single source of truth for config keys.
- **Recommendation:** Extract to a shared constants class or use `IConfiguration` with strongly-typed options.

### 3.4 Deploy Scripts Could Be More Robust

- **Finding:**
  - No retry logic for `azd provision` (transient Azure failures).
  - Hardcoded CPU/memory values in deploy scripts.
  - ACR created with public network access enabled.
- **Recommendation:**
  - Add retry wrapper (1-2 retries with backoff) for `azd provision`.
  - Parameterize CPU/memory via environment variables with sensible defaults.
  - Document the ACR public access tradeoff; optionally add `--public-network-access disabled` flag.

---

## 4. Documentation Enhancements 📝

### 4.1 Inconsistent Scenario READMEs

| Element | Scenario 1 | Scenario 2 | Scenario 3 |
|---------|:----------:|:----------:|:----------:|
| "What You'll Learn" bullets | ❌ | ✅ | ✅ |
| Architecture diagram | ❌ | ✅ | ❌ |
| "How to Use" section | ❌ | ✅ | ❌ |
| Troubleshooting | ❌ | Partial | ❌ |

- **Recommendation:** Standardize all READMEs to a common template:
  ```
  # Scenario N: [Name]
  - Overview
  - What You'll Learn
  - Architecture (if multi-component)
  - Prerequisites
  - Quick Start (with expected output)
  - How to Use
  - Deploy to Azure
  - Troubleshooting
  - Clean Up
  ```

### 4.2 Missing `ARCHITECTURE.md`

- **Finding:** No single document explains the hosting adapter protocol, Responses API JSON schema, credential switching at runtime, or .NET Aspire service discovery.
- **Recommendation:** Create `docs/ARCHITECTURE.md` covering:
  - How the hosting adapter works (HTTP server, protocol translation)
  - Responses API endpoint schema (`/responses`)
  - Credential switching (local vs. container)
  - Aspire service discovery for Scenario 2

### 4.3 `test.http` Files Missing Deployed Examples

- **Finding:** All `test.http` files only have local (`localhost:8088`) requests. No examples for testing against a deployed Foundry agent.
- **Recommendation:** Add a "Deployed Testing" section to each `test.http` with:
  - Foundry endpoint URL placeholder
  - Required auth headers
  - Example requests matching the local ones

### 4.4 Missing Root-Level Community Files

- **Finding:** `CONTRIBUTING.md`, `SECURITY.md`, and `SUPPORT.md` exist only in `scenario-1-intro/`, not at the repo root.
- **Recommendation:** Add root-level versions (or symlinks) so GitHub surfaces them in the repo UI.

### 4.5 WIP Status Accuracy

- **Finding:** Scenarios 2 and 3 are marked `🚧 Work in Progress` but have substantial working code (75% and 40% respectively).
- **Recommendation:** Update status badges to reflect actual completeness, or add a progress indicator.

### 4.6 `docs/SCENARIOS.md` Linkage

- **Finding:** Proposes 3 additional scenarios (Code Metrics, Secret Scanner, Storyboard Generator) but isn't referenced from the root README and has no status indicators.
- **Recommendation:** Add a "Future Scenarios" link in root README; add status column (Proposed / Planned / In Development).

---

## 5. New Scenario Opportunities 🚀

From `docs/SCENARIOS.md` and gap analysis:

| Scenario | Description | Complexity | Value |
|----------|-------------|:----------:|:-----:|
| **Code Metrics Agent** | Paste code → get complexity, maintainability analysis | 🟢 Low | High — no external deps, pure computation |
| **Secret Scanner Agent** | Scan code for leaked credentials/API keys | 🟡 Medium | Very High — security demo |
| **Storyboard Generator** | Multi-modal agent: image + text generation | 🔴 High | High — showcases multi-modal capabilities |

- **Recommendation:** Code Metrics Agent is the best next candidate — low complexity, no external dependencies, demonstrates a new class of server-side computation.

---

## 6. Infrastructure Additions 🏗️

| Item | Current | Recommended |
|------|---------|-------------|
| Dependabot | Not configured | Add `dependabot.yml` for NuGet + Docker |
| `.env.example` | Not present | Add per scenario with required env vars |
| `global setup script` | Per-scenario `setup.ps1` | Add root `setup-all.ps1` for full lab setup |
| Graceful shutdown | Not handled | Add `IHostApplicationLifetime` shutdown hooks |
| Rate limiting awareness | Not present | Document Azure OpenAI quota limits; add backoff guidance |

---

## Maturity Scorecard

| Aspect | Current | Target | Gap |
|--------|:-------:|:------:|:---:|
| Code Quality | 8/10 | 9/10 | Error handling, null safety |
| Package Health | 7/10 | 9/10 | Pin versions, track prerelease→GA |
| Documentation | 8/10 | 9/10 | Consistency, ARCHITECTURE.md |
| Test Coverage | 0/10 | 7/10 | Unit tests for tool functions |
| CI/CD | 2/10 | 8/10 | Build + test + deploy pipeline |
| Security | 8/10 | 9/10 | Root SECURITY.md, ACR access |
| Docker | 6/10 | 8/10 | .dockerignore, HEALTHCHECK |
| DevEx | 7/10 | 9/10 | Troubleshooting, .env.example |
| **Overall** | **7.5/10** | **8.5/10** | |

---

## Implementation Order (Suggested)

1. **Phase 1 — Foundation:** CI/CD pipeline + test projects + pin package versions
2. **Phase 2 — Hardening:** Error handling + .dockerignore + HEALTHCHECK + structured logging
3. **Phase 3 — Documentation:** Standardize READMEs + ARCHITECTURE.md + test.http deployed examples
4. **Phase 4 — Polish:** .env.example + Dependabot + root community files + WIP status updates
5. **Phase 5 — Growth:** New scenario (Code Metrics Agent)
