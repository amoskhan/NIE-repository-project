# AWS Pipeline Buildspecs

These buildspecs are used by the CodeBuild projects created from the shared AWS app infrastructure for NIE Template.

- `build/Jenkinsfile`
  - checks out the requested branch or Git ref and runs the standard frontend and backend gates
  - packages the exact checked-out commit with `git archive`
  - uploads the archive to the environment-specific S3 source key
  - starts the matching CodePipeline execution and waits for its terminal status

- `buildspec.build.yml`
  - installs the frontend workspace with `pnpm@10.33.0`
  - builds the main and auth Vite apps from Terraform-provided app URLs
  - publishes the main UI and login UI to S3
  - invalidates the app CloudFront distribution when `CLOUDFRONT_DISTRIBUTION_ID` is provided
  - builds and pushes backend API images to ECR
  - emits `build-artifacts/image-map.json` for the deploy stage
- `buildspec.deploy.yml`
  - updates kubeconfig for the shared EKS cluster
  - injects the ECR image map into the Helm values
  - runs `helm upgrade --install --wait` for the API workloads
- `Start-ApplicationRelease.ps1` / `Start-ApplicationRelease.sh`
  - validates required CLIs are available
  - reads the source bucket and pipeline name from Terraform state in the infra repo
  - packages an immutable source zip from `-SourceRef` (default `HEAD`) with `git archive`
  - uploads the artifact to the environment-specific source key and starts the release pipeline
  - optionally waits for the pipeline execution to finish
- `Test-DeploymentIdentity.ps1` / `Test-DeploymentIdentity.sh`
  - verifies the expected app-named Helm chart and release script exist
  - blocks deployment when a Copier update has preserved legacy generic chart
    or release files beside the app-named files
  - never deletes or rewrites customized deployment files
  - validates the exact resolved Git commit selected by `-SourceRef` before the
    release script archives it

## Example

```powershell
.\deploy\pipeline\Start-ApplicationRelease.ps1 -Environment stg -InfraRepoPath C:\git\nie-eks-aio -Wait
```

```bash
bash deploy/pipeline/Start-ApplicationRelease.sh --environment stg --infra-repo-path "$HOME/git/nie-eks-aio" --wait
```

The Jenkins job requires `SOURCE_BUCKET`. `SOURCE_OBJECT_KEY` and
`PIPELINE_NAME` default to `application/<environment>/source.zip` and
`application-<environment>-pipeline`; both can be overridden when the shared
infrastructure exports different names. AWS credentials are supplied by the
agent role or the optional `AWS_PROFILE` parameter.

After every `copier update`, run:

```powershell
.\deploy\pipeline\Test-DeploymentIdentity.ps1
```

```bash
bash deploy/pipeline/Test-DeploymentIdentity.sh
```

If the guard detects legacy deployment files, follow the deployment identity
migration in `docs/template-distribution.md` before deploying.

## URLs

- Development: `https://apps.dev.nie.edu.sg/MYAPP`
- Staging: `https://apps.stg.nie.edu.sg/MYAPP`
- Production: `https://apps.nie.edu.sg/MYAPP`
