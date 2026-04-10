# Project Context

- **Owner:** Bruno Capuano
- **Project:** Microsoft Foundry Hosted Agents — .NET Learning Lab. Three scenarios teaching hosted agent development from beginner to advanced.
- **Stack:** .NET 10, C#, Microsoft Agent Framework, Blazor Server, .NET Aspire, Docker, Azure (Foundry, ACR), azd CLI
- **Created:** 2026-04-08

## Key Files

- `README.md` — Root repo overview
- `scenario-1-intro/README.md` — Scenario 1 walkthrough
- `scenario-2-data-crunch/README.md` — Scenario 2 (WIP)
- `scenario-3-image-gen/README.md` — Scenario 3 (WIP)
- `docs/PLAN.md` — Implementation plan
- `docs/SCENARIOS.md` — Scenario design catalog
- `scenario-1-intro/test.http` — Example API requests

## Learnings

- **Scenario 1 README must be scenario-specific, not boilerplate.** Rewritten with clear learning outcomes, architecture diagram, single function tool documentation, and troubleshooting focused on hosted agent patterns.
- **Template consistency matters for docs.** Scenario 2 established the gold standard structure (Prerequisites table, Quick Start numbered steps, Troubleshooting table, Clean Up, navigation). All three scenarios now follow this pattern.
- **test.http files serve both local and deployed workflows.** Added "Deployed Testing" sections to all three test.http files with placeholders for Foundry endpoint and token, making it clear how to test agents after deployment without guessing at the API contract.
