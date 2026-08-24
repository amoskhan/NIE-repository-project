#Requires -Version 7.2
[CmdletBinding()]
param(
    [ValidateSet('Debug', 'Release')]
    [string] $Configuration = 'Release',
    [string] $OpenCoverDirectory = 'artifacts/opencover',
    [string] $ReportDirectory = 'artifacts/crap-report',
    [double] $MaximumCrapScore = 40000,
    [double] $MaximumCyclomaticComplexity = 200,
    [int] $Top = 25,
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

$openCoverPath = Resolve-RepositoryPath $OpenCoverDirectory
$reportPath = Resolve-RepositoryPath $ReportDirectory

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

    $testProjects = @(
        Get-ChildItem -Path 'src/backend/Tests/*/*.Tests.csproj' -File |
            Sort-Object -Property FullName
    )
    if ($testProjects.Count -eq 0) {
        throw 'No backend test projects were found.'
    }

    New-Item -ItemType Directory -Path $openCoverPath -Force | Out-Null
    Get-ChildItem -LiteralPath $openCoverPath -Filter '*.opencover.xml' -File -ErrorAction SilentlyContinue |
        Remove-Item -Force

    foreach ($testProject in $testProjects) {
        $name = [System.IO.Path]::GetFileNameWithoutExtension($testProject.Name)
        $testAssemblyPath = Join-Path $testProject.Directory.FullName "bin/$Configuration/net10.0/$name.dll"
        if (-not (Test-Path -LiteralPath $testAssemblyPath -PathType Leaf)) {
            throw "Expected built test assembly '$testAssemblyPath' for '$($testProject.FullName)'."
        }

        $outputPath = Join-Path $openCoverPath "$name.opencover.xml"
        $targetArguments = "test --project `"$($testProject.FullName)`" -c $Configuration --no-build --no-progress"
        $coverletArguments = @(
            'tool', 'run', 'coverlet', '--',
            $testAssemblyPath,
            '--target', 'dotnet',
            '--targetargs', $targetArguments,
            '--format', 'opencover',
            '--output', $outputPath,
            '--exclude', '[*.Tests]*',
            '--exclude-by-file', '**/Migrations/**'
        )

        Write-Host "Collecting OpenCover coverage for $name"
        & dotnet @coverletArguments
        if ($LASTEXITCODE -ne 0) {
            throw "Coverage collection failed for $name with exit code $LASTEXITCODE."
        }
        if (-not (Test-Path -LiteralPath $outputPath)) {
            throw "Coverage collection for $name did not produce '$outputPath'."
        }
    }

    $reports = @(Get-ChildItem -LiteralPath $openCoverPath -Filter '*.opencover.xml' -File)
    if ($reports.Count -ne $testProjects.Count) {
        throw "Expected $($testProjects.Count) OpenCover reports but found $($reports.Count)."
    }

    $reportArguments = @(
        'tool', 'run', 'reportgenerator', '--',
        "-reports:$OpenCoverDirectory/*.opencover.xml",
        "-targetdir:$ReportDirectory",
        '-reporttypes:Html;MarkdownSummaryGithub;TextSummary',
        '-title:NIE Template risk hotspots',
        'riskHotspotsAnalysisThresholds:metricThresholdForCyclomaticComplexity=15',
        "riskHotspotsAnalysisThresholds:maximumThresholdForCyclomaticComplexity=$MaximumCyclomaticComplexity"
    )
    & dotnet @reportArguments
    if ($LASTEXITCODE -ne 0) {
        throw "ReportGenerator complexity analysis failed with exit code $LASTEXITCODE."
    }

    if ($SummaryPath) {
        $complexitySummary = Join-Path $reportPath 'SummaryGithub.md'
        if (Test-Path -LiteralPath $complexitySummary) {
            Add-Content -LiteralPath $SummaryPath -Value (Get-Content -LiteralPath $complexitySummary -Raw)
        }
    }

    & (Join-Path $PSScriptRoot 'Get-CrapScore.ps1') `
        -Path "$OpenCoverDirectory/*.opencover.xml" `
        -MaximumCrapScore $MaximumCrapScore `
        -Top $Top `
        -SummaryPath $SummaryPath
}
finally {
    Pop-Location
}
