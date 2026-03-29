# User Testing Readiness Checklist

## Objective
- [ ] Project is fully ready for internal user testing with repeatable setup, stable core workflows, and clear known limits.

## A) Environment and Bootstrap Reliability
- [x] Docker dependencies start with pgvector-enabled Postgres.
- [x] Port conflict guidance documented in `README.md`.
- [ ] Add one-command bootstrap script (`up deps + migrate + smoke checks`).
- [ ] Add one-command teardown/reset script.
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
- [ ] Add enable/disable user action.
- [ ] Add password reset/change action.
- [ ] Add user management audit log events.

## C) API and Worker End-to-End Confidence
- [x] Initial integration path exists (retry/reprocess/source flow).
- [ ] Add API endpoint integration tests for Sources/Documents/Jobs controller routes.
- [ ] Add ingestion processing tests for `ProcessPendingJobsAsync` success/failure transitions.
- [ ] Add document persistence tests (versions/chunks/events expectations).
- [ ] Add negative-path API tests (not found/validation failures).

## D) Authorization and Security Validation
- [x] Page-level role policy tests for Sources/Users added.
- [ ] Add behavior-level authorization tests (restricted handlers/actions).
- [ ] Add unauthorized/forbidden path tests.
- [ ] Verify dev admin seed role assignment with automated test.

## E) User Test Ops and Observability
- [ ] Create `Docs/user-testing-runbook.md` with:
  : test accounts
  : sample source folder setup
  : expected outcomes and timing
- [ ] Add troubleshooting guide for common setup/runtime failures.
- [ ] Add log collection instructions for bug reports.

## F) Release Gate
- [x] Local tests green (`dotnet test`).
- [ ] CI test run green on main branch.
- [ ] Fresh DB smoke pass complete:
  : login
  : add source
  : trigger scan
  : jobs/documents visible
  : add user
- [ ] No blocker defects open for core flows.
- [ ] README + runbook validated by a second person.
- [ ] Remaining issues triaged into blocker vs post-test backlog.

## Current Focus
- [x] Next slice completed: user workflow integration tests.
- [ ] Next slice to execute: document persistence tests.
