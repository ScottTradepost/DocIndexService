# DocIndexService User Testing Runbook

**Duration:** ~30 minutes | **Prerequisites:** Windows, Docker Desktop, .NET 8 SDK

## Phase 0: Bootstrap (5 min)

### 0.1 Start dependencies

From repo root:
```powershell
Copy-Item .\deploy\docker\.env.sample .\deploy\docker\.env -Force
docker compose --env-file .\deploy\docker\.env -f .\deploy\docker\docker-compose.yml up -d
```

**What this does:** Starts PostgreSQL (port 5433), Apache Tika (9998), and Ollama (11434) in Docker.

**Wait for ready state:**
```powershell
# Check logs for "database system is ready to accept connections"
docker compose -f .\deploy\docker\docker-compose.yml logs postgres | Select-String "ready"
```

### 0.2 Apply database migrations

```powershell
dotnet ef database update --project .\src\DocIndexService.Infrastructure\DocIndexService.Infrastructure.csproj --startup-project .\src\DocIndexService.Api\DocIndexService.Api.csproj
```

**Expected output:** `Done!` and no errors. Tables and seed data for dev admin user created.

### 0.3 Start application hosts

Open **three separate terminals** in VS Code and run each:

**Terminal 1 - API:**
```powershell
dotnet run --project .\src\DocIndexService.Api\DocIndexService.Api.csproj
```
**Expected:** `Listening on http://localhost:5166` (or similar)

**Terminal 2 - Admin UI:**
```powershell
dotnet run --project .\src\DocIndexService.Admin\DocIndexService.Admin.csproj
```
**Expected:** `Listening on http://localhost:5170` (or similar)

**Terminal 3 - Worker:**
```powershell
dotnet run --project .\src\DocIndexService.Worker\DocIndexService.Worker.csproj
```
**Expected:** `Scan worker started with interval 15 minutes` — **Worker runs continuously in background.**

---

## Phase 1: Login (2 min)

1. Open browser: **http://localhost:5170**
2. You'll see the login page.
3. Enter credentials:
   - **Username:** `admin`
   - **Password:** `Admin#12345`
4. Click **Login**.
5. **Expected result:** Redirected to Dashboard showing stats (Sources, Documents, Jobs, Users counters).

---

## Phase 2: Add Document Source (3 min)

**Goal:** Create a source pointing to a real folder with documents.

### 2.1 Prepare test folder

On your machine, create a folder with sample files:
```
C:\Users\YourName\TestDocs\
  ├── sample1.txt
  ├── sample2.pdf
  └── subfolder\
      └── sample3.docx
```

(You can use **any** existing folder with .txt/.pdf/.docx files for this test.)

### 2.2 Add source in UI

1. Click **Sources** in left nav.
2. In the "Add Source" form:
   - **Name:** `TestSource` (or any name)
   - **Root Path:** `C:\Users\YourName\TestDocs` (your test folder path)
   - **Include Patterns:** `*` (match all files)
   - **Exclude Patterns:** (leave blank)
   - **Scan Interval:** `15` (minutes)
   - **Recursive:** ✓ (checked — include subfolders)
   - **Enabled:** ✓ (checked)
3. Click **Create Source**.

**Expected result:** Green confirm message: "Source 'TestSource' created!" appears, then page reloads and the source is visible in the list.

**Verify:** New row shows:
- Name: TestSource
- Path: C:\Users\YourName\TestDocs
- Status: Enabled

---

## Phase 3: Trigger Manual Scan (2 min)

1. In the Sources table, find your **TestSource** row.
2. Click the **Scan Now** button (right side of row).
3. **Expected result:** Success message: "Scan triggered for source 'TestSource'".

**What happens behind the scenes:**
- Worker picks up the scan job (may take 30–60 seconds).
- Tika extracts text from each file.
- Documents and chunks are persisted.
- Jobs status transitions: `Pending` → `Running` → `Completed`.

---

## Phase 4: View Jobs & Documents (5 min)

### 4.1 Check Jobs

1. Click **Jobs** in left nav.
2. **Expected:** A job row for your scan appears (may take 30–45 seconds):
   - Status: `Completed` (or `Running` if still in progress)
   - Source: TestSource
   - Type: `FullReconciliation` (full scan)
   - Documents Processed: (count of files found)

**If still running**, wait 30 seconds and refresh.

### 4.2 Check Documents

1. Click **Documents** in left nav.
2. **Expected:** Document rows appear for each file from your TestSource:
   - File: sample1.txt, sample2.pdf, sample3.docx, etc.
   - Status: `Indexed` (green badge)
   - Last Indexed: Timestamp of scan

**Verify content extraction:**
- Click on a document row → **Details** page shows:
  - Title (extracted or filename)
  - File size, extension
  - Chunks section (parsed text snippets)
  - Page count (if PDF)

---

## Phase 5: User Management Workflow (6 min)

### 5.1 Create a new user

1. Click **Users** in left nav.
2. In the "Add User" form:
   - **Username:** `testuser1`
   - **Email:** `testuser1@localhost`
   - **Password:** `TestPass#123` (min 8 chars)
   - **Enabled:** ✓ (checked)
   - **Roles:** Check `IndexManager`
3. Click **Create User**.

**Expected result:** Confirm message: "User 'testuser1' created." Table updates with new row.

