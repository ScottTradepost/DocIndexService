#!/usr/bin/env pwsh

<#
.SYNOPSIS
    DocIndexService Bootstrap — One-command setup for local development
    
.DESCRIPTION
    Performs complete Phase 0 setup: Docker Compose start, EF migrations, service host launch.
    
    Starts three background PowerShell jobs for API, Admin, Worker hosts.
    Outputs URLs and job IDs for manual monitoring.
    
.EXAMPLE
    .\bootstrap.ps1
    
.EXAMPLE
    .\bootstrap.ps1 -SkipDocker -RestartOnly
    (Assumes Docker already running; restarts only application hosts)
    
.PARAMETER SkipDocker
    Skip Docker Compose startup (use if containers already running).
    
.PARAMETER RestartOnly
    Kill existing app host processes and restart them (useful after code changes).
    
.PARAMETER NoJobs
    Run service hosts in foreground instead of background jobs (useful for debugging).
    
.NOTES
    Author: DocIndexService Team
    Requires: Docker Desktop, .NET 8 SDK, PowerShell 5.1+
    
    On failure, check: Docker Desktop running, port 5433/5166/5170 not in use, 
    appsettings files present, .env file copied to deploy/docker/.env
#>

[CmdletBinding()]
param(
    [switch] $SkipDocker,
    [switch] $RestartOnly,
    [switch] $NoJobs
)

$ErrorActionPreference = 'Stop'
$RepoRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $RepoRoot

Write-Host "`n=== DocIndexService Bootstrap ===" -ForegroundColor Cyan

# ─────────────────────────────────────────────────────────────────────────────
# Phase 0a: Kill existing processes (if restart)
# ─────────────────────────────────────────────────────────────────────────────

if ($RestartOnly) {
    Write-Host "`n[1/4] Killing existing service processes..." -ForegroundColor Yellow
    @(
        'DocIndexService.Api',
        'DocIndexService.Admin',
        'DocIndexService.Worker'
    ) | ForEach-Object {
        Write-Verbose "  Looking for: $_"
        Get-Process -Name $_ -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
    }
    Start-Sleep -Seconds 1
    Write-Host "  ✓ Processes terminated" -ForegroundColor Green
}

# ─────────────────────────────────────────────────────────────────────────────
# Phase 0b: Docker Compose (if not skipped)
# ─────────────────────────────────────────────────────────────────────────────

if (-not $SkipDocker) {
    Write-Host "`n[2/4] Starting Docker Compose dependencies..." -ForegroundColor Yellow
    
    # Point to docker folder
    $DockerPath = Join-Path $RepoRoot 'deploy' 'docker'
    $EnvFile = Join-Path $DockerPath '.env'
    $ComposePath = Join-Path $DockerPath 'docker-compose.yml'
    
    # Copy .env.sample if .env missing
    if (-not (Test-Path $EnvFile)) {
        $SampleEnv = Join-Path $DockerPath '.env.sample'
        if (Test-Path $SampleEnv) {
            Write-Host "  Copying .env.sample → .env" -ForegroundColor Gray
            Copy-Item $SampleEnv $EnvFile -Force
        }
    }
    
    # Start containers with env overlay
    Write-Host "  Running: docker compose up -d" -ForegroundColor Gray
    & docker compose --env-file $EnvFile -f $ComposePath up -d 2>&1 | Write-Verbose
    
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Docker Compose failed. Verify Docker Desktop is running."
        exit 1
    }
    
    Write-Host "  Waiting for postgres readiness..." -ForegroundColor Gray
    $maxAttempts = 30
    $attempt = 0
    while ($attempt -lt $maxAttempts) {
        $ready = & docker compose -f $ComposePath logs postgres 2>&1 | Select-String "ready to accept connections" -ErrorAction SilentlyContinue
        if ($ready) {
            Write-Host "  ✓ PostgreSQL ready" -ForegroundColor Green
            break
        }
        $attempt++
        Start-Sleep -Seconds 1
    }
    
    if ($attempt -eq $maxAttempts) {
        Write-Warning "Postgres did not report ready; continuing anyway..."
    }
}
else {
    Write-Host "`n[2/4] Skipping Docker (using existing containers)" -ForegroundColor Yellow
}

# ─────────────────────────────────────────────────────────────────────────────
# Phase 0c: Database Migrations
# ─────────────────────────────────────────────────────────────────────────────

Write-Host "`n[3/4] Applying database migrations..." -ForegroundColor Yellow

$InfraPath = Join-Path $RepoRoot 'src' 'DocIndexService.Infrastructure'
$ApiPath = Join-Path $RepoRoot 'src' 'DocIndexService.Api'

Write-Host "  Running: dotnet ef database update" -ForegroundColor Gray
& dotnet ef database update `
    --project $InfraPath `
    --startup-project $ApiPath `
    --verbose:$false 2>&1 |
    Where-Object { $_ -notmatch '^\s*$' -and $_ -notmatch '^Using project' } |
    ForEach-Object { Write-Verbose $_ }

if ($LASTEXITCODE -ne 0) {
    Write-Error "EF migration failed. Check: DB connection, appsettings.json, Postgres running."
    exit 1
}

Write-Host "  ✓ Migrations complete" -ForegroundColor Green

# ─────────────────────────────────────────────────────────────────────────────
# Phase 0d: Start Application Hosts
# ─────────────────────────────────────────────────────────────────────────────

Write-Host "`n[4/4] Starting application hosts..." -ForegroundColor Yellow

