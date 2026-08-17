# FinanceAssistant

Local-first modular monolith for personal finance management built on .NET 10.

## Architecture

```
Domain ← Application ← Infrastructure ↗
                        ↘ Web ↗
```

- **Domain** — pure business concepts and invariants
- **Application** — use cases, request/result models, and ports
- **Infrastructure** — LiteDB, parsing, file handling, model adapters
- **Web** — Blazor Interactive Server, components, forms, display models

## Getting Started

```bash
dotnet restore FinanceAssistant.slnx
dotnet build FinanceAssistant.slnx
dotnet test FinanceAssistant.slnx
dotnet run --project src/FinanceAssistant.Web/FinanceAssistant.Web.csproj
```

## Development

Read `AGENTS.md` first for coding-agent rules and validation commands.

Use focused docs only when relevant:

- `docs/PROJECT_STRUCTURE.md` — architecture and project boundaries
- `docs/BEHAVIOR_CONTRACT.md` — accepted product behavior
- `docs/DECISION_LOG.md` — architecture, persistence, security, assistant, and model decisions
- `docs/TASK_BACKLOG.md` — active planned work
- `docs/REVIEW_CHECKLIST.md` — commit and review gates

Historical planning and retired agent prompts live under `docs/archive/` and are not default context.

## Invariants

- Single server-resolved local profile — no user IDs in commands or UI input
- Fresh LiteDB database on init — no legacy database import
- Positive `decimal` amounts, transaction type carries direction
- Deterministic category keyword rules with `Other` fallback
- Untrusted document content with hashing, cleanup, and no-original-retention
- Assistant reads execute immediately; writes require typed preview and user confirmation
