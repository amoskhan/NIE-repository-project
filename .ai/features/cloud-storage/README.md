# Cloud Storage (S3 + Local)

> **Status:** `released` | **Removable in derived repos:** **no** — `document-management` depends on it, though you can stay on the local-disk implementation forever

## Overview

Pluggable file storage — local disk or AWS S3 — behind a single `IFileStorageService`
contract. Two implementations: `FileStorageService` (local filesystem) and
`S3FileStorageService` (AWS S3 via the `AWSSDK.S3` package). The active one is selected at
startup by `FileStorage:Provider` (`"S3"` → S3; anything else → local). On real AWS,
credentials come from the **AWS default credential chain** (IAM role / `AWS_ACCESS_KEY_ID`
env / shared profile). For local development the same S3 code can target a
**LocalStack** emulator via an explicit `ServiceUrl` + test credentials.

## Key Files

| Layer        | Path                                                                                      |
| ------------ | ----------------------------------------------------------------------------------------- |
| Contract     | `Services/FileStorage/IFileStorageService.cs`                                             |
| Local        | `Services/FileStorage/FileStorageService.cs`                                              |
| S3           | `Services/FileStorage/S3FileStorageService.cs`                                            |
| Content type | `Services/FileStorage/FileStorageContentTypes.cs`                                         |
| Package      | `AWSSDK.S3` (`Libraries/Services/Services.csproj`)                                        |
| Selection    | `API/Program.cs` — `FileStorage:Provider` switch                                          |
| Local S3 dev | `.devcontainer/docker-compose.yml` (`localstack`) + `.devcontainer/localstack/init-s3.sh` |

## Config

```json
"FileStorage": {
  "Provider": "Local",            // "Local" (default) or "S3"
  "BasePath": "./uploads/...",    // local provider — disk root
  "S3": {
    "BucketName": "...",          // required when Provider = S3
    "KeyPrefix": "",              // optional object-key prefix
    "Region": "ap-southeast-1",   // also AWS:Region / AWS_REGION env; defaults to ap-southeast-1
    "ServiceUrl": "",             // optional custom endpoint (LocalStack/MinIO); empty = real AWS
    "AccessKey": "",              // optional; omit on AWS (use the default credential chain)
    "SecretKey": ""               // optional; omit on AWS
  }
}
```

> **Real AWS**: leave `ServiceUrl` empty and omit `AccessKey`/`SecretKey` — the SDK resolves
> credentials from its default chain (IAM role, env vars, or shared profile) and uses the
> region endpoint. **Custom endpoint** (LocalStack/MinIO): set `ServiceUrl`; the client then
> uses path-style addressing and the explicit `AccessKey`/`SecretKey` if provided.

## Local development with LocalStack

The dev container ships a `localstack` service (S3 only, gateway on `:4566`). Its init hook
(`.devcontainer/localstack/init-s3.sh`) creates the `apptemplate-local` bucket on startup.
The dev `appsettings.json` `FileStorage:S3` block is pre-pointed at it
(`ServiceUrl: http://localhost:4566`, bucket `apptemplate-local`, `test`/`test` creds), with
`Provider` left as `Local`.

To exercise S3 locally:

1. Start the dev stack (`docker compose -f .devcontainer/docker-compose.yml up -d`, or open
   the dev container) — LocalStack comes up and the bucket is created.
2. Set `FileStorage:Provider` to `S3` (env `FileStorage__Provider=S3` or an appsettings
   override).
3. Run the API and upload/download via `DocumentController`; objects land in LocalStack.

Inspect with: `awslocal s3 ls s3://apptemplate-local` (or
`aws --endpoint-url=http://localhost:4566 s3 ls s3://apptemplate-local`).
