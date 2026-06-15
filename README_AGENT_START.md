# Agent Start Instructions

## 1. Planning baseline

The planning pack is committed before implementation. The Project Lead / Product Owner then approves or revises architecture and product behavior in a separate commit.

## 2. Architecture approval

Read `AGENTS.md` and every file under `docs/`. Inspect the repository and the read-only `./FinanceTracker` reference. The Legacy Analyst supplies evidence; the Project Lead / Product Owner accepts behavior and updates the decision log, structure, behavior contract, commit plan, and backlog together.

## 3. Foundation

The Foundation Agent populates the existing `.NET 10` solution, creates the four production projects and five test projects, renames `global.json.template` to `global.json` after confirming the installed SDK, and adds the product `README.md`. Follow `docs/PROJECT_STRUCTURE.md` and normalize generated package versions before restore.

Validate locally:

```bash
dotnet restore FinanceAssistant.slnx
dotnet build FinanceAssistant.slnx --no-restore
dotnet test FinanceAssistant.slnx --no-build
dotnet format FinanceAssistant.slnx --verify-no-changes --no-restore
```

## 4. Delivery

Work through `docs/TASK_BACKLOG.md` in order. Every row has one accountable owner; slice owners deliver all affected layers, tests, and documentation in one focused commit. Use the Review & Commit Steward for the risk-based gates defined in `AGENTS.md` and `docs/COMMIT_PLAN.md`.
