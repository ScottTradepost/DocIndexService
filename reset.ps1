#!/usr/bin/env pwsh

<#
.SYNOPSIS
    DocIndexService Reset — Tear down all services and optionally wipe data for a clean restart.
    
.DESCRIPTION
    Kills running service processes, stops Docker Compose, and optionally drops the Postgres
    volume so the next bootstrap starts with a completely fresh database.
    
    Use -DataOnly to preserve Docker containers but wipe just the database (faster).
    Use -SoftReset to kill only the app processes (API, Admin, Worker) without touching Docker.
    
.EXAMPLE
    # Full clean reset (stop everything, drop DB volume, ready for fresh bootstrap)
    .\reset.ps1
    
.EXAMPLE
    # Wipe DB data but keep containers running
    .\reset.ps1 -DataOnly
    
.EXAMPLE
    # Kill only app processes (restart code without touching DB or Docker)
    .\reset.ps1 -SoftReset
    
.EXAMPLE
    # Full reset + clean build artifacts (slowest, cleanest)
    .\reset.ps1 -CleanBuilds
    
.PARAMETER DataOnly
    Drop the Postgres data volume only. Does not stop Docker containers themselves.
    Faster than a full reset when you want a fresh DB quickly.
    
.PARAMETER SoftReset
    Kill app processes (API, Admin, Worker) only. Docker remains running.
    Useful when you've changed code and want to restart the hosts.
    
.PARAMETER CleanBuilds
    Also remove bin/obj build artifacts (triggers full recompile on next build).
    
.PARAMETER Force
    Skip confirmation prompts. Use in automation.
    
.NOTES
    Requires: Docker Desktop, .NET 8 SDK, PowerShell 5.1+
    After running reset.ps1, run .\bootstrap.ps1 to start fresh.
#>

[CmdletBinding()]
param(
    [switch] $DataOnly,
    [switch] $SoftReset,
    [switch] $CleanBuilds,
    [switch] $Force
)

$ErrorActionPreference = 'Stop'
$RepoRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $RepoRoot

Write-Host "`n=== DocIndexService Reset ===" -ForegroundColor Cyan

if ($DataOnly -and $SoftReset) {
    Write-Error "Cannot combine -DataOnly and -SoftReset. Choose one."
    exit 1
}

# ─────────────────────────────────────────────────────────────────────────────
# Phase 1: Confirm if destructive (full or DataOnly)
# ─────────────────────────────────────────────────────────────────────────────

if (-not $SoftReset -and -not $Force) {
    $mode = if ($DataOnly) { "DATA-ONLY (drops postgres volume, preserves containers)" }
            elseif ($CleanBuilds) { "FULL + BUILD CLEAN (stops Docker, drops volume, removes bin/obj)" }
            else { "FULL (stops Docker, drops postgres volume)" }
    
    Write-Host "`n  Mode: $mode" -ForegroundColor Yellow
    Write-Host "  Data WILL be permanently deleted." -ForegroundColor Red
    $confirm = Read-Host "`n  Type 'yes' to continue"
    
    if ($confirm.Trim().ToLower() -ne 'yes') {
        Write-Host "  Aborted." -ForegroundColor Yellow
        exit 0
    }
}

# ─────────────────────────────────────────────────────────────────────────────
# Phase 2: Kill app processes
# ─────────────────────────────────────────────────────────────────────────────

Write-Host "`n[1/4] Stopping application processes..." -ForegroundColor Yellow

$appNames = @('DocIndexService.Api', 'DocIndexService.Admin', 'DocIndexService.Worker')
$killed = 0
foreach ($name in $appNames) {
    $procs = Get-Process -Name $name -ErrorAction SilentlyContinue
    if ($procs) {
        $procs | Stop-Process -Force -ErrorAction SilentlyContinue
        $killed += $procs.Count
        Write-Host "  Killed: $name ($($procs.Count) process(es))" -ForegroundColor Green
    }
    else {
        Write-Verbose "  Not running: $name"
    }
}

# Also kill background PowerShell jobs for DocIndex services
Get-Job | Where-Object { $_.Name -match 'DocIndex' } | ForEach-Object {
    Stop-Job -Id $_.Id -ErrorAction SilentlyContinue
    Remove-Job -Id $_.Id -Force -ErrorAction SilentlyContinue
    Write-Host "  Removed PS job: $($_.Name)" -ForegroundColor Green
}

if ($killed -eq 0 -and -not (Get-Job | Where-Object { $_.Name -match 'DocIndex' })) {
    Write-Host "  No app processes were running." -ForegroundColor Gray
}

# Wait for OS to release file locks
if ($killed -gt 0) { Start-Sleep -Seconds 1 }

