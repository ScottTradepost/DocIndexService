# DocIndexService Phase 1 Plan

## Objective
Deliver a practical, installable phase 1 foundation for DocIndexService that can:
- run locally
- index configured folders
- detect new, changed, and deleted files
- store document metadata in PostgreSQL
- provide a secure admin dashboard
- expose API endpoints for future integration
- prepare the system for later semantic search and grounded AI features

## Phase 1 Scope

### Included
- solution and project structure
- PostgreSQL setup with EF Core
- admin authentication and roles
- admin dashboard shell
- document source management
- incremental scan workflow
- full reconciliation workflow
- ingestion jobs and job history
- file fingerprinting
- placeholder extraction/indexing pipeline
- Tika client abstraction
- Ollama client abstraction
- document list and detail pages
- jobs page
- audit log page
- API shells for health, sources, documents, jobs, and search
- Docker Compose for dependencies

### Deferred
- advanced OCR
- full semantic search quality tuning
- production-grade chunk ranking
- extraction templates
- workflow automation rules
- multi-tenant remote management
- end-user search portal
- AD/LDAP integration

## Milestones

### Milestone 1: Solution Bootstrap
Create:
- solution file
- all phase 1 projects
- project references
- shared configuration
- logging foundation
- base README

Definition of done:
- solution builds
- apps start
- shared settings load correctly

### Milestone 2: Domain and Persistence
Create:
- entities
- enums
- DbContext
- EF configurations
- migrations
- seed hooks for development admin user

Definition of done:
- database can be created and migrated
- key entities exist
- base test coverage exists for persistence

### Milestone 3: Admin Security and Shell
Create:
- login
- auth plumbing
- role authorization
- admin layout and navigation
- base dashboard page
- users page shell

Definition of done:
- login works
- role restrictions work
- unauthorized users are blocked

### Milestone 4: Source Management
Create:
- source CRUD
- path validation
- include/exclude pattern handling
- enable/disable source flow
- manual scan trigger

Definition of done:
- admin can manage watched folders
- source settings persist correctly

### Milestone 5: File Sync and Jobs
Create:
- scanner
- fingerprinting
- new/changed/deleted detection
- job creation
- job event logging
- retry flow
- nightly reconciliation hook

Definition of done:
- system detects file changes reliably
- jobs are persisted and visible in UI

### Milestone 6: Extraction Pipeline Skeleton
Create:
- extraction coordinator
- Tika abstraction
- normalization placeholder
- chunking placeholder
- embedding placeholder
- status updates and error handling

Definition of done:
- pipeline executes end-to-end
- placeholder outputs are persisted
- failures are recorded cleanly

### Milestone 7: API Layer
Create:
- health endpoints
- sources endpoints
- documents endpoints
- jobs endpoints
- search contract endpoints

Definition of done:
- Swagger loads
- endpoints return structured responses
- app services are used cleanly

### Milestone 8: Hardening and Dev Setup
Create:
- Docker Compose for Postgres, Tika, Ollama
- sample environment file
- setup docs
- integration tests for core workflows

Definition of done:
- local setup works from docs
- basic tests pass
- developers can stand up the environment quickly

## Suggested First Build Order
1. solution structure
2. Core/Application/Infrastructure foundations
3. DbContext + entities + migration
4. Admin auth + layout
5. Source CRUD
6. Worker scheduled scan loop
7. Job persistence and monitoring
8. Extraction/indexing interfaces
9. API controllers
10. Docker/dev setup
11. cleanup and tests

## Acceptance Criteria for Phase 1
- a local admin can log in
- a source directory can be added
- a scan can be triggered
- documents are detected and recorded
- changed and deleted files are tracked
- jobs are visible and retryable
- dashboard shows useful health/status info
- the codebase is ready for phase 2 semantic search work

## Risks to Watch
- too much logic in controllers
- weak separation between Admin, Api, and Worker
- overengineering the AI layer too early
- lack of audit trail
- trying to support too many file types immediately
- depending only on filesystem watchers
- poor dev setup documentation

## Recommended Commit Strategy
- one milestone per branch or commit series
- keep migrations reviewed carefully
- run build after every major Copilot generation step
- commit after each stable milestone
