#Requires -Version 7.2
[CmdletBinding()]
param(
    [ValidateSet('Debug', 'Release')]
    [string] $Configuration = 'Release',
    [string] $ResultsDirectory = 'artifacts/test-results',
    [string] $ReportDirectory = 'artifacts/coverage-report',
    [int] $MinimumExpectedTests = 1100,
    [double] $MinimumLineCoverage = 33,
    [double] $MinimumBranchCoverage = 28,
    [double] $MinimumMethodCoverage = 35,
    [string] $SummaryPath,
    [switch] $SkipBuild,
    [switch] $SkipToolRestore
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$repositoryPrefix = [System.IO.Path]::GetFullPath($repositoryRoot).TrimEnd(
    [System.IO.Path]::DirectorySeparatorChar,
    [System.IO.Path]::AltDirectorySeparatorChar) + [System.IO.Path]::DirectorySeparatorChar
$pathComparison = if ([OperatingSystem]::IsWindows()) {
    [StringComparison]::OrdinalIgnoreCase
}
else {
    [StringComparison]::Ordinal
}

function Resolve-RepositoryPath([string] $Path) {
    $candidate = if ([System.IO.Path]::IsPathFullyQualified($Path)) {
        $Path
    }
    else {
        Join-Path $repositoryRoot $Path
    }
    $resolved = [System.IO.Path]::GetFullPath($candidate)
    if (-not $resolved.StartsWith($repositoryPrefix, $pathComparison)) {
        throw "Path '$Path' resolves outside the repository."
    }

    return $resolved
}

$resultsPath = Resolve-RepositoryPath $ResultsDirectory

Push-Location $repositoryRoot
try {
    if (-not $SkipToolRestore) {
        & dotnet tool restore
        if ($LASTEXITCODE -ne 0) {
            throw "dotnet tool restore failed with exit code $LASTEXITCODE."
        }
    }

    if (-not $SkipBuild) {
        & dotnet build src/backend/Backend.sln -c $Configuration -warnaserror
        if ($LASTEXITCODE -ne 0) {
            throw "Backend build failed with exit code $LASTEXITCODE."
        }
    }

    # Coverage filenames are generated GUIDs. Remove only old collector output
    # inside the validated artifacts directory so stale runs cannot inflate or
    # contaminate a local report.
    New-Item -ItemType Directory -Path $resultsPath -Force | Out-Null
    Get-ChildItem -LiteralPath $resultsPath -Filter '*.cobertura.xml' -File -Recurse -ErrorAction SilentlyContinue |
        Remove-Item -Force

    $testArguments = @(
        'test', '--solution', 'src/backend/Backend.sln',
        '-c', $Configuration,
        '--no-build',
        '--results-directory', $resultsPath,
        '--minimum-expected-tests', $MinimumExpectedTests,
        '--coverage',
        '--coverage-output-format', 'cobertura',
        '--coverage-settings', 'src/backend/coverage.runsettings'
    )
    & dotnet @testArguments
    if ($LASTEXITCODE -ne 0) {
        throw "Backend tests or coverage collection failed with exit code $LASTEXITCODE."
    }

    $testProjectCount = @(Get-ChildItem -Path 'src/backend/Tests/*/*.Tests.csproj' -File).Count
    $coverageReportCount = @(
        Get-ChildItem -LiteralPath $resultsPath -Filter '*.cobertura.xml' -File -Recurse
    ).Count
    if ($testProjectCount -eq 0 -or $coverageReportCount -ne $testProjectCount) {
        throw "Expected one coverage report for each of $testProjectCount test projects but found $coverageReportCount."
    }

    & (Join-Path $PSScriptRoot 'Invoke-BackendCoverageReport.ps1') `
        -Reports "$ResultsDirectory/**/*.cobertura.xml" `
        -TargetDirectory $ReportDirectory `
        -MinimumLineCoverage $MinimumLineCoverage `
        -MinimumBranchCoverage $MinimumBranchCoverage `
        -MinimumMethodCoverage $MinimumMethodCoverage `
        -SummaryPath $SummaryPath `
        -SkipToolRestore
}
finally {
    Pop-Location
}