if ($SoftReset) {
    Write-Host "`n=== Soft Reset Complete ===" -ForegroundColor Cyan
    Write-Host "`nApp processes stopped. Run .\bootstrap.ps1 to restart."
    Write-Host ""
    exit 0
}

# ─────────────────────────────────────────────────────────────────────────────
# Phase 3: Drop Postgres data volume
# ─────────────────────────────────────────────────────────────────────────────

Write-Host "`n[2/4] Dropping database volume..." -ForegroundColor Yellow

$DockerPath = Join-Path $RepoRoot 'deploy' 'docker'
$ComposePath = Join-Path $DockerPath 'docker-compose.yml'

if ($DataOnly) {
    # Stop only postgres container to release the volume
    Write-Host "  Stopping postgres container..." -ForegroundColor Gray
    & docker compose -f $ComposePath stop postgres 2>&1 | Write-Verbose
    Start-Sleep -Seconds 2
    
    # Drop and recreate postgres volume
    Write-Host "  Dropping postgres volume..." -ForegroundColor Gray
    & docker compose -f $ComposePath rm -f postgres 2>&1 | Write-Verbose
    
    # Remove named volume
    & docker volume rm docindex_postgres_data 2>&1 | Write-Verbose
    & docker volume rm docindexservice_postgres_data 2>&1 | Write-Verbose
    
    # Restart postgres
    Write-Host "  Restarting postgres container..." -ForegroundColor Gray
    $EnvFile = Join-Path $DockerPath '.env'
    & docker compose --env-file $EnvFile -f $ComposePath up -d postgres 2>&1 | Write-Verbose
    
    Write-Host "  ✓ Postgres volume reset, container restarted" -ForegroundColor Green
}
else {
    # Full stop
    Write-Host "  Stopping all containers..." -ForegroundColor Gray
    & docker compose -f $ComposePath down -v 2>&1 | Write-Verbose
    
    if ($LASTEXITCODE -ne 0) {
        Write-Warning "docker compose down returned exit code $LASTEXITCODE — may have already been stopped"
    }
    
    Write-Host "  ✓ Containers stopped, volumes removed" -ForegroundColor Green
}

# ─────────────────────────────────────────────────────────────────────────────
# Phase 4: Clean build artifacts (optional)
# ─────────────────────────────────────────────────────────────────────────────

if ($CleanBuilds) {
    Write-Host "`n[3/4] Cleaning build artifacts..." -ForegroundColor Yellow
    
    $patterns = @(
        ".\src\*\bin",
        ".\src\*\obj",
        ".\tests\*\bin",
        ".\tests\*\obj"
    )
    
    foreach ($pattern in $patterns) {
        $dirs = Get-ChildItem -Path $RepoRoot -Filter (Split-Path -Leaf $pattern) -Recurse -Directory -ErrorAction SilentlyContinue |
                Where-Object { $_.FullName -like "*\$(Split-Path -Parent $pattern)\*" }
        foreach ($dir in $dirs) {
            Remove-Item -Recurse -Force $dir.FullName -ErrorAction SilentlyContinue
        }
    }
    
    # Also use dotnet clean for good measure
    & dotnet clean "$RepoRoot\DocIndexService.slnx" -q 2>&1 | Write-Verbose
    Write-Host "  ✓ Build artifacts cleaned" -ForegroundColor Green
}
else {
    Write-Host "`n[3/4] Skipping build artifacts (use -CleanBuilds to force)" -ForegroundColor Gray
}

# ─────────────────────────────────────────────────────────────────────────────
# Phase 5: Summary
# ─────────────────────────────────────────────────────────────────────────────

Write-Host "`n[4/4] Verifying cleanup..." -ForegroundColor Yellow

$runningContainers = & docker ps --filter "name=docindex" --format "{{.Names}}" 2>&1
if ($DataOnly -and $runningContainers -match 'postgres') {
    Write-Host "  ✓ Postgres container running with fresh volume" -ForegroundColor Green
}
elseif (-not $DataOnly) {
    if ($runningContainers) {
        Write-Warning "  Some docindex containers still running: $runningContainers"
    }
    else {
        Write-Host "  ✓ No docindex containers running" -ForegroundColor Green
    }
}

Write-Host "`n=== Reset Complete ===" -ForegroundColor Cyan
Write-Host ""

if ($DataOnly) {
    Write-Host "Postgres is fresh. Run migrations and start services:" -ForegroundColor Green
    Write-Host "  .\bootstrap.ps1 -SkipDocker"
}
else {
    Write-Host "Everything reset. Run bootstrap to start fresh:" -ForegroundColor Green
    Write-Host "  .\bootstrap.ps1"
}
Write-Host ""
