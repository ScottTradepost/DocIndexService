# User Testing Readiness Plan

## Goal
Bring DocIndexService to a stable, testable state for internal user testing with reproducible setup, working admin workflows, and validated API/worker behavior.

## Current State Snapshot
- Setup/migrations/testing baseline is working.
- Docker dependencies are running with pgvector-enabled Postgres.
- Source add flow exists and requires an existing absolute path.
- User add flow has now been implemented in Admin Users page.
- Core test suites currently pass.

## Definition of Ready for User Testing
- New tester can clone repo, start dependencies, migrate DB, and open Admin/API without tribal knowledge.
- Core workflows are successful end-to-end:
  - Login
  - Add source
  - Trigger scan
  - Observe jobs/documents
  - Add user with role
- Regression suite covers critical auth, ingestion, and persistence behavior.
- Known limitations are documented and non-blocking.

## Workstreams

### 1) Environment and Bootstrap Reliability
- Normalize ports and environment guidance (`.env`, Postgres port conflicts).
- Add one-command local bootstrap script for:
  - Compose up
  - DB migrate
  - Optional app startup checks
- Add deterministic health checks for dependencies and app endpoints.

Acceptance criteria:
- Fresh machine bootstrap succeeds in under 15 minutes.
- Setup instructions run without manual correction.

### 2) Admin Workflow Completion
- Sources:
  - Improve UX messaging for path validation (absolute + exists + accessible).
  - Add edit/disable/delete success/error feedback consistency.
- Users:
  - Add create-user integration tests.
  - Add enable/disable and password reset actions.
  - Add duplicate username/email UX handling polish.

Acceptance criteria:
- Source add and user add work reliably in manual smoke tests.
- All user management actions produce clear success/error messages.

### 3) API and Worker End-to-End Confidence
- Add API integration tests for Sources/Documents/Jobs endpoints.
- Add ingestion processing tests that verify pending jobs become completed/failed appropriately.
- Add document persistence tests (document version/chunk/event persistence expectations).

Acceptance criteria:
- End-to-end ingestion path is exercised by automated tests.
- Retry/reprocess behavior is covered and deterministic.

### 4) Authorization and Security Validation
- Expand role access tests to cover both page-level and behavior-level authorization.
- Add negative-path tests for unauthorized access.
- Verify seeded admin and role assignment behavior for development.

Acceptance criteria:
- Unauthorized requests are rejected as expected.
- Role-restricted pages/actions are enforced in tests.

### 5) User Test Ops and Observability
- Add a user-testing runbook:
  - Test accounts
  - Sample source folder setup
  - Expected outcomes per workflow
- Capture logs and common failure troubleshooting guide.

Acceptance criteria:
- Non-developer tester can execute runbook and file actionable feedback.

## Proposed Execution Order
1. Finish automated coverage gaps (document persistence + API integration + user workflow tests).
2. Polish Admin source/user UX feedback.
3. Add bootstrap script + runbook.
4. Run full smoke + regression pass and freeze for user testing.

## Release Gate Checklist
- [ ] All core tests green in CI/local.
- [ ] Manual smoke pass completed on fresh DB.
- [ ] No blocker bugs in add source/add user/scan/jobs.
- [ ] README + runbook verified by a second person.
- [ ] Open issues triaged into blocker vs post-test backlog.