$ApiProjectPath = Join-Path $RepoRoot 'src' 'DocIndexService.Api'
$AdminProjectPath = Join-Path $RepoRoot 'src' 'DocIndexService.Admin'
$WorkerProjectPath = Join-Path $RepoRoot 'src' 'DocIndexService.Worker'

if ($NoJobs) {
    Write-Host "`n  ⚠ Running hosts in FOREGROUND (Ctrl+C to stop all)" -ForegroundColor Magenta
    Write-Host "`n  Select which host to run:" -ForegroundColor Cyan
    Write-Host "    1: API (http://localhost:5166)"
    Write-Host "    2: Admin UI (http://localhost:5170)"
    Write-Host "    3: Worker (background scans)"
    Write-Host "    4: All three"
    $choice = Read-Host "  Choice (1-4)"
    
    switch ($choice) {
        '1' { 
            Write-Host "`n  Starting API in foreground... (Ctrl+C to stop)" -ForegroundColor Green
            & dotnet run --project $ApiProjectPath
        }
        '2' { 
            Write-Host "`n  Starting Admin UI in foreground... (Ctrl+C to stop)" -ForegroundColor Green
            & dotnet run --project $AdminProjectPath
        }
        '3' { 
            Write-Host "`n  Starting Worker in foreground... (Ctrl+C to stop)" -ForegroundColor Green
            & dotnet run --project $WorkerProjectPath
        }
        '4' {
            Write-Error "Cannot run all three in foreground; use default (without -NoJobs)"
            exit 1
        }
        default {
            Write-Error "Invalid choice"
            exit 1
        }
    }
}
else {
    # Start as background jobs
    $jobs = @()
    
    $jobScript = {
        param($ProjectPath, $Name)
        $origLocation = Get-Location
        try {
            Set-Location $using:RepoRoot
            Write-Host "[JOB] Starting $Name..." -ForegroundColor Green
            & dotnet run --project $ProjectPath --no-launch-profile 2>&1 | Write-Host -ForegroundColor Gray
        }
        catch {
            Write-Host "[JOB] $Name failed: $_" -ForegroundColor Red
        }
        finally {
            Set-Location $origLocation
        }
    }
    
    # Start API
    Write-Host "  Starting API (port 5166)..." -ForegroundColor Green
    $apiJob = Start-Job -Name "DocIndexService.Api" -ScriptBlock $jobScript -ArgumentList @($ApiProjectPath, "API")
    $jobs += $apiJob
    
    # Start Admin
    Write-Host "  Starting Admin (port 5170)..." -ForegroundColor Green
    $adminJob = Start-Job -Name "DocIndexService.Admin" -ScriptBlock $jobScript -ArgumentList @($AdminProjectPath, "Admin")
    $jobs += $adminJob
    
    # Start Worker
    Write-Host "  Starting Worker (scans every 15 min)..." -ForegroundColor Green
    $workerJob = Start-Job -Name "DocIndexService.Worker" -ScriptBlock $jobScript -ArgumentList @($WorkerProjectPath, "Worker")
    $jobs += $workerJob
    
    # Wait for startup and output status
    Start-Sleep -Seconds 2
    Write-Host "`n  Job Status:" -ForegroundColor Cyan
    $jobs | ForEach-Object {
        $state = $_.State
        $color = if ($state -eq 'Running') { 'Green' } else { 'Yellow' }
        Write-Host "    [$($_.Name)] $state (ID: $($_.Id))" -ForegroundColor $color
    }
}

# ─────────────────────────────────────────────────────────────────────────────
# Output summary
# ─────────────────────────────────────────────────────────────────────────────

if (-not $NoJobs) {
    Write-Host "`n=== Bootstrap Complete ===" -ForegroundColor Cyan
    Write-Host "`nAccess URLs:" -ForegroundColor Green
    Write-Host "  Admin UI:        http://localhost:5170"
    Write-Host "  API Swagger:     http://localhost:5166/swagger"
    Write-Host "  API Health:      http://localhost:5166/health"
    
    Write-Host "`nBackground Jobs:" -ForegroundColor Green
    Write-Host "  API ID:          $($apiJob.Id)"
    Write-Host "  Admin ID:        $($adminJob.Id)"
    Write-Host "  Worker ID:       $($workerJob.Id)"
    
    Write-Host "`nJob Management:" -ForegroundColor Yellow
    Write-Host "  View output:     Get-Job -Id <ID> | Receive-Job -Keep"
    Write-Host "  Stop all:        Get-Job | Stop-Job"
    Write-Host "  Show all:        Get-Job"
    
    Write-Host "`nNext Steps:" -ForegroundColor Cyan
    Write-Host "  1. Open: http://localhost:5170"
    Write-Host "  2. Login: admin / Admin#12345"
    Write-Host "  3. Follow: Docs/user-testing-runbook.md"
    Write-Host ""
}
