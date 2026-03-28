# DocIndexService Bootstrap Spec for Copilot

## Goal
Build the initial foundation for **DocIndexService**, a locally installable document indexing and AI document search platform for small businesses.

The system must:
- run locally on a client device or server
- use PostgreSQL as the main database
- support indexing local directories
- detect new, updated, and deleted files
- expose an API for search and document operations
- include a secure admin dashboard
- support future AI-assisted search, summarize, ask, similar, and extract features
- be structured for maintainability, testing, and phased growth

This first implementation should focus on a solid installable foundation, not advanced AI features.

---

## Architecture Overview

Projects to create:

- `DocIndexService.Api`
- `DocIndexService.Admin`
- `DocIndexService.Worker`
- `DocIndexService.Core`
- `DocIndexService.Application`
- `DocIndexService.Infrastructure`
- `DocIndexService.Contracts`
- `DocIndexService.Tests`

### Responsibilities

#### DocIndexService.Core
Contains:
- entities
- enums
- constants
- interfaces
- domain models
- basic shared types

#### DocIndexService.Application
Contains:
- application services
- orchestration logic
- use cases
- DTO mapping
- validation logic
- job coordination interfaces

#### DocIndexService.Infrastructure
Contains:
- PostgreSQL access and EF Core configuration
- pgvector integration
- file system scanning services
- Apache Tika client
- Ollama client
- authentication implementation
- repositories
- background job persistence
- logging and audit persistence

#### DocIndexService.Contracts
Contains:
- request/response DTOs
- API contracts
- shared DTOs between Api and Admin

#### DocIndexService.Api
Contains:
- REST endpoints for document search and operations
- health endpoints
- authentication endpoints if needed
- Swagger/OpenAPI

#### DocIndexService.Admin
Contains:
- secure admin UI
- dashboard pages
- source directory management
- job monitoring
- document review screens
- user management screens

#### DocIndexService.Worker
Contains:
- scheduled scanning
- reconciliation of new/updated/deleted files
- extraction pipeline
- chunking pipeline
- embedding pipeline
- indexing job processing

#### DocIndexService.Tests
Contains:
- unit tests for application services
- integration tests for database-backed workflows
- initial test coverage for scanning and sync behavior

---

## Initial Technical Stack

Use:
- .NET 8
- ASP.NET Core Web API
- ASP.NET Core admin web app or Blazor Server for Admin
- Entity Framework Core
- PostgreSQL
- pgvector for embeddings
- Docker Compose for local infrastructure
- Apache Tika server container for text extraction
- Ollama for embeddings and future summarization/QA
- Serilog for structured logging
- FluentValidation for request validation
- xUnit for tests

Do not introduce unnecessary complexity.
Do not add event buses or microservices.
Do not add message brokers in phase 1.
Use a modular monolith structure split into separate projects.

---

## Initial Functional Scope

Implement phase 1 foundations only.

### Must include
- secure admin login
- role-based auth for admin area
- PostgreSQL persistence
- document source directory configuration
- recursive scan support
- include/exclude pattern support
- scheduled incremental scans
- nightly full reconciliation
- detection of new files
- detection of updated files
- detection of deleted files
- persistence of document metadata
- persistence of ingestion jobs and job history
- placeholder extraction pipeline that is fully structured
- initial Tika integration interface and service
- initial Ollama integration interface and service
- search endpoint shell with repository/service structure in place
- dashboard showing counts and status
- document list page
- document detail page
- ingestion job status page
- retry failed job action
- manual scan action
- audit log records for admin operations

### Can be stubbed initially if structure is present
- real chunking implementation
- real embedding generation
- real semantic search
- real OCR
- real summarization
- real extraction templates

---

## Security Requirements

Admin side must include:
- login page
- local user authentication
- password hashing
- role checks
- anti-forgery where appropriate
- session timeout or token expiration
- audit log of admin actions

Initial roles:
- `SystemAdmin`
- `IndexManager`
- `Reviewer`

