$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
if (-not $IsWindows) { throw 'Windows is required for the WPF desktop suite.' }

$repoPath = Split-Path $PSScriptRoot -Parent
Push-Location $repoPath
try {
    $resultsPath = Join-Path $repoPath 'TestResults/ci'
    New-Item -ItemType Directory -Path $resultsPath -Force | Out-Null
    # A unique run directory prevents stale TRX files from satisfying this execution.
    $runPath = Join-Path $resultsPath ([Guid]::NewGuid().ToString('N'))
    New-Item -ItemType Directory -Path $runPath | Out-Null
    Add-Type -AssemblyName System.Windows.Forms
    $desktop = [pscustomobject]@{
        OS = [Environment]::OSVersion.VersionString
        SessionId = (Get-Process -Id $PID).SessionId
        UserInteractive = [Environment]::UserInteractive
        Screen = [System.Windows.Forms.Screen]::PrimaryScreen.Bounds.ToString()
        ImageVersion = $env:ImageVersion
    }
    $desktop | ConvertTo-Json | Tee-Object -FilePath (Join-Path $runPath 'desktop.json') | Write-Host

    function Invoke-DotNet([string[]] $Arguments) {
        & dotnet @Arguments
        if ($LASTEXITCODE -ne 0) { throw "dotnet $($Arguments -join ' ') failed with exit $LASTEXITCODE" }
    }

    $projects = @(
        'src/TradeBlotter.Sut/TradeBlotter.Sut.csproj',
        'probes/TradeBlotter.Probe/TradeBlotter.Probe.csproj',
        'src/TradeBlotter.Framework/TradeBlotter.Framework.csproj',
        'src/TradeBlotter.Screenplay/TradeBlotter.Screenplay.csproj',
        'tests/TradeBlotter.Framework.Tests/TradeBlotter.Framework.Tests.csproj',
        'tests/TradeBlotter.Screenplay.Tests/TradeBlotter.Screenplay.Tests.csproj',
        'tests/TradeBlotter.Specs/TradeBlotter.Specs.csproj'
    )
    foreach ($project in $projects) {
        Invoke-DotNet @('restore', $project)
        Invoke-DotNet @('build', $project, '--configuration', 'Release', '--no-restore')
    }

    $env:TRADEBLOTTER_SUT = Join-Path $repoPath 'src/TradeBlotter.Sut/bin/Release/net9.0-windows/TradeBlotter.Sut.exe'
    $suites = @(
        @{ Name = 'framework'; Project = $projects[4]; Filter = 'FullyQualifiedName~DriverTests'; Count = 16 },
        @{ Name = 'screenplay'; Project = $projects[5]; Filter = 'FullyQualifiedName~ScreenplayTests'; Count = 34 },
        @{ Name = 'bdd'; Project = $projects[6]; Filter = 'FullyQualifiedName~TradeBlotter.Specs.Features'; Count = 10 },
        @{ Name = 'native'; Project = $projects[4]; Filter = 'FullyQualifiedName~DesktopSmokeTests'; Count = 1 }
    )
    foreach ($suite in $suites) {
        Invoke-DotNet @('test', $suite.Project, '--configuration', 'Release', '--no-build', '--no-restore',
            '--filter', $suite.Filter, '--results-directory', $runPath, '--logger', "trx;LogFileName=$($suite.Name).trx",
            '--logger', 'console;verbosity=normal', '--blame-hang-timeout', '90s', '--blame-hang-dump-type', 'none')
        [xml] $trx = Get-Content -LiteralPath (Join-Path $runPath "$($suite.Name).trx") -Raw
        $counts = $trx.TestRun.ResultSummary.Counters
        if ([int]$counts.total -ne $suite.Count -or [int]$counts.passed -ne $suite.Count -or [int]$counts.executed -ne $suite.Count) {
            throw "$($suite.Name): expected $($suite.Count) executed and passed; got $($counts.OuterXml)"
        }
        Write-Host "VERIFIED $($suite.Name): $($suite.Count)/$($suite.Count) passed"
    }
    $remaining = @(Get-Process -Name TradeBlotter.Sut -ErrorAction SilentlyContinue)
    if ($remaining.Count -ne 0) { throw "SUT processes remain after tests: $($remaining.Id -join ', ')" }
    Write-Host 'PASS: 61 tests, four TRX reports, no remaining SUT process.'
}
finally {
    Pop-Location
}
