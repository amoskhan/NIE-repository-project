#Requires -Version 7.2
[CmdletBinding()]
param(
    [string] $Reports = 'artifacts/test-results/**/*.cobertura.xml',
    [string] $TargetDirectory = 'artifacts/coverage-report',
    [double] $MinimumLineCoverage = 33,
    [double] $MinimumBranchCoverage = 28,
    [double] $MinimumMethodCoverage = 35,
    [string] $SummaryPath,
    [switch] $SkipToolRestore
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repositoryRoot = Split-Path -Parent $PSScriptRoot
Push-Location $repositoryRoot
try {
    if (-not $SkipToolRestore) {
        & dotnet tool restore
        if ($LASTEXITCODE -ne 0) {
            throw "dotnet tool restore failed with exit code $LASTEXITCODE."
        }
    }

    $arguments = @(
        'tool', 'run', 'reportgenerator', '--',
        "-reports:$Reports",
        "-targetdir:$TargetDirectory",
        '-reporttypes:Html;Cobertura;MarkdownSummaryGithub;TextSummary;SonarQube',
        '-title:NIE Template backend coverage',
        'riskHotspotsAnalysisThresholds:metricThresholdForCyclomaticComplexity=15',
        'riskHotspotsAnalysisThresholds:metricThresholdForCrapScore=30',
        "minimumCoverageThresholds:lineCoverage=$MinimumLineCoverage",
        "minimumCoverageThresholds:branchCoverage=$MinimumBranchCoverage",
        "minimumCoverageThresholds:methodCoverage=$MinimumMethodCoverage"
    )

    & dotnet @arguments
    if ($LASTEXITCODE -ne 0) {
        throw "ReportGenerator failed or a backend coverage floor was not met (exit code $LASTEXITCODE)."
    }

    $textSummary = Join-Path $TargetDirectory 'Summary.txt'
    if (Test-Path -LiteralPath $textSummary) {
        Get-Content -LiteralPath $textSummary
    }

    if ($SummaryPath) {
        $githubSummary = Join-Path $TargetDirectory 'SummaryGithub.md'
        if (Test-Path -LiteralPath $githubSummary) {
            Add-Content -LiteralPath $SummaryPath -Value (Get-Content -LiteralPath $githubSummary -Raw)
        }
    }
}
finally {
    Pop-Location
}