Only `SystemAdmin` can manage users.
Only `SystemAdmin` and `IndexManager` can manage sources and reindex.
`Reviewer` can view status and documents only.

---

## Database Design

Create EF Core entities and migrations for these tables:

- `Users`
- `Roles`
- `UserRoles`
- `ApiClients`
- `DocumentSources`
- `Documents`
- `DocumentVersions`
- `DocumentChunks`
- `IngestionJobs`
- `IngestionJobEvents`
- `AuditLogs`
- `SystemSettings`

### DocumentSources
Fields should include:
- Id
- Name
- RootPath
- IsEnabled
- IsRecursive
- IncludePatterns
- ExcludePatterns
- ScanIntervalMinutes
- LastScanUtc
- LastSuccessfulScanUtc
- CreatedUtc
- UpdatedUtc

### Documents
Fields should include:
- Id
- SourceId
- RelativePath
- FullPath
- FileName
- Extension
- MimeType
- Sha256
- FileSize
- FileLastModifiedUtc
- Status
- IsDeleted
- Title
- Summary
- LastIndexedUtc
- CreatedUtc
- UpdatedUtc
- MetadataJson

### DocumentVersions
Fields should include:
- Id
- DocumentId
- VersionNumber
- Sha256
- FileLastModifiedUtc
- ExtractedTextPath
- CreatedUtc

### DocumentChunks
Fields should include:
- Id
- DocumentId
- ChunkIndex
- PageStart
- PageEnd
- Text
- TokenCount
- Embedding
- EmbeddingModel
- EmbeddingVersion
- MetadataJson

### IngestionJobs
Fields should include:
- Id
- SourceId
- DocumentId nullable
- JobType
- Status
- StartedUtc
- CompletedUtc nullable
- ErrorMessage nullable
- AttemptCount
- PayloadJson

### AuditLogs
Fields should include:
- Id
- UserId nullable
- ActionType
- EntityType
- EntityId
- DetailsJson
- CreatedUtc
- IpAddress nullable

---

## Initial Admin Screens

Create an admin UI with these pages:

### 1. Login
- username/email
- password
- validation messages

### 2. Dashboard
Show:
- total sources
- active sources
- total documents
- indexed documents
- failed jobs
- pending jobs
- deleted documents
- last scan time
- recent ingestion activity

### 3. Sources
- list sources
- add source
- edit source
- enable/disable source
- run scan now
- run full reindex
- test source path availability

### 4. Documents
- paged searchable list
- filter by source
- filter by status
- filter by deleted flag
- open details page

### 5. Document Details
- file metadata
- source
- current status
- version history
- extracted text preview placeholder
- last indexed time
- actions: reprocess, mark ignored, open file if available

### 6. Jobs
- list jobs
- filter by status/type/source/date
- view failure reason
- retry job

### 7. Audit Log
- paged list
- filter by action/entity/date

### 8. Users
- list users
- create user
- change role
- disable user

---

## Initial API Endpoints

Create versioned REST endpoints under `/api/v1`.

### Health
- `GET /api/v1/health`
- `GET /api/v1/health/dependencies`

### Sources
- `GET /api/v1/sources`
- `GET /api/v1/sources/{id}`
- `POST /api/v1/sources`
- `PUT /api/v1/sources/{id}`
- `POST /api/v1/sources/{id}/scan`
- `POST /api/v1/sources/{id}/reindex`

### Documents
- `GET /api/v1/documents`
- `GET /api/v1/documents/{id}`
- `POST /api/v1/documents/{id}/reprocess`

### Jobs
- `GET /api/v1/jobs`
- `GET /api/v1/jobs/{id}`
- `POST /api/v1/jobs/{id}/retry`

### Search
Implement the contract and controller shell even if internals are stubbed initially:
- `POST /api/v1/search`
- `POST /api/v1/search/similar`
- `POST /api/v1/ask`
- `POST /api/v1/summarize`
- `POST /api/v1/extract`

These endpoints should return structured placeholder responses where full logic is not ready yet.

---

## File Sync Behavior

