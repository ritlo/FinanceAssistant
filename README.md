# FinanceAssistant

FinanceAssistant is a private, local-first personal finance application built with .NET 10 and Blazor Interactive Server. It keeps core finance workflows on your machine and makes an OpenAI-compatible assistant optional.

## Features

- Create, edit, categorize, and delete income and expense transactions.
- Review monthly income, expenses, net balance, and spending by category.
- Store notes and track payment reminders.
- Extract text from PDF and plain-text documents without retaining the original upload.
- Use an assistant-first interface with a local or explicitly approved remote model endpoint.
- Execute assistant reads immediately while requiring a typed preview and explicit confirmation for every assistant write.
- Persist application data locally in LiteDB for one server-resolved local profile.

The transaction, summary, note, reminder, and document workflows continue to work when no model endpoint is running.

## Requirements

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) (`global.json` targets `10.0.100` with latest-feature roll-forward)
- Optional: an OpenAI-compatible chat-completions endpoint, such as a local llama.cpp server

## Quick start

```bash
git clone https://github.com/ritlo/FinanceAssistant.git
cd FinanceAssistant
dotnet restore FinanceAssistant.slnx
dotnet run --project src/FinanceAssistant.Web/FinanceAssistant.Web.csproj
```

Open the local address printed by ASP.NET Core. The default assistant endpoint is `http://localhost:8080/v1/chat/completions`; the rest of the application does not require it.

## Configuration

Configuration lives under the `FinanceAssistant` section in `src/FinanceAssistant.Web/appsettings.json`. Override values with normal ASP.NET Core configuration providers, including environment variables and user secrets.

| Key | Default | Purpose |
|---|---|---|
| `FinanceAssistant:Currency` | `USD` | ISO 4217 currency used by all stored amounts |
| `FinanceAssistant:DatabasePath` | OS local application-data directory | LiteDB database location |
| `FinanceAssistant:DocumentTemporaryDirectoryPath` | Beside the database | Temporary document-processing directory |
| `FinanceAssistant:Assistant:Endpoint` | `http://localhost:8080/v1/chat/completions` | Initial OpenAI-compatible endpoint |
| `FinanceAssistant:Assistant:Model` | `local` | Model name sent to the endpoint |
| `FinanceAssistant:Assistant:ApiKey` | unset | Optional bearer token; keep it outside source control |
| `FinanceAssistant:Assistant:AllowRemote` | `false` | Allows non-loopback endpoints when explicitly enabled |

For example:

```bash
FinanceAssistant__Currency=EUR \
dotnet run --project src/FinanceAssistant.Web/FinanceAssistant.Web.csproj
```

The in-app Settings page stores assistant endpoint, port, remote-access permission, and write-proposal preference locally. Remote endpoints remain blocked until explicitly allowed and display a warning that financial or extracted document content may leave the machine.

The configured currency is bound to a database when it is created. Starting the application later with a different currency fails instead of reinterpreting existing amounts.

## Local data and safety

- The default database is `FinanceAssistant/FinanceAssistant.db` under the operating system's local application-data directory.
- PDF and plain-text uploads are limited to 10 MiB; PDFs are limited to 100 pages.
- Upload types and contents are validated, extracted text is treated as untrusted, and original files are deleted after parsing succeeds or fails.
- Assistant output cannot supply profile identity or write directly to persistence.
- Assistant proposals expire after 10 minutes and are persisted idempotently to prevent duplicate confirmed writes.
- Remote model access is opt-in. Core finance features remain local and deterministic.

FinanceAssistant is designed for a single local user. Do not expose the Web host publicly without adding an appropriate authentication and deployment boundary.

## Architecture

FinanceAssistant is a modular monolith hosted by one Blazor Interactive Server process:

```text
Web -> Application -> Domain
Web -> Infrastructure -> Application
Infrastructure -> Domain
```

- `FinanceAssistant.Domain` contains business entities, value objects, and invariants.
- `FinanceAssistant.Application` contains use cases and ports and depends only on Domain.
- `FinanceAssistant.Infrastructure` implements LiteDB, document parsing, local identity, clock, and model adapters.
- `FinanceAssistant.Web` composes the application and provides the Blazor UI.

The solution also contains focused Domain, Application, Infrastructure integration, Web, and architecture test projects.

## Development

Run the complete validation sequence from the repository root:

```bash
dotnet restore FinanceAssistant.slnx
dotnet build FinanceAssistant.slnx --no-restore
dotnet test FinanceAssistant.slnx --no-build
dotnet format FinanceAssistant.slnx --verify-no-changes --no-restore
git diff --check
```
