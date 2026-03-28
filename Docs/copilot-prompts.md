# DocIndexService Copilot Prompts

Use these prompts in order. Keep `/docs/bootstrap-spec.md` in the repository and refer Copilot to it every time.

---

## Prompt 1 - Solution Bootstrap

Read `/docs/bootstrap-spec.md` and use it as the authoritative architecture guide for this repository.

Create the initial solution structure for DocIndexService as a modular monolith in .NET 8 with these projects:

- DocIndexService.Api
- DocIndexService.Admin
- DocIndexService.Worker
- DocIndexService.Core
- DocIndexService.Application
- DocIndexService.Infrastructure
- DocIndexService.Contracts
- DocIndexService.Tests

Requirements:
- wire all project references correctly
- use PostgreSQL with EF Core
- prepare for pgvector integration
- add clean folder structure within each project
- add base Program.cs / startup wiring for Api, Admin, and Worker
- add shared configuration loading
- add Serilog setup
- add placeholder README sections for setup and architecture
- do not implement advanced features yet
- do not collapse into a single project
- keep code maintainable and production-oriented

After creating the structure, summarize what was created and identify any TODO placeholders clearly in comments.

---

## Prompt 2 - Domain and Persistence

Using `/docs/bootstrap-spec.md` as the source of truth, implement the domain and persistence foundation.

Create:
- core entities
- enums
- DTO contracts
- EF Core DbContext
- entity configurations
- initial migration setup
- repository interfaces needed for phase 1

Include these tables/entities:
- Users
- Roles
- UserRoles
- ApiClients
- DocumentSources
- Documents
- DocumentVersions
- DocumentChunks
- IngestionJobs
- IngestionJobEvents
- AuditLogs
- SystemSettings

Requirements:
- use PostgreSQL conventions where appropriate
- prepare DocumentChunks for pgvector use
- include created/updated timestamps
- add sensible indexes
- keep controllers and UI out of this step
- keep the model clean and extensible
- explain any assumptions in code comments only where needed

---

## Prompt 3 - Admin Security and Shell

Using `/docs/bootstrap-spec.md`, implement the initial admin security and admin app shell.

Build:
- login page
- local user authentication
- password hashing
- role-based authorization for SystemAdmin, IndexManager, Reviewer
- protected admin layout
- navigation menu
- dashboard shell pages for:
  - Dashboard
  - Sources
  - Documents
  - Jobs
  - Audit Log
  - Users

Requirements:
- only SystemAdmin can manage users
- only SystemAdmin and IndexManager can manage indexing sources
- Reviewer is read-only
- add development seed support for an initial admin user
- keep the UI clean and functional, not flashy
- use server-side rendering or Blazor Server if already selected in the solution

---

## Prompt 4 - Source Management and Sync

Using `/docs/bootstrap-spec.md`, implement source directory management and the phase 1 sync pipeline.

Build:
- CRUD for DocumentSources
- source validation
- manual scan action
- scheduled incremental scan loop in Worker
- nightly reconciliation hook
- detection of new files
- detection of updated files
- detection of deleted files
- ingestion job creation for each needed operation

Requirements:
- do not rely only on file system watchers
- scheduled scans are the source of truth
- use path + hash/fingerprint logic
- add logging around scan activity
- persist job records and job events
- update dashboard statistics

---

## Prompt 5 - Extraction and Indexing Pipeline Skeleton

Implement the extraction and indexing pipeline structure from `/docs/bootstrap-spec.md`.

Create interfaces and initial implementations for:
- IFileScanner
- IFileFingerprintService
- ITextExtractionService
- IChunkingService
- IEmbeddingService
- IDocumentIndexService
- IIngestionCoordinator

Requirements:
- wire the pipeline end-to-end even if some pieces are placeholders
- integrate a Tika client abstraction
- integrate an Ollama client abstraction
- create placeholder chunking and embedding implementations where full behavior is not finished
- update document status and timestamps through the pipeline
- ensure failures are recorded in IngestionJobs and IngestionJobEvents

---

## Prompt 6 - API Layer

Implement the initial REST API from `/docs/bootstrap-spec.md`.

Create versioned endpoints under `/api/v1` for:
- health
- sources
- documents
- jobs
- search

Requirements:
- use request/response DTOs from Contracts
- keep controllers thin
- use Application services
- add Swagger/OpenAPI
- search endpoints may return structured placeholder results initially
- all endpoints must be production-oriented and ready for later expansion

---

## Prompt 7 - Docker and Local Dev Setup

Create the development infrastructure setup described in `/docs/bootstrap-spec.md`.

Add:
- docker-compose.yml under `/deploy/docker`
- postgres service
- tika service
- ollama service
- sample environment file
- README setup instructions
- migration instructions
- local run instructions for Api, Admin, and Worker

Requirements:
- make the setup practical for local development
- assume .NET apps may run directly on the host while dependencies run in Docker
- keep configuration clear and easy to change for client deployments later

---

## Prompt 8 - Guardrail Prompt

Do not invent a different architecture. Follow `/docs/bootstrap-spec.md` exactly unless there is a compile issue. If you need to deviate, explain the reason clearly in comments and in your summary.

This is an installable local business system with:
- PostgreSQL
- admin dashboard
- API
- worker
- document sources
- sync and job tracking

It is not a single-project demo and not a chatbot-first app.