Implement a source sync service that:
- scans source directories
- computes identity using path plus file hash
- detects new files
- detects updated files
- detects deleted files
- creates ingestion jobs accordingly

Use:
- scheduled incremental scan every configurable interval
- full reconciliation service callable manually
- clean abstractions for file enumeration and fingerprinting

Do not depend exclusively on file system watchers.
Watchers may be added later, but scheduled scans are the source of truth.

---

## Extraction and Indexing Pipeline Design

Create the pipeline structure now even if some pieces are placeholders.

Pipeline stages:
1. Discover file
2. Create/update document record
3. Create ingestion job
4. Extract text via extraction service
5. Normalize text
6. Chunk text
7. Generate embeddings
8. Persist chunks and vectors
9. Update document status and timestamps

Define interfaces for:
- `IFileScanner`
- `IFileFingerprintService`
- `ITextExtractionService`
- `IChunkingService`
- `IEmbeddingService`
- `IDocumentIndexService`
- `IIngestionCoordinator`

Implement placeholder services where needed, but wire the full flow.

---

## Search Design

Create the contract and service layer for:
- keyword search
- hybrid search
- similar document search
- grounded ask
- summarize
- structured extract

For this first implementation:
- keyword search may be implemented first against metadata and extracted text fields
- semantic/hybrid logic may be stubbed or partially implemented
- all responses must be structured and include source references when available

Search response should support:
- document id
- document title
- file path/reference
- score
- snippet
- summary
- page range if known

---

## Configuration

Use strongly typed options for:
- PostgreSQL connection
- Tika endpoint
- Ollama endpoint
- scan intervals
- file size limits
- allowed extensions
- extraction settings
- security settings

Support configuration from:
- appsettings.json
- appsettings.Development.json
- environment variables

---

## Docker and Local Dev Setup

Create a `docker-compose.yml` under `deploy/docker` that includes:
- postgres
- tika
- ollama

Also create:
- sample `.env`
- setup instructions in `README.md`
- initial migration instructions
- how to run Admin, Api, and Worker locally

Do not containerize every .NET app immediately unless helpful. It is acceptable for local dev to run the .NET apps directly while infra runs in Docker.

---

## Coding Standards

- use clean namespaces
- use async APIs appropriately
- use dependency injection throughout
- use cancellation tokens in background services and IO-heavy operations
- prefer explicit DTOs over anonymous payloads
- keep controllers thin
- keep domain entities focused
- log key actions and failures
- add XML comments only where genuinely useful
- avoid unnecessary abstraction layers beyond what supports phase 1

---

## Testing Requirements

Create initial tests for:
- source create/update validation
- file scan new/update/delete detection
- ingestion job creation
- role-based access checks
- document metadata persistence

Use xUnit.
Use integration tests for database-backed workflows where appropriate.

---

## Deliverables for Initial Copilot Pass

Generate the following:

1. Solution and project structure
2. Project references wired correctly
3. Core entities and enums
4. EF Core DbContext and entity configurations
5. Initial migration
6. Authentication and authorization foundation
7. Admin UI shell with navigation and pages listed above
8. API controllers and DTOs
9. Worker service with scheduled scan loop
10. File scanning and fingerprint service interfaces plus initial implementation
11. Tika and Ollama client abstractions
12. Search contracts and placeholder search service
13. Logging configuration
14. Docker compose for dependencies
15. README with setup steps
16. Seed admin user option for development

---

## Important Constraints

- This is an installable local business system, not a demo chatbot
- The admin dashboard is a core requirement
- PostgreSQL is the default database and should be used
- The code should be maintainable and production-oriented
- Build for realistic small-business deployment
- Keep phase 1 practical and grounded

---

## Instruction to Copilot

Create the full initial solution and codebase structure for the system described above.
Do not collapse everything into one project.
Do not skip the admin dashboard.
Do not replace PostgreSQL.
Do not overengineer with unnecessary distributed architecture.
Build the foundation cleanly so later phases can add richer extraction, semantic search, grounded answers, and structured document intelligence.
