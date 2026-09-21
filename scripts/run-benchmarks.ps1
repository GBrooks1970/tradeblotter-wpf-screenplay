[CmdletBinding()]
param(
    [ValidateSet('flaui','winappdriver','ranorex','ranorex-mock')][string[]] $Drivers = @('flaui','winappdriver','ranorex'),
    [ValidateRange(1,100)][int] $Repetitions = 3,
    [ValidateRange(1,1000)][int] $Lookups = 20,
    [ValidateRange(0,10)][int] $Warmups = 1,
    [switch] $AllowUnavailable,
    [string] $Output
)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$repo = Split-Path $PSScriptRoot -Parent
Push-Location $repo
try {
    if (-not $IsWindows) { throw 'Windows is required.' }
    if (Get-Process -Name TradeBlotter.Sut -ErrorAction SilentlyContinue) { throw 'Close existing SUT instances before benchmarking.' }
    & dotnet build src/TradeBlotter.Sut --configuration Release --verbosity quiet
    if ($LASTEXITCODE -ne 0) { throw 'SUT build failed.' }
    $revision = & git rev-parse HEAD
    if ($LASTEXITCODE -ne 0) { throw 'Cannot capture Git revision.' }
    if (& git status --porcelain --untracked-files=normal) { $revision += '-dirty' }
    if (-not $Output) { $Output = Join-Path $repo "TestResults/benchmarks/$([Guid]::NewGuid().ToString('N'))" }
    & dotnet run --project tools/TradeBlotter.Benchmarks --configuration Release -- "--drivers=$($Drivers -join ',')" "--repetitions=$Repetitions" "--lookups=$Lookups" "--warmups=$Warmups" "--output=$Output" "--revision=$revision" "--allow-unavailable=$($AllowUnavailable.IsPresent.ToString().ToLowerInvariant())"
    if ($LASTEXITCODE -ne 0) { throw "Benchmark reported errors or unavailable engines (exit $LASTEXITCODE). See $Output." }
}
finally { Pop-Location }
