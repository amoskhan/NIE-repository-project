# Cloud Storage - File Map

## Owned files

| Path                                                                             | Layer          | Purpose                                                                                                                                                                                                                       |
| -------------------------------------------------------------------------------- | -------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `src/backend/Libraries/Services/Services/FileStorage/IFileStorageService.cs`     | Contract       | File storage operations (save / stream / bytes / get / open-read / exists / delete), all `CancellationToken`-aware.                                                                                                           |
| `src/backend/Libraries/Services/Services/FileStorage/FileStorageService.cs`      | Local provider | Local-filesystem implementation; reads `FileStorage:BasePath`.                                                                                                                                                                |
| `src/backend/Libraries/Services/Services/FileStorage/S3FileStorageService.cs`    | S3 provider    | AWS S3 implementation (AWSSDK.S3); reads `FileStorage:S3:{BucketName,KeyPrefix,Region,ServiceUrl,AccessKey,SecretKey}`. Real AWS uses the default credential chain; an optional `ServiceUrl` + creds target LocalStack/MinIO. |
| `src/backend/Libraries/Services/Services/FileStorage/FileStorageContentTypes.cs` | Helper         | Extension → MIME content-type map shared by both providers.                                                                                                                                                                   |
| `src/backend/API/appsettings.json`                                               | Config         | `FileStorage:Provider` + `FileStorage:BasePath` (local) + `FileStorage:S3:{BucketName,KeyPrefix,Region,ServiceUrl,AccessKey,SecretKey}` (dev block pre-pointed at LocalStack).                                                |

## Touched files

| Path                                                | Why                                                                                                                           |
| --------------------------------------------------- | ----------------------------------------------------------------------------------------------------------------------------- |
| `src/backend/API/Program.cs`                        | Selects `S3FileStorageService` vs `FileStorageService` by `FileStorage:Provider`; skips local upload-dir creation in S3 mode. |
| `src/backend/Libraries/Services/Services.csproj`    | `AWSSDK.S3` package reference.                                                                                                |
| `src/backend/API/Controllers/DocumentController.cs` | Uses `IFileStorageService` for upload/download/delete flows.                                                                  |
| `src/backend/Libraries/Domain/Models/Document.cs`   | Stores metadata and ownership information for persisted files.                                                                |

## Local development (LocalStack)

| Path                                  | Why                                                                                     |
| ------------------------------------- | --------------------------------------------------------------------------------------- |
| `.devcontainer/docker-compose.yml`    | `localstack` service (S3 only, gateway `:4566`) for exercising the S3 provider locally. |
| `.devcontainer/localstack/init-s3.sh` | Init hook that creates the `apptemplate-local` dev bucket on startup.                   |
| `.devcontainer/devcontainer.json`     | Forwards port `4566`.                                                                   |

## Migrations

No migration is required for switching providers. A migration is required only when changing document metadata shape.

## External dependencies

| Package     | Purpose                                                                                    |
| ----------- | ------------------------------------------------------------------------------------------ |
| `AWSSDK.S3` | AWS S3 client used by `S3FileStorageService` (`AmazonS3Client`, `PutObjectRequest`, etc.). |
