# Work Routing

How to decide who handles what.

## Routing Table

| Work Type | Route To | Examples |
|-----------|----------|----------|
| C# agent code, function tools, .NET Aspire | Dallas | Implement agents, add tools, configure Aspire AppHost |
| Blazor UI, Razor components, frontend | Lambert | Build pages, components, agent service integration |
| Azure, Docker, deployment, CI/CD | Parker | setup.ps1, deploy.ps1, Dockerfiles, infra/ templates |
| READMEs, docs, learning content | Brett | Scenario guides, CHANGELOG, test.http annotations |
| Code review | Ripley | Review PRs, check quality, suggest improvements |
| Architecture & design | Ripley | Cross-scenario patterns, technology choices |
| Scope & priorities | Ripley | What to build next, trade-offs, triage |
| Session logging | Scribe | Automatic — never needs routing |
| Work monitoring | Ralph | Backlog tracking, issue status, CI checks |

## Issue Routing

| Label | Action | Who |
|-------|--------|-----|
| `squad` | Triage: analyze issue, assign `squad:{member}` label | Ripley |
| `squad:ripley` | Architecture/scope/review work | Ripley |
| `squad:dallas` | Backend C# agent implementation | Dallas |
| `squad:lambert` | Blazor frontend work | Lambert |
| `squad:parker` | Infrastructure/deployment work | Parker |
| `squad:brett` | Documentation/learning content | Brett |

### How Issue Assignment Works

1. When a GitHub issue gets the `squad` label, the **Lead** triages it — analyzing content, assigning the right `squad:{member}` label, and commenting with triage notes.
2. When a `squad:{member}` label is applied, that member picks up the issue in their next session.
3. Members can reassign by removing their label and adding another member's label.
4. The `squad` label is the "inbox" — untriaged issues waiting for Lead review.

## Rules

1. **Eager by default** — spawn all agents who could usefully start work, including anticipatory downstream work.
2. **Scribe always runs** after substantial work, always as `mode: "background"`. Never blocks.
3. **Quick facts → coordinator answers directly.** Don't spawn an agent for "what port does the server run on?"
4. **When two agents could handle it**, pick the one whose domain is the primary concern.
5. **"Team, ..." → fan-out.** Spawn all relevant agents in parallel as `mode: "background"`.
6. **Anticipate downstream work.** If a feature is being built, spawn the tester to write test cases from requirements simultaneously.
7. **Issue-labeled work** — when a `squad:{member}` label is applied to an issue, route to that member. The Lead handles all `squad` (base label) triage.
