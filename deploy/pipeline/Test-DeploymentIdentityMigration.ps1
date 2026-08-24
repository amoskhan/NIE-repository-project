$ErrorActionPreference = "Stop"
$guardPath = Join-Path $PSScriptRoot "Test-DeploymentIdentity.ps1"
$testRoot = Join-Path ([System.IO.Path]::GetTempPath()) "nie-deployment-identity-$([guid]::NewGuid().ToString('N'))"

try {
    # Simulate the output state produced when Copier preserves customized legacy
    # deployment files while also rendering the new app-named artifacts.
    $chartPath = Join-Path $testRoot "deploy\helm\sample-app"
    $pipelinePath = Join-Path $testRoot "deploy\pipeline"
    New-Item -ItemType Directory -Path $chartPath, $pipelinePath -Force | Out-Null
    New-Item -ItemType File -Path (Join-Path $chartPath "Chart.yaml") -Force | Out-Null
    New-Item -ItemType File -Path (Join-Path $pipelinePath "Start-sample-appRelease.ps1") -Force | Out-Null
    New-Item -ItemType File -Path (Join-Path $pipelinePath "Start-sample-appRelease.sh") -Force | Out-Null

    & $guardPath -ExpectedAppName "sample-app" -RepositoryRoot $testRoot

    $legacyChartPath = Join-Path $testRoot "deploy\helm\application"
    New-Item -ItemType Directory -Path $legacyChartPath -Force | Out-Null
    Set-Content -LiteralPath (Join-Path $legacyChartPath "Chart.yaml") -Value "customized: true"
    Set-Content -LiteralPath (Join-Path $pipelinePath "Start-ApplicationRelease.ps1") -Value "# customized"
    Set-Content -LiteralPath (Join-Path $pipelinePath "Start-ApplicationRelease.sh") -Value "# customized"

    $legacyRejected = $false
    try {
        & $guardPath -ExpectedAppName "sample-app" -RepositoryRoot $testRoot
    } catch {
        $legacyRejected = $_.Exception.Message.Contains("Legacy generic deployment artifacts were found")
    }

    if (-not $legacyRejected) {
        throw "Expected the migration guard to reject legacy artifacts."
    }

    & git -C $testRoot init --quiet
    & git -C $testRoot config user.name "Deployment Identity Test"
    & git -C $testRoot config user.email "deployment-identity-test@example.invalid"
    & git -C $testRoot add --all
    & git -C $testRoot commit --quiet -m "legacy deployment layout"
    $legacyCommit = (& git -C $testRoot rev-parse HEAD).Trim()

    Remove-Item -LiteralPath $legacyChartPath -Recurse -Force
    Remove-Item -LiteralPath (Join-Path $pipelinePath "Start-ApplicationRelease.ps1") -Force
    Remove-Item -LiteralPath (Join-Path $pipelinePath "Start-ApplicationRelease.sh") -Force
    & git -C $testRoot add --all
    & git -C $testRoot commit --quiet -m "app-named deployment layout"

    & $guardPath -ExpectedAppName "sample-app" -RepositoryRoot $testRoot
    $legacyCommitRejected = $false
    try {
        & $guardPath -ExpectedAppName "sample-app" -RepositoryRoot $testRoot -GitCommit $legacyCommit
    } catch {
        $legacyCommitRejected = $_.Exception.Message.Contains("Legacy generic deployment artifacts were found")
    }

    if (-not $legacyCommitRejected) {
        throw "Expected the commit-bound migration guard to reject a legacy SourceRef."
    }

    Write-Host "Deployment identity migration regression test passed."
} finally {
    if (Test-Path $testRoot) {
        Remove-Item -LiteralPath $testRoot -Recurse -Force
    }
}
