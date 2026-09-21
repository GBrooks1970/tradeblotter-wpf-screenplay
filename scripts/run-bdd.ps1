[CmdletBinding(PositionalBinding = $false)]
param(
    [ValidateSet('flaui', 'winappdriver')][string] $Driver = 'flaui',
    [switch] $Benchmark,
    [Parameter(Position = 0, ValueFromRemainingArguments)][string[]] $Options = @()
)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
foreach ($option in $Options) {
    if ($option -notmatch '^--driver=(flaui|winappdriver)$') { throw "Unknown option: $option" }
    $Driver = $Matches[1]
}
if (-not $IsWindows) { throw 'Windows is required.' }
$repo = Split-Path $PSScriptRoot -Parent
$run = Join-Path $repo "TestResults/$Driver/$([Guid]::NewGuid().ToString('N'))"
New-Item -ItemType Directory -Path $run -Force | Out-Null
$oldDriver = $env:TRADEBLOTTER_DRIVER
$oldHome = $env:APPIUM_HOME
$oldDiagnostics = $env:TRADEBLOTTER_DIAGNOSTICS_DIRECTORY
$owned = [System.Collections.Generic.List[System.Diagnostics.Process]]::new()
function Start-Owned([string] $File, [string[]] $Arguments, [string] $Name) {
    $process = Start-Process -FilePath $File -ArgumentList $Arguments -PassThru -WindowStyle Hidden -WorkingDirectory $repo -RedirectStandardOutput "$run/$Name.stdout.log" -RedirectStandardError "$run/$Name.stderr.log"
    $owned.Add($process)
    return $process
}
function Wait-Server([System.Diagnostics.Process] $Process, [string] $Url) {
    $timer = [Diagnostics.Stopwatch]::StartNew()
    while ($timer.Elapsed.TotalSeconds -lt 45) {
        if ($Process.HasExited) { throw "Server PID $($Process.Id) exited. See $run." }
        try {
            Invoke-RestMethod -Uri $Url -TimeoutSec 2 | Out-Null
            return
        } catch { Start-Sleep -Milliseconds 250 }
    }
    throw "Server readiness timed out: $Url. See $run."
}
Push-Location $repo
try {
    $env:TRADEBLOTTER_DRIVER = $Driver
    $env:TRADEBLOTTER_DIAGNOSTICS_DIRECTORY = Join-Path $run 'desktop'
    if ($Driver -eq 'winappdriver') {
        $wad = Join-Path ${env:ProgramFiles(x86)} 'Windows Application Driver/WinAppDriver.exe'
        if (-not (Test-Path $wad)) { throw 'Install Microsoft WinAppDriver 1.2.1 and enable Windows Developer Mode before running parity.' }
        foreach ($port in 4723,4724) {
            if (Get-NetTCPConnection -LocalPort $port -State Listen -ErrorAction SilentlyContinue) { throw "Port $port is already owned. Refusing to reuse or stop that server." }
        }
        $appium = Join-Path $repo 'tools/winappdriver/node_modules/appium/build/lib/main.js'
        if (-not (Test-Path $appium)) { throw 'Run npm ci --prefix tools/winappdriver first.' }
        $env:APPIUM_HOME = Join-Path $run 'appium-home'
        $driverPath = Join-Path $repo 'tools/winappdriver/node_modules/appium-windows-driver'
        & node $appium driver install --source=local $driverPath
        if ($LASTEXITCODE -ne 0) { throw 'Appium Windows driver registration failed.' }
        $backend = Start-Owned $wad @('127.0.0.1','4724') 'winappdriver'
        Wait-Server $backend 'http://127.0.0.1:4724/status'
        $proxy = Start-Owned (Get-Command node).Source @(('"' + $appium + '"'),'--address','127.0.0.1','--port','4723','--base-path','/') 'appium'
        Wait-Server $proxy 'http://127.0.0.1:4723/status'
    }
    & dotnet test tests/TradeBlotter.Specs/TradeBlotter.Specs.csproj --configuration Release --filter 'FullyQualifiedName~TradeBlotter.Specs.Features' --results-directory $run --logger 'trx;LogFileName=bdd.trx' --logger 'console;verbosity=normal' --blame-hang-timeout 90s --blame-hang-dump-type none
    if ($LASTEXITCODE -ne 0) { throw "BDD failed for $Driver. See $run." }
    [xml] $trx = Get-Content "$run/bdd.trx" -Raw
    $counts = $trx.TestRun.ResultSummary.Counters
    if ([int]$counts.total -ne 10 -or [int]$counts.executed -ne 10 -or [int]$counts.passed -ne 10) { throw "Expected 10 executed and passed scenarios: $($counts.OuterXml)" }
    Write-Host "VERIFIED ${Driver}: 10/10 unchanged BDD scenarios passed."
    if ($Benchmark) {
        $benchmarkDrivers = if ($Driver -eq 'winappdriver') { @('flaui','winappdriver','ranorex-mock','ranorex') } else { @('flaui','ranorex-mock','ranorex') }
        & "$PSScriptRoot/run-benchmarks.ps1" -Drivers $benchmarkDrivers -AllowUnavailable -Output (Join-Path $run 'benchmark')
        # This job promises a native sample for its selected driver. The allowed
        # unavailable row is Ranorex only, not a way to hide failed prerequisites.
        $samples = @(Import-Csv (Join-Path $run 'benchmark/samples.csv'))
        if (@($samples | Where-Object { $_.engine -eq $Driver -and $_.phase -eq 'measured' -and $_.status -eq 'PASS' }).Count -ne 3) {
            throw "Expected three native benchmark samples for $Driver."
        }
    }
}
finally {
    $cleanupErrors = @()
    foreach ($process in $owned) {
        try {
            if (-not $process.HasExited) {
                $process.Kill($true)
                if (-not $process.WaitForExit(5000)) { throw "Owned server PID $($process.Id) did not exit." }
            }
        } catch { $cleanupErrors += $_ }
        finally { $process.Dispose() }
    }
    $env:TRADEBLOTTER_DRIVER = $oldDriver
    $env:APPIUM_HOME = $oldHome
    $env:TRADEBLOTTER_DIAGNOSTICS_DIRECTORY = $oldDiagnostics
    Pop-Location
    if ($cleanupErrors.Count) { throw ($cleanupErrors -join [Environment]::NewLine) }
    Write-Host 'Owned automation servers stopped.'
}
