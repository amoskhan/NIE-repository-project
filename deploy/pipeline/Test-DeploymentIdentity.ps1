[CmdletBinding()]
param(
    [ValidatePattern("^[a-z][a-z0-9-]{2,39}$")]
    [string]$ExpectedAppName = "application",

    [string]$RepositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path,

    [string]$GitCommit
)

$ErrorActionPreference = "Stop"
$repositoryPath = (Resolve-Path $RepositoryRoot).Path
$expectedChartPath = Join-Path $repositoryPath "deploy\helm\$ExpectedAppName"
$expectedReleaseStem = if ($ExpectedAppName -eq "application") {
    "Start-ApplicationRelease"
} else {
    "Start-${ExpectedAppName}Release"
}
$expectedReleaseFiles = @(
    "$expectedReleaseStem.ps1",
    "$expectedReleaseStem.sh"
)
$legacyChartPath = Join-Path $repositoryPath "deploy\helm\application"
$legacyReleaseFiles = @(
    "Start-ApplicationRelease.ps1",
    "Start-ApplicationRelease.sh"
)
$problems = [System.Collections.Generic.List[string]]::new()

if ($GitCommit) {
    $resolvedCommit = (& git -C $repositoryPath rev-parse --verify "$GitCommit`^{commit}").Trim()
    if ($LASTEXITCODE -ne 0 -or -not $resolvedCommit) {
        throw "Git commit '$GitCommit' could not be resolved in $repositoryPath."
    }

    $treeFiles = @(
        & git -C $repositoryPath ls-tree -r --name-only $resolvedCommit -- deploy/helm deploy/pipeline
    )
    if ($LASTEXITCODE -ne 0) {
        throw "Git tree inspection failed for commit '$resolvedCommit'."
    }

    $expectedChartFile = "deploy/helm/$ExpectedAppName/Chart.yaml"
    if ($treeFiles -cnotcontains $expectedChartFile) {
        $problems.Add("Expected Helm chart is missing from commit ${resolvedCommit}: $expectedChartFile")
    }
    foreach ($expectedReleaseFile in $expectedReleaseFiles) {
        $expectedReleaseTreeFile = "deploy/pipeline/$expectedReleaseFile"
        if ($treeFiles -cnotcontains $expectedReleaseTreeFile) {
            $problems.Add("Expected release script is missing from commit ${resolvedCommit}: $expectedReleaseTreeFile")
        }
    }

    if ($ExpectedAppName -ne "application") {
        if ($treeFiles.Where({ $_.StartsWith("deploy/helm/application/", [StringComparison]::Ordinal) }).Count -gt 0) {
            $problems.Add("Legacy generic Helm chart still contains files in commit ${resolvedCommit}: deploy/helm/application")
        }
        foreach ($legacyReleaseFile in $legacyReleaseFiles) {
            $legacyReleaseTreeFile = "deploy/pipeline/$legacyReleaseFile"
            if ($treeFiles -ccontains $legacyReleaseTreeFile) {
                $problems.Add("Legacy generic release script still exists in commit ${resolvedCommit}: $legacyReleaseTreeFile")
            }
        }
    }
} else {
    if (-not (Test-Path (Join-Path $expectedChartPath "Chart.yaml") -PathType Leaf)) {
        $problems.Add("Expected Helm chart is missing: $expectedChartPath")
    }
    foreach ($expectedReleaseFile in $expectedReleaseFiles) {
        $expectedReleasePath = Join-Path $repositoryPath "deploy\pipeline\$expectedReleaseFile"
        if (-not (Test-Path $expectedReleasePath -PathType Leaf)) {
            $problems.Add("Expected release script is missing: $expectedReleasePath")
        }
    }

    if ($ExpectedAppName -ne "application") {
        $legacyChartFiles = @(
            Get-ChildItem -LiteralPath $legacyChartPath -Recurse -File -ErrorAction SilentlyContinue
        )
        if ($legacyChartFiles.Count -gt 0) {
            $problems.Add("Legacy generic Helm chart still contains files: $legacyChartPath")
        }
        foreach ($legacyReleaseFile in $legacyReleaseFiles) {
            $legacyReleasePath = Join-Path $repositoryPath "deploy\pipeline\$legacyReleaseFile"
            if (Test-Path $legacyReleasePath -PathType Leaf) {
                $problems.Add("Legacy generic release script still exists: $legacyReleasePath")
            }
        }
    }
}

if ($problems.Count -gt 0) {
    $details = $problems -join [Environment]::NewLine
    throw @"
Deployment identity validation failed.
$details

Legacy generic deployment artifacts were found or app-named artifacts are missing.
Do not deploy or delete customized files automatically. Follow the non-destructive
'Deployment identity migration on update' workflow in docs/template-distribution.md,
then rerun this guard.
"@
}

Write-Host "Deployment identity is unambiguous for '$ExpectedAppName'."
