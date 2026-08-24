# Deployment Helm Pipeline

Standard deploy/helm and deploy/pipeline scaffold for AWS CodePipeline, CodeBuild, S3/CloudFront frontend delivery, and EKS backend workloads.

Rules version: 2026.08.07.1
Feature key: deployment-helm-pipeline  
Adoption: **mandatory**

## Adoption and navigation

- Menu or entry point: not independently required. No dedicated menu is required.
- Visibility: Deployment is operational infrastructure, not application navigation.
- If the feature is not adopted, report not-applicable with the product or architecture reason; do not silently omit it.

## Required libraries and minimums

| Library | Package/runtime | Minimum | Ecosystem |
| --- | --- | --- | --- |
| Helm | helm | 3.17.3 | tool |

Versions are floors, not forced pins for derived applications. Stable upgrades are allowed when compatibility, build, tests, security, and deployment evidence pass. The template itself keeps exact tested pins and lockfiles.

## Rules

| Rule | Severity | Area | Requirement | Required evidence |
| --- | --- | --- | --- | --- |
| NIE-DEPLOY-001 | error | structure | Keep deploy/helm and deploy/pipeline parameterized by application key, environment, image repository/tag, and workload. | helm-lint |
| NIE-DEPLOY-002 | error | security | Reference secrets from the deployment platform; never store credentials in chart values, build artifacts, or logs. | security-scan |
| NIE-DEPLOY-003 | error | operations | Configure liveness/readiness probes, resource requests/limits, rollout safety, disruption budgets, and autoscaling where needed. | helm-tests |
| NIE-DEPLOY-004 | error | supply-chain | Use immutable sanitized image tags/digests, dependency scanning, SBOM generation, and signed/traceable build provenance where available. | pipeline |
| NIE-DEPLOY-005 | error | verification | Run helm lint/template, container build, manifest policy checks, and a deployment smoke test before promotion. | pipeline |
| NIE-DEPLOY-006 | error | rollback | Document and test rollback for application images, configuration, and database migrations. | release-review |

## Canonical reference footprint

Use these files as working examples and comparison targets. Procurement is a reference vertical, not a runtime dependency.

- deploy/helm/<appKey>/Chart.yaml
- deploy/helm/<appKey>/values.yaml
- deploy/helm/<appKey>/templates/workloads.yaml
- deploy/helm/<appKey>/templates/ingress.yaml
- deploy/pipeline/buildspec.build.yml
- deploy/pipeline/buildspec.deploy.yml
- deploy/pipeline/Start-<AppName>Release.ps1

## AI implementation and verification

The implementing AI must:

1. Assess every rule above before editing and identify affected backend, frontend, data, security, navigation, and test surfaces.
2. Compare the application implementation with the pinned canonical template reference. Classify each shared file as identical, behind, customized, ahead, conflict, or not applicable.
3. Implement missing behavior directly. Preserve domain behavior and integrate shared updates through extension points rather than overwriting valid customization.
4. Add or update focused unit, architecture, integration, component, API, and browser tests according to the evidence type for each rule.
5. Run the standard repository gates and record exact commands, exit status, and meaningful test counts or artifacts.
6. Report each rule as pass, fail, not-applicable, manual-review, or approved-exception with file/line and test evidence.

A separate AI verifier must review material changes from a fresh context, inspect the diff and evidence, rerun risk-relevant gates, and issue an independent verdict. A claim without traceable evidence is not a pass.
