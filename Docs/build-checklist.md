# DocIndexService Build Checklist

## Repository Setup
- [ ] Create `/src`, `/tests`, `/docs`, `/deploy/docker`
- [x] Add bootstrap and planning docs to `/docs`
- [x] Create solution file
- [x] Create all initial projects
- [x] Wire project references
- [x] Add base README

## Solution Projects
- [x] DocIndexService.Api
- [x] DocIndexService.Admin
- [x] DocIndexService.Worker
- [x] DocIndexService.Core
- [x] DocIndexService.Application
- [x] DocIndexService.Infrastructure
- [x] DocIndexService.Contracts
- [x] DocIndexService.Tests

## Foundation
- [x] Shared configuration loading
- [x] Serilog configuration
- [x] Dependency injection setup
- [x] Environment-based config support
- [x] Base app settings files

## Domain and Persistence
- [x] Create entities
- [x] Create enums
- [x] Create DbContext
- [x] Create entity configurations
- [x] Configure PostgreSQL
- [x] Prepare pgvector column support
- [x] Add initial migration
- [x] Add development seed path

## Security
- [x] Implement login
- [x] Add password hashing
- [x] Add local user store
- [x] Add role-based authorization
- [x] Add SystemAdmin role
- [x] Add IndexManager role
- [x] Add Reviewer role
- [x] Protect admin pages
- [x] Add audit logging for admin actions

## Admin Dashboard
- [x] Login page
- [x] Admin layout
- [x] Dashboard page
- [x] Sources page
- [x] Documents page
- [ ] Document details page
- [x] Jobs page
- [x] Audit log page
- [x] Users page

## Source Management
- [x] Add source
- [x] Edit source
- [x] Disable source
- [x] Enable source
- [x] Validate source path
- [x] Manual scan action
- [x] Full reindex action
- [x] Include/exclude pattern support

## Worker and Sync
- [x] Scheduled incremental scan
- [x] Full reconciliation hook
- [x] New file detection
- [x] Updated file detection
- [x] Deleted file detection
- [x] File fingerprinting
- [x] Job creation
- [x] Job event logging
- [ ] Retry failed job support

## Extraction and Indexing
- [x] IFileScanner
- [x] IFileFingerprintService
- [x] ITextExtractionService
- [x] IChunkingService
- [x] IEmbeddingService
- [x] IDocumentIndexService
- [x] IIngestionCoordinator
- [x] Tika client abstraction
- [x] Ollama client abstraction
- [x] Placeholder chunking flow
- [x] Placeholder embedding flow

## API
- [x] Health endpoints
- [ ] Sources endpoints
- [ ] Documents endpoints
- [ ] Jobs endpoints
- [x] Search endpoints
- [x] Swagger/OpenAPI
- [x] Versioned route structure

## Dev Setup
- [ ] Docker Compose for Postgres
- [ ] Docker Compose for Tika
- [ ] Docker Compose for Ollama
- [ ] Sample environment file
- [ ] Setup instructions in README
- [ ] Migration instructions in README

## Tests
- [ ] Source validation tests
- [ ] File scan detection tests
- [ ] Ingestion job creation tests
- [ ] Role access tests
- [ ] Document persistence tests
- [ ] Basic integration test path

## Before Moving to Phase 2
- [x] Solution builds cleanly
- [ ] Database migration works
- [ ] Admin login works
- [ ] Source CRUD works
- [ ] Scan loop runs
- [ ] Jobs appear in UI
- [ ] API loads in Swagger
- [ ] Core docs are up to date