### 5.2 Disable the user

1. In the Users table, find **testuser1** row.
2. Click the **Disable** button (red badge changes to gray "Disabled").
3. **Expected:** Status message: "User 'testuser1' has been disabled."
4. Confirm: Row now shows **Disabled** badge.

### 5.3 Re-enable the user

1. Click the **Enable** button.
2. **Expected:** Status message: "User 'testuser1' has been enabled."
3. Confirm: Row shows **Enabled** badge again.

### 5.4 Reset user password

1. Click the **Reset Pass** button.
2. A modal dialog opens: "Reset Password for testuser1"
3. Enter new password: `NewPassword#456`
4. Click **Reset Password**.

**Expected result:** Confirm message: "Password for user 'testuser1' has been reset."

**Verification:** Log out (click **Account** → **Logout**), then log back in as:
- **Username:** `testuser1`
- **Password:** `NewPassword#456` (the new password)
- **Expected:** Login succeeds and redirects to Dashboard.

---

## Phase 6: Audit Trail (2 min)

1. Click **Audit** in left nav.
2. **Expected:** Audit log entries for actions performed:
   - User creation
   - Source creation
   - Scan job execution
   - Document indexing
   - User enable/disable
   - Password reset
3. Each entry shows: Timestamp, Action, Resource, User, Status.

---

## Phase 7: API Verification (optional, 2 min)

Open browser: **http://localhost:5166/swagger**

**Expected:** Swagger UI showing available endpoints:
- GET `/api/v1/health` — Health check
- GET `/api/v1/sources` — List sources
- GET `/api/v1/documents` — List documents
- POST `/api/v1/documents/{id}/scan` — Trigger scan
- GET `/api/v1/jobs` — List jobs
- etc.

**Try one endpoint:**
1. Click **GET /api/v1/health**
2. Click **Try it out** → **Execute**
3. **Expected response:** `{ "status": "ok", "utcTimestamp": "2026-03-29T..." }`

---

## Smoke Pass Summary

✅ **All tests passed if:**
- [x] Logged in with admin account
- [x] Added a document source
- [x] Triggered a scan
- [x] Documents appear in Documents page
- [x] Jobs show completed status
- [x] Created a new user
- [x] Enable/disabled a user
- [x] Reset user password
- [x] Audit log recorded all actions
- [x] API health endpoint responds

---

## Troubleshooting

### "Postgres connection refused"
**Issue:** Docker container not running or port 5433 in use.  
**Fix:**
```powershell
docker compose -f .\deploy\docker\docker-compose.yml ps
# If postgres not listed, restart:
docker compose --env-file .\deploy\docker\.env -f .\deploy\docker\docker-compose.yml up -d
```

### "Login fails: 'Invalid credentials'"
**Issue:** Typo in username/password or seed data not applied.  
**Fix:**
```powershell
# Verify seed data applied:
dotnet ef database update --project .\src\DocIndexService.Infrastructure\DocIndexService.Infrastructure.csproj --startup-project .\src\DocIndexService.Api\DocIndexService.Api.csproj
# Try login again with: admin / Admin#12345
```

### "Scan triggered but documents don't appear after 2 min"
**Issue:** Worker may be hung or Tika failing to extract text.  
**Fix:**
```powershell
# Check worker terminal for errors
# Restart worker: Ctrl+C in worker terminal, then re-run
# Check Tika is healthy:
Invoke-WebRequest http://localhost:9998/version
```

### "Users page shows 'Access Denied'"
**Issue:** Logged-in account doesn't have SystemAdmin role.  
**Fix:** Log out and back in with `admin` account (default has SystemAdmin role).

### "Source path "C:\Users\..." is rejected as invalid"
**Issue:** Path must be absolute Windows path (not relative or network path).  
**Fix:** Use full path like `C:\Users\YourName\TestDocs`, not `~\TestDocs` or `\\network\share`.

### "Documents not extracting text - chunks are empty"
**Issue:** Tika timeout or format not supported.  
**Fix:**
- Try .txt files first (guaranteed to work)
- Increase timeout in appsettings.json: `"Tika": { "TimeoutSeconds": 60 }`
- Check logs for `Tika extraction failed` warnings

---

## Known Limitations & Expected Behavior

- **Search:** Not fully implemented yet; use Documents page to browse.
- **AI Summarization:** Requires Ollama service; currently stubbed (placeholder responses).
- **User Roles:** Only `SystemAdmin` and `IndexManager` exist (add more as needed).
- **Concurrent Scans:** Worker processes one scan at a time (queue-based).
- **File Size Limit:** 50 MB per file (configurable in `appsettings.shared.json`).
- **Supported formats:** .txt, .pdf, .docx, .xlsx (via Tika).

---

## Post-Test Reset

To clean up and start fresh:

```powershell
# Stop all hosts (Ctrl+C in each terminal)
# Stop Docker:
docker compose -f .\deploy\docker\docker-compose.yml down
# Clean database:
docker volume rm docindexservice_postgres_data
# Clean test artifacts:
dotnet clean
```

Then re-run **Phase 0** to bootstrap fresh.

---

## Contact & Feedback

- Report issues in: `Docs/` folder as markdown notes
- Include: screenshots, browser console errors, worker terminal logs
- Ask questions in: Team chat or code review
