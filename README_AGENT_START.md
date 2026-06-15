# Agent Start Instructions

Use this sequence to establish the repository before implementation begins.

## 1. Commit the planning pack

Review these files in the repository root:

```text
AGENTS.md
README_AGENT_START.md
Directory.Build.props
Directory.Packages.props
global.json.template
.editorconfig
.gitignore
docs/
```

Rename `global.json.template` to `global.json` only after confirming the installed .NET 10 SDK version.

Create the bootstrap commit before asking the Project Lead Agent to approve the architecture:

```bash
git status
git add AGENTS.md README_AGENT_START.md docs .editorconfig .gitignore Directory.Build.props Directory.Packages.props global.json.template

git commit -m "Add agent planning pack"
```

## 2. Run Project Lead Agent

Use the prompt from `docs/PROMPTS_FOR_AGENTS.md`.

The Project Lead Agent must approve or update the structure in a separate planning commit before implementation begins.

## 3. Create and validate the solution

Create the solution and production projects first. Add the test projects and CI workflow together so the first CI run performs both a real build and real tests.

```bash
dotnet build
dotnet test
```

## 4. Begin backlog

Start from `docs/TASK_BACKLOG.md`.

Each completed significant task must be committed.
