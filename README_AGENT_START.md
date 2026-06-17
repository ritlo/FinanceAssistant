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

Use `docs/IMPLEMENTATION_PLAN.md` for the atomic execution steps prepared for lightweight implementation models. Stop at the documented task boundary and commit before starting the next task.

## 5. Model routing and pi skills

Use `gpt-5.5` for planning, architecture and product decisions,
ambiguous requirements, risk reviews, and final acceptance. Use local model
`Qwen3.6-35B-A3B-UD-IQ3_XXS.gguf` for simple implementation, mechanical
edits, focused low-risk tests, formatting fixes, repository inspection
summaries, validation command runs, and low-risk documentation updates.

When `pi-dynamic-workflows` is available, delegate simple work to a workflow
subagent on Qwen, using either the configured small tier or exact model
`Qwen3.6-35B-A3B-UD-IQ3_XXS.gguf`. Keep `gpt-5.5` for the supervising plan,
review, or synthesis step.

If workflow subagent routing is unavailable, prompt the user to switch the
active Pi model before implementation or validation work with
`/model Qwen3.6-35B-A3B-UD-IQ3_XXS.gguf`. Prompt them to switch back with
`/model gpt-5.5` before planning, architecture, risk review, or acceptance.

Use built-in pi agent skills before escalating simple work to `gpt-5.5`:
brainstorming and planning skills for design, test-driven-development for
behavior changes, systematic-debugging for failures,
verification-before-completion before completion claims, requesting-code-review
or receiving-code-review for review loops, ast-grep and lsp-navigation for
precise code navigation, context-mode for large command output or file
analysis, and caveman skills for compressed status or commit work.

Escalate back to `gpt-5.5` when the task changes architecture, dependencies,
persistence schema, document security, assistant writes, product invariants, or
accepted behavior. Local-model work must still follow all repository
validation, staging, review, and commit rules.
