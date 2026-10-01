# CashFlow Pro — dev helpers for Windows 11 (PowerShell 5.1 / 7+)
# Debian counterpart: scripts/dev/profile.sh (same function names)
# Docs: scripts/dev/README.md
#
# Load it from your $PROFILE (open with: code $PROFILE):
#   . "C:\Users\rafae\Documents\repos\CashFlowPro\scripts\dev\profile.ps1"

# Repo root: CASHFLOW_ROOT wins, otherwise derive it from this file's location (scripts/dev/..).
$script:CfRoot = if ($env:CASHFLOW_ROOT) { $env:CASHFLOW_ROOT } else { (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path }
$script:CfAnalytics = Join-Path $CfRoot 'src\Analytics'
$script:CfCoreBanking = Join-Path $CfRoot 'src\CoreBanking'

# Run a script block inside a directory and always come back.
function Invoke-CfIn {
    param([string]$Path, [scriptblock]$Block)
    Push-Location $Path
    try { & $Block } finally { Pop-Location }
}

# ─── Navigation ──────────────────────────────────────────
function cf-root { Set-Location $CfRoot }
function cf-an   { Set-Location $CfAnalytics }
function cf-cb   { Set-Location $CfCoreBanking }

# ─── Infrastructure (Docker Compose) ─────────────────────
# Usage: cf-up            -> postgres, redis, rabbitmq only
#        cf-up -All       -> everything, including core-banking and analytics
function cf-up {
    param([switch]$All)
    Invoke-CfIn $CfRoot {
        if ($All) { docker compose up -d --build }
        else { docker compose up -d postgres redis rabbitmq }
    }
}
function cf-down { Invoke-CfIn $CfRoot { docker compose down } }
function cf-ps   { Invoke-CfIn $CfRoot { docker compose ps } }
# Run any docker compose command from the repo root, whatever the current folder.
# Usage: cf-dc ps | cf-dc up -d analytics | cf-dc config --quiet | cf-dc restart redis
function cf-dc {
    $dcArgs = $args
    Invoke-CfIn $CfRoot { docker compose @dcArgs }
}
# Usage: cf-logs [service]   (default: analytics)
function cf-logs {
    param([string]$Service = 'analytics')
    Invoke-CfIn $CfRoot { docker compose logs -f --tail 100 $Service }
}

# ─── Analytics (Java / Spring Boot) ──────────────────────
function cf-an-run   { Invoke-CfIn $CfAnalytics { .\mvnw.cmd spring-boot:run } }
function cf-an-build { Invoke-CfIn $CfAnalytics { .\mvnw.cmd clean package -DskipTests } }
# Usage: cf-an-test                      -> all tests
#        cf-an-test TransferEventTest    -> a single test class
# Failures are printed to the console (expected vs actual, line of the test), not hidden in target/surefire-reports.
function cf-an-test {
    param([string]$TestName)
    Invoke-CfIn $CfAnalytics {
        $mvnArgs = @('test', '-B', '-Dsurefire.useFile=false', '-DtrimStackTrace=true')
        if ($TestName) { $mvnArgs += "-Dtest=$TestName" }
        # Stop printing at "Total time:", which drops Maven's long trailing help text (the cause is printed above it).
        $done = $false
        $passed = $false
        $failed = New-Object System.Collections.Generic.List[string]
        .\mvnw.cmd @mvnArgs | ForEach-Object {
            if ($_ -match 'Total time:') { $done = $true }
            if ($done) { return }
            $_
            if ($_ -match 'BUILD SUCCESS') { $passed = $true }
            # Surefire failure line, e.g. "[ERROR]   TransferEventTest.myTest:39 » message"
            if ($_ -match '^\[ERROR\]\s{2,}(\S+\.\S+:\d+.*)$') { $failed.Add($Matches[1]) }
        }
        Write-Host ''
        if ($failed.Count -gt 0) {
            Write-Host "=== FAILED TESTS ($($failed.Count)) ===" -ForegroundColor Red
            foreach ($f in $failed) { Write-Host (' x ' + $f.Substring(0, [Math]::Min($f.Length, 160))) -ForegroundColor Red }
        }
        elseif ($passed) { Write-Host '=== ALL TESTS PASSED ===' -ForegroundColor Green }
        else { Write-Host '=== BUILD FAILED before the tests ran (compile error?) - read the output above ===' -ForegroundColor Yellow }
    }
}

# ─── Core Banking (.NET) ─────────────────────────────────
function cf-cb-run   { Invoke-CfIn $CfCoreBanking { dotnet run } }
function cf-cb-build { Invoke-CfIn $CfRoot { dotnet build } }
function cf-cb-test  { Invoke-CfIn $CfRoot { dotnet test } }
function cf-test-all { cf-cb-test; cf-an-test }

# ─── Tools & checks ──────────────────────────────────────
# Usage: cf-redis            -> interactive redis-cli
#        cf-redis KEYS '*'   -> run one command
function cf-redis { docker exec -it cashflow-redis redis-cli @args }
function cf-psql  { docker exec -it cashflow-postgres psql -U cashflow -d cashflow_core }
function cf-rabbit-ui { Start-Process 'http://localhost:15672' }   # user/pass: cashflow / cashflow_pass
function cf-swagger   { Start-Process 'http://localhost:5000/swagger' }
# Ports used by the project: 5000 core-banking, 5001 analytics, 5432, 6379, 5672, 15672, 8080
function cf-ports {
    Get-NetTCPConnection -State Listen -ErrorAction SilentlyContinue |
        Where-Object { $_.LocalPort -in 5000, 5001, 5432, 6379, 5672, 15672, 8080 } |
        Sort-Object LocalPort | Select-Object LocalPort, OwningProcess
}

# ─── Conventions ─────────────────────────────────────────
# Create a Java package in main AND test of Analytics (en-US, lowercase, dot separated).
# Usage: cf-mkpkg model            -> com.cashflow.analytics.model
#        cf-mkpkg service.impl     -> com.cashflow.analytics.service.impl
function cf-mkpkg {
    param([Parameter(Mandatory)][string]$Package)
    $rel = 'com\cashflow\analytics\' + ($Package -replace '\.', '\')
    foreach ($set in 'main', 'test') {
        $dir = Join-Path $CfAnalytics "src\$set\java\$rel"
        New-Item -ItemType Directory -Force -Path $dir | Out-Null   # -Force also creates parents and is idempotent
        Write-Host "ok  $dir"
    }
}

# Show current branch and whether it follows Conventional Commits naming (feature/, fix/, chore/, docs/).
function cf-branch {
    Invoke-CfIn $CfRoot {
        $b = git branch --show-current
        Write-Host "branch: $b"
        if ($b -notmatch '^(feature|fix|chore|docs)/') { Write-Warning 'branch name does not follow <type>/<name>' }
    }
}

# Commit following Conventional Commits.
# Usage: cf-commit fix "align TransferCompleted JSON contract" analytics
function cf-commit {
    param(
        [Parameter(Mandatory)][ValidateSet('feat', 'fix', 'chore', 'docs', 'test', 'refactor')][string]$Type,
        [Parameter(Mandatory)][string]$Message,
        [string]$Scope
    )
    $prefix = if ($Scope) { "$Type($Scope)" } else { $Type }
    Invoke-CfIn $CfRoot { git commit -m "${prefix}: $Message" }
}

# ─── Help ────────────────────────────────────────────────
function cf-help {
    Get-Command cf-* -CommandType Function | Sort-Object Name | ForEach-Object { Write-Host $_.Name }
    Write-Host "`nDetails: $CfRoot\scripts\dev\README.md"
}
