# AI conformance report template

Copy this structure into the pull request, change report, or AI handoff. Do not create a permanent report file for every change unless the repository requires one.

## Change

- Request:
- Acceptance criteria:
- Implementing AI:
- Independent verifying AI:
- Local rules version:
- Canonical repository and commit:
- Application/template commit assessed:

## Scope and risk

- Adopted features affected:
- Global rules affected:
- Security/privacy/data risks:
- Breaking or destructive decisions requiring approval:
- Assumptions:

## Rule evidence

| Rule ID | Status | Implementation evidence | Test/command evidence | Notes or residual risk |
| --- | --- | --- | --- | --- |
| NIE-... | pass/fail/not-applicable/manual-review/approved-exception | File and line | Exact command and result/artifact | Explanation |

## Shared-code comparison

| Canonical file/component | Classification | Application action | Evidence |
| --- | --- | --- | --- |
| path | identical/behind/customized/ahead/conflict/not-applicable | retained/replaced/merged/deferred | Diff and tests |

## Libraries and versions

| Runtime/library | Required minimum | Actual | Change | Compatibility/security evidence |
| --- | --- | --- | --- | --- |

## Dependency selection and portability

Complete this section for every added, removed, or materially replaced dependency.

| Concern | Candidates considered | Selected package and verified publisher | Official/open-source status and license | Maintenance, security, and adoption evidence | Abstraction boundary | Alternative and exit/migration plan |
| --- | --- | --- | --- | --- | --- | --- |
| | | | | | | |

## Verification summary

| Gate | Command or workflow | Result | Counts/artifact |
| --- | --- | --- | --- |
| C# format/analyzers/build | | | |
| C# tests/coverage | | | |
| ESLint/type-check/build | | | |
| Vue tests/coverage | | | |
| API/browser tests | | | |
| Dependency/static/secret scans | | | |

## Exception record

For each exception include rule ID, rationale, risk, owner, approver, creation date, expiry, remediation, and tracking reference. An unapproved or non-expiring waiver is a failure.

## Independent verifier verdict

- Overall verdict:
- Rules challenged:
- Checks rerun:
- Findings requiring correction:
- Manual decisions still required:
- Residual risk:
