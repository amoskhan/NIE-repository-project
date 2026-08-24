#Requires -Version 7.2
[CmdletBinding()]
param(
    [string[]] $Projects = @('Domain.csproj', 'Application.csproj'),
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

    Push-Location 'src/backend'
    try {
        foreach ($project in $Projects) {
            $projectName = [System.IO.Path]::GetFileNameWithoutExtension($project)
            & dotnet tool run dotnet-stryker -- `
                --config-file stryker-config.json `
                --project $project `
                --test-runner mtp `
                --output "StrykerOutput/$projectName"
            if ($LASTEXITCODE -ne 0) {
                throw "Stryker failed for $project with exit code $LASTEXITCODE."
            }
        }
    }
    finally {
        Pop-Location
    }
}
finally {
    Pop-Location
}
