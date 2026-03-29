# User Testing Readiness Checklist

## Objective
- [ ] Project is fully ready for internal user testing with repeatable setup, stable core workflows, and clear known limits.

## A) Environment and Bootstrap Reliability
- [x] Docker dependencies start with pgvector-enabled Postgres.
- [x] Port conflict guidance documented in `README.md`.
- [x] Add one-command bootstrap script — `bootstrap.ps1` (Docker + EF migrations + 3 service jobs).
- [x] Add one-command teardown/reset script — `reset.ps1` (soft/data-only/full with -Force flag).
- [ ] Add deterministic dependency health check script.
- [ ] Verify bootstrap on a clean machine profile in <= 15 minutes.

## B) Admin Workflow Completion

### Sources
- [x] Add-source form and backend validation implemented.
- [x] Root-path requirement hint shown in UI.
- [ ] Add explicit inline validation message mapping (absolute/exists/accessible) near fields.
- [ ] Add regression tests for source add failure cases from UI handler path.
- [ ] Verify source edit/disable/delete status messaging consistency.

### Users
- [x] Users page can create users with role assignment.
- [x] Duplicate username/email checks implemented.
- [x] Role access policy test coverage added.
- [x] Create-user workflow integration tests added.
- [x] Add enable/disable user action.
- [x] Add password reset/change action.
- [ ] Add user management audit log events.

## C) API and Worker End-to-End Confidence
- [x] Initial integration path exists (retry/reprocess/source flow).
- [ ] Add API endpoint integration tests for Sources/Documents/Jobs controller routes.
- [ ] Add ingestion processing tests for `ProcessPendingJobsAsync` success/failure transitions.
- [ ] Add document persistence tests (versions/chunks/events expectations).
- [ ] Add negative-path API tests (not found/validation failures).

## D) Authorization and Security Validation
- [x] Page-level role policy tests for Sources/Users added.
- [x] API controllers now require authentication (`Basic` auth against `ApiClients`), with `/api/v1/health` left anonymous.
- [ ] Add behavior-level authorization tests (restricted handlers/actions).
- [ ] Add unauthorized/forbidden path tests.
- [ ] Verify dev admin seed role assignment with automated test.

## E) User Test Ops and Observability
- [x] Create `Docs/user-testing-runbook.md` with:
  : test accounts (admin seed user documented)
  : sample source folder setup (step-by-step instructions)
  : expected outcomes and timing (~30 min end-to-end)
- [x] Add troubleshooting guide for common setup/runtime failures.
- [x] Add log collection instructions for bug reports (via terminal logs).
- [x] Create `bootstrap.ps1` one-command setup script (Docker + migrations + service startup).

## F) Release Gate
- [x] Local tests green (`dotnet test`). — 23/23 tests passing.
- [ ] CI test run green on main branch.
- [x] Fresh DB smoke pass complete:
  : ✅ login (admin / Admin#12345 → Dashboard 200)
  : ✅ Sources page (200, table present)
  : ✅ Documents page (200, table present)
  : ✅ Users page (200, admin user visible)
  : ✅ Jobs page (200, placeholder with no data)
  : ✅ Audit page (200)
  : ✅ Dashboard (200, stat counters)
  : ✅ API health (200 ok)
  : ✅ Worker running (polling IngestionJobs, DB queries visible in logs)
  : ✅ API endpoints now return 401 without credentials (`sources`, `documents`, `jobs`, `search` verified)
- [ ] No blocker defects open for core flows.
- [ ] README + runbook validated by a second person.
- [ ] Remaining issues triaged into blocker vs post-test backlog.

### Known Issues Found in Smoke Pass
- **Jobs page**: Shows placeholder text only — no job table. Pending implementation.
- **Audit page**: Renders but content TBC (may be placeholder).
- **`/api/v1/health/dependencies`**: Returns `degraded` with `not-checked` for all deps. Health probes not yet wired to Postgres/Tika/Ollama.

## Current Focus
- [x] User enable/disable + password reset workflows completed (5 new tests, all passing).
- [x] User-testing-runbook.md created with 7-phase workflow guide (~30 min end-to-end).
- [x] bootstrap.ps1 script created for one-command setup (Docker + migrations + 3 service jobs).
- [x] Fresh DB smoke pass completed — 9/9 pages/endpoints responding; 3 known issues filed above.
- [x] reset.ps1 created — supports -SoftReset / -DataOnly / -CleanBuilds / -Force flags.
- [x] API controller authorization enforced — unauthenticated requests now receive 401 while health stays anonymous.
- [ ] **Next slice (Priority 1):** Add authorization behavior/unauthorized-path tests for the API.
- [ ] **Then:** Wire health dependency probes (Postgres/Tika/Ollama) to `/api/v1/health/dependencies`.
- [ ] **After:** Document persistence tests + user audit log events.
