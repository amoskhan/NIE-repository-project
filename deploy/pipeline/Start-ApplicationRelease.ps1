param(
    [Parameter(Mandatory = $true)]
    [ValidateSet("dev", "stg", "prd")]
    [string]$Environment,

    [Parameter(Mandatory = $true)]
    [string]$InfraRepoPath,

    [string]$SourceRepoPath,
    [string]$SourceRef = "HEAD",
    [string]$AwsProfile,
    [string]$Region = "ap-southeast-1",
    [switch]$Wait
)

$ErrorActionPreference = "Stop"
$env:AWS_PAGER = ""
$env:AWS_CLI_AUTO_PROMPT = "off"

function Get-RequiredCommand {
    param([string]$Name)
    if (-not (Get-Command $Name -ErrorAction SilentlyContinue)) {
        throw "Required command '$Name' was not found in PATH."
    }
}

function Get-TfvarString {
    param([string]$Path, [string]$Name)
    $pattern = "^\s*$([regex]::Escape($Name))\s*=\s*`"(?<value>[^`"]+)`""
    $match = Select-String -Path $Path -Pattern $pattern | Select-Object -First 1
    if ($match) { return $match.Matches[0].Groups["value"].Value }
    return $null
}

function Resolve-SourceCommit {
    param([string]$RepoPath, [string]$Ref)
    $sourceRoot = (Resolve-Path $RepoPath).Path
    $resolvedCommit = (& git -C $sourceRoot rev-parse --verify "$Ref`^{commit}").Trim()
    if ($LASTEXITCODE -ne 0 -or -not $resolvedCommit) {
        throw "Git ref '$Ref' could not be resolved in $sourceRoot."
    }
    return $resolvedCommit
}

function New-SourceZip {
    param([string]$RepoPath, [string]$Commit, [string]$DestinationPath)
    if (Test-Path $DestinationPath) { Remove-Item $DestinationPath -Force }
    $sourceRoot = (Resolve-Path $RepoPath).Path
    & git -C $sourceRoot archive --format=zip --output=$DestinationPath $Commit
    if ($LASTEXITCODE -ne 0 -or -not (Test-Path $DestinationPath)) {
        throw "git archive failed while preparing source artifact for '$Commit'."
    }
}

Get-RequiredCommand git
Get-RequiredCommand aws
Get-RequiredCommand terraform

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
if (-not $SourceRepoPath) { $SourceRepoPath = Resolve-Path (Join-Path $scriptDir "..\.." ) }
$SourceRepoPath = (Resolve-Path $SourceRepoPath).Path
$resolvedCommit = Resolve-SourceCommit -RepoPath $SourceRepoPath -Ref $SourceRef
$identityGuardPath = Join-Path $scriptDir "Test-DeploymentIdentity.ps1"
if (-not (Test-Path $identityGuardPath -PathType Leaf)) {
    throw "Deployment identity guard was not found: $identityGuardPath"
}
& $identityGuardPath -RepositoryRoot $SourceRepoPath -GitCommit $resolvedCommit

$InfraRepoPath = (Resolve-Path $InfraRepoPath).Path
$appInfraPath = Join-Path $InfraRepoPath "app\application"
$tfvarsPath = Join-Path $appInfraPath "environments\$Environment.tfvars"

if (-not (Test-Path $tfvarsPath)) { throw "Terraform tfvars file not found: $tfvarsPath" }
if (-not $AwsProfile) { $AwsProfile = Get-TfvarString -Path $tfvarsPath -Name "aws_profile" }
if (-not $AwsProfile) { throw "AWS profile was not provided and could not be read from $tfvarsPath." }

$pipelineName = $null
try { $pipelineName = (& terraform -chdir=$appInfraPath output -raw pipeline_name 2>$null).Trim() } catch { $pipelineName = $null }
if (-not $pipelineName) { $pipelineName = "application-$Environment-pipeline" }

$pipelineJson = & aws codepipeline get-pipeline --name $pipelineName --region $Region --profile $AwsProfile --output json --no-cli-pager
$pipeline = $pipelineJson | ConvertFrom-Json
$sourceAction = $pipeline.pipeline.stages |
    Where-Object { $_.name -eq "Source" } |
    Select-Object -ExpandProperty actions |
    Where-Object { $_.name -eq "Source" } |
    Select-Object -First 1

if (-not $sourceAction) { throw "Could not find Source action in pipeline '$pipelineName'." }
$sourceBucket = $sourceAction.configuration.S3Bucket
$sourceObjectKey = $sourceAction.configuration.S3ObjectKey
if (-not $sourceBucket -or -not $sourceObjectKey) { throw "Pipeline '$pipelineName' does not have an S3 source bucket/key configured." }

$artifactPath = Join-Path ([System.IO.Path]::GetTempPath()) "application-$Environment-source.zip"
Write-Host "Packaging $SourceRepoPath to $artifactPath"
New-SourceZip -RepoPath $SourceRepoPath -Commit $resolvedCommit -DestinationPath $artifactPath
Write-Host "Packaged immutable source commit: $resolvedCommit"

Write-Host "Uploading source artifact to s3://$sourceBucket/$sourceObjectKey"
& aws s3 cp $artifactPath "s3://$sourceBucket/$sourceObjectKey" --region $Region --profile $AwsProfile --no-cli-pager
if ($LASTEXITCODE -ne 0) { throw "Failed to upload source artifact." }

Write-Host "Starting CodePipeline execution: $pipelineName"
$executionId = (& aws codepipeline start-pipeline-execution --name $pipelineName --region $Region --profile $AwsProfile --query pipelineExecutionId --output text --no-cli-pager).Trim()
Write-Host "Pipeline execution ID: $executionId"

if ($Wait) {
    do {
        Start-Sleep -Seconds 30
        $status = (& aws codepipeline get-pipeline-execution --pipeline-name $pipelineName --pipeline-execution-id $executionId --region $Region --profile $AwsProfile --query 'pipelineExecution.status' --output text --no-cli-pager).Trim()
        Write-Host "Pipeline status: $status"
    } while ($status -in @("InProgress", "Stopping"))
    if ($status -ne "Succeeded") {
        & aws codepipeline list-action-executions --pipeline-name $pipelineName --region $Region --profile $AwsProfile --filter "pipelineExecutionId=$executionId" --query "actionExecutionDetails[].{stage:stageName,action:actionName,status:status,error:errorDetails.message}" --output table --no-cli-pager
        throw "Pipeline execution finished with status '$status'."
    }
}
