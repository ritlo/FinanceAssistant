# Agent Start Instructions

## 1. Planning baseline

The planning pack is committed before implementation. The Project Lead Agent then approves or revises the architecture in a separate commit.

## 2. Architecture approval

Read `AGENTS.md` and every file under `docs/`. Inspect the repository and the read-only `./FinanceTracker` reference. Update the decision log, structure, behavior contract, commit plan, and backlog together.

## 3. Foundation

Populate the existing `.NET 10` solution, create the four production projects and five test projects, rename `global.json.template` to `global.json` after confirming the installed SDK, and add the product `README.md`. Follow `docs/PROJECT_STRUCTURE.md` and normalize generated package versions before restore.

Validate locally:

```bash
dotnet restore FinanceAssistant.slnx
dotnet build FinanceAssistant.slnx --no-restore
dotnet test FinanceAssistant.slnx --no-build
dotnet format FinanceAssistant.slnx --verify-no-changes --no-restore
```

## 4. Delivery

Work through `docs/TASK_BACKLOG.md` in vertical slices. Each slice includes its behavior, tests, documentation updates, and focused commit.
